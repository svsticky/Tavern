using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services.PaymentServices;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    /// <summary>
    /// Implements webhook handling for provider payment updates.
    /// </summary>
    public class PaymentWebhookService(
        PostgresDbContext db,
        AbstractPaymentService paymentService,
        ILogger<PaymentWebhookService> logger
    ) : IPaymentWebhookService
    {
        private bool IsUsingAccountingTool =>
            !string.Equals(Environment.GetEnvironmentVariable("ACCOUNTING_ENABLED"), "false", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(db.Settings.FirstOrDefault(s => s.Name == "AccountingService")?.Value);

        /// <inheritdoc />
        public async Task HandleWebhookAsync(string id)
        {
            logger.LogInformation("Handling payment service webhook for payment id {Id}.", id);
            GetPaymentResponse result = await paymentService.GetPaymentAsync(id);
            var payments = await GetPaymentsByPaymentServiceId(id);

            if (!payments.Any())
                throw new Exception("Payment not found");

            if (result.Status == PaymentStatus.Paid)
            {
                await ProcessPaidPayments(payments, result);
                logger.LogInformation("Processed paid webhook for payment id {Id}. Matched {PaymentCount} local payments.", id, payments.Count);
            }
        }

        private async Task<List<Payment>> GetPaymentsByPaymentServiceId(string id)
        {
            var membershipPayments = await db.MembershipPayments
                .Include(p => p.Member)
                .Where(p => p.PaymentServiceId == id)
                .Cast<Payment>()
                .ToListAsync();

            var enrollmentPayments = await db.EnrollmentPayments
                .Include(p => p.Member)
                .Where(p => p.PaymentServiceId == id)
                .Cast<Payment>()
                .ToListAsync();

            var paymentServiceFeePayments = await db.PaymentServiceFeePayments
                .Include(p => p.Member)
                .Where(p => p.PaymentServiceId == id)
                .Cast<Payment>()
                .ToListAsync();

            return membershipPayments
                .Concat(enrollmentPayments)
                .Concat(paymentServiceFeePayments)
                .ToList();
        }

        private async Task ProcessPaidPayments(IEnumerable<Payment> payments, GetPaymentResponse result)
        {
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                foreach (var payment in payments)
                {
                    MarkPaymentPaid(payment, result);
                    QueueAuthenticationSystemSyncIfNeeded(payment);
                    QueueAccountingTaskIfNeeded(payment);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Failed processing paid webhook transaction for payment id {PaymentId}.", result.PaymentId);
                throw;
            }
        }

        private static void MarkPaymentPaid(Payment payment, GetPaymentResponse result)
        {
            payment.PaidAt = result.PaidAt;
        }

        private void QueueAuthenticationSystemSyncIfNeeded(Payment payment)
        {
            if (payment is MembershipPayment membershipPayment)
            {
                var task = new AuthOutboxTask
                {
                    TaskType = AuthTaskType.Sync,
                    AuthSystemUserId = membershipPayment.Member?.AuthSystemUserId
                        ?? throw new Exception("Member does not have a authentication system ID")
                };

                db.AuthOutboxTasks.Add(task);
            }
        }

        private void QueueAccountingTaskIfNeeded(Payment payment)
        {
            if (IsUsingAccountingTool)
            {
                db.AccountingToolOutboxTasks.Add(new AccountingToolOutboxTask
                {
                    PaymentId = payment.Id,
                    TaskType = payment is MembershipPayment
                        ? AccountingToolTaskType.MembershipPayment
                        : AccountingToolTaskType.EnrollmentPayment
                });
            }
        }
    }
}

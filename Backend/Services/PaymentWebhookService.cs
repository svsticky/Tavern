using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models.Payment.Response;

namespace Backend.Services
{
    public class PaymentWebhookService(
        PostgresDbContext db,
        IPaymentClient paymentClient
    ) : IPaymentWebhookService
    {
        private readonly bool _isUsingAccountingTool = Environment.GetEnvironmentVariable("ACCOUNTING_SERVICE") != null;

        public async Task HandleWebhookAsync(string id)
        {
            PaymentResponse result = await paymentClient.GetPaymentAsync(id);
            var payments = await GetPaymentsByMollieId(id);

            if (!payments.Any())
                throw new Exception("Payment not found");

            if (result.Status == "paid")
            {
                await ProcessPaidPayments(payments, result);
            }
        }

        private async Task<List<Payment>> GetPaymentsByMollieId(string id)
        {
            var membershipPayments = await db.MembershipPayments
                .Include(p => p.Member)
                .Where(p => p.MollieId == id)
                .Cast<Payment>()
                .ToListAsync();

            var enrollmentPayments = await db.EnrollmentPayments
                .Include(p => p.Member)
                .Where(p => p.MollieId == id)
                .Cast<Payment>()
                .ToListAsync();

            var mollieFeePayments = await db.MollieFeePayments
                .Include(p => p.Member)
                .Where(p => p.MollieId == id)
                .Cast<Payment>()
                .ToListAsync();

            return membershipPayments
                .Concat(enrollmentPayments)
                .Concat(mollieFeePayments)
                .ToList();
        }

        private async Task ProcessPaidPayments(IEnumerable<Payment> payments, PaymentResponse result)
        {
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                foreach (var payment in payments)
                {
                    MarkPaymentPaid(payment, result);
                    QueueKeycloakSyncIfNeeded(payment);
                    QueueAccountingTaskIfNeeded(payment);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static void MarkPaymentPaid(Payment payment, PaymentResponse result)
        {
            payment.PaidAt = result.PaidAt;
        }

        private void QueueKeycloakSyncIfNeeded(Payment payment)
        {
            if (payment is MembershipPayment membershipPayment)
            {
                var task = new KeycloakOutboxTask
                {
                    TaskType = KeycloakTaskType.Sync,
                    KeycloakId = membershipPayment.Member?.KeycloakId
                        ?? throw new Exception("Member does not have a Keycloak ID")
                };

                db.KeycloakOutboxTasks.Add(task);
            }
        }

        private void QueueAccountingTaskIfNeeded(Payment payment)
        {
            if (_isUsingAccountingTool)
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

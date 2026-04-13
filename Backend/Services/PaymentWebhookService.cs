using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
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
        private readonly bool _isUsingAccountingTool = Environment.GetEnvironmentVariable("USE_EXACT_API") == "true";

        public async Task HandleWebhookAsync(string id)
        {
            PaymentResponse result = await paymentClient.GetPaymentAsync(id);

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

            var payments = membershipPayments.Concat(enrollmentPayments).Concat(mollieFeePayments).ToList();

            if (!payments.Any())
                throw new Exception("Payment not found");

            if (result.Status == "paid")
            {
                var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    foreach (var payment in payments)
                    {
                        payment.PaidAt = result.PaidAt;

                        if (payment is MembershipPayment membershipPayment)
                        {
                            var task = new KeyCloakOutboxTask
                            {
                                TaskType = KeycloakTaskType.Sync,
                                KeycoakId = membershipPayment.Member?.KeycloakId 
                                    ?? throw new Exception("Member does not have a Keycloak ID")
                            };

                            db.KeyCloakOutboxTasks.Add(task);
                        }

                        if (_isUsingAccountingTool)
                        {
                            db.AccountingToolOutboxTasks.Add(new AccountingToolOutboxTask
                            {
                                PaymentId = payment.Id,
                                TaskType = payment is MembershipPayment ? AccountingToolTaskType.MembershipPayment : AccountingToolTaskType.EnrollmentPayment
                            });
                        }
                        
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
        }
    }
}
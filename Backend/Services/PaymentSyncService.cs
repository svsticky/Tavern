using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Mollie.Api.Client.Abstract;

namespace Backend.Services;

public class PaymentSyncService(IServiceProvider serviceProvider) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit before starting the sync to ensure the application has fully started and all services are available. 
        await Task.Delay(5000);

        await StartSyncPaymentsLoop();
    }

    private async Task StartSyncPaymentsLoop()
    {
        await SyncPayments();

        await Task.Delay(600000).ContinueWith(_ => StartSyncPaymentsLoop());; // Wait for 10 minutes before checking again. This creates a loop where we check every 10 minutes for any updates on pending payments.
    }

    private async Task SyncPayments()
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        var paymentClient = scope.ServiceProvider.GetRequiredService<IPaymentClient>();

        var pendingMembershipPayments = await db.MembershipPayments.Where(p => p.PaidAt == null).ToListAsync();
        var pendingEnrollmentPayments = await db.EnrollmentPayments.Where(p => p.PaidAt == null).ToListAsync();

        var pendingPayments = pendingMembershipPayments.Cast<Payment>().Concat(pendingEnrollmentPayments.Cast<Payment>());

        foreach (var payment in pendingPayments)
        {
            try 
            {
                var mollieStatus = await paymentClient.GetPaymentAsync(payment.MollieId);
                if (mollieStatus.Status == "paid")
                {
                    Payment fullPayment;

                    if(payment is MembershipPayment)
                    {
                        fullPayment = await db.MembershipPayments.Include(p => p.Member).FirstAsync(p => p.Id == payment.Id);
                    }
                    else
                    {
                        fullPayment = await db.EnrollmentPayments.Include(p => p.Member).FirstAsync(p => p.Id == payment.Id);
                    }

                    var transaction = await db.Database.BeginTransactionAsync();

                    try
                    {
                        if(fullPayment.Member != null)
                        {
                            if(fullPayment.Member.KeycloakId == null) throw new Exception("Member isn't synced with Keycloak yet, cannot sync payment status.");

                            db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
                            {
                                KeycoakId = fullPayment.Member.KeycloakId.Value,
                                TaskType = KeycloakTaskType.Sync,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        payment.PaidAt = mollieStatus.PaidAt;

                        db.AccountingToolOutboxTasks.Add(new AccountingToolOutboxTask
                        {
                            PaymentId = payment.Id,
                            TaskType = payment is MembershipPayment ? AccountingToolTaskType.MembershipPayment : AccountingToolTaskType.EnrollmentPayment
                        });

                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                    }
                }
                
                if (mollieStatus.Status == "expired" || mollieStatus.Status == "canceled")
                {
                    // If the payment is expired or canceled, we can remove it from our database as it can no longer be paid.
                    db.Remove(payment);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception) { }
        }
    }
}
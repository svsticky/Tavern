using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services.PaymentServices;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Background worker that periodically reconciles pending payments with the payment provider.
/// </summary>
public class PaymentSyncService(
    IServiceProvider serviceProvider,
    ILogger<PaymentSyncService> logger) : BackgroundService
{

    /// <summary>
    /// Executes the payment synchronization loop, which periodically checks for pending payments in the database and reconciles their status with the payment provider. The loop runs indefinitely until the application is stopped, with a delay between each synchronization cycle to prevent excessive load on the payment provider's API. During each cycle, the service retrieves all pending payments, checks their status with the payment provider, and updates the local database accordingly, marking payments as paid or removing expired/canceled payments as needed. This ensures that the application's payment records remain accurate and up-to-date with the external payment provider's status.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit before starting the sync to ensure the application has fully started and all services are available. 
        try
        {
            await Task.Delay(5000, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        logger.LogInformation("Payment sync worker started.");

        await StartSyncPaymentsLoop(stoppingToken);
    }

    private async Task StartSyncPaymentsLoop(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));

        // Run immediately on startup before the first 10-minute tick
        await ExecuteSyncSafely();

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecuteSyncSafely();
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown requested
        }
    }

    private async Task ExecuteSyncSafely()
    {
        try
        {
            await SyncPayments();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fout tijdens het synchroniseren van betalingen.");
        }
    }

    private async Task SyncPayments()
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        var paymentService = scope.ServiceProvider.GetRequiredService<AbstractPaymentService>();
        var paymentValidationService = scope.ServiceProvider.GetRequiredService<IPaymentValidationService>();
        var authOutboxWorker = scope.ServiceProvider.GetRequiredService<AuthOutboxWorker>();

        var pendingMembershipPayments = await db.MembershipPayments.Include(p => p.Member).Where(p => p.PaidAt == null).ToListAsync();
        var pendingEnrollmentPayments = await db.EnrollmentPayments.Where(p => p.PaidAt == null).ToListAsync();
        var pendingPaymentServiceFeePayments = await db.PaymentServiceFeePayments.Where(p => p.PaidAt == null).ToListAsync();
        var pendingBegunstigerPayments = await db.BegunstigerPayments.Include(p => p.Member).Where(p => p.PaidAt == null).ToListAsync();

        var pendingPayments = pendingMembershipPayments.Cast<Payment>()
            .Concat(pendingPaymentServiceFeePayments.Cast<Payment>())
            .Concat(pendingEnrollmentPayments.Cast<Payment>())
            .Concat(pendingBegunstigerPayments.Cast<Payment>());
        logger.LogInformation("Syncing pending payments. Membership: {MembershipCount}, Enrollment: {EnrollmentCount}, PaymentServiceFee: {PaymentServiceFeeCount}, Begunstiger: {BegunstigerCount}",
            pendingMembershipPayments.Count, pendingEnrollmentPayments.Count, pendingPaymentServiceFeePayments.Count, pendingBegunstigerPayments.Count);

        foreach (var payment in pendingPayments)
        {
            try
            {
                var paymentResponse = await paymentService.GetPaymentAsync(payment.PaymentServiceId);
                if (paymentResponse.Status == PaymentStatus.Paid)
                {
                    logger.LogInformation("Payment {PaymentId} marked paid in the payment service system. Processing sync.", payment.Id);
                    Payment fullPayment;

                    if (payment is MembershipPayment)
                    {
                        fullPayment = await db.MembershipPayments.Include(p => p.Member).FirstAsync(p => p.Id == payment.Id);
                    }
                    else if (payment is PaymentServiceFeePayment)
                    {
                        fullPayment = await db.PaymentServiceFeePayments.Include(p => p.Member).FirstAsync(p => p.Id == payment.Id);
                    }
                    else if (payment is EnrollmentPayment)
                    {
                        fullPayment = await db.EnrollmentPayments.Include(p => p.Member).FirstAsync(p => p.Id == payment.Id);
                    }
                    else if (payment is BegunstigerPayment)
                    {
                        fullPayment = await db.BegunstigerPayments.Include(p => p.Member).FirstAsync(p => p.Id == payment.Id);
                    }
                    else
                    {
                        throw new Exception("Unknown payment type");
                    }

                    using var transaction = await db.Database.BeginTransactionAsync();

                    try
                    {
                        if (fullPayment.Member != null)
                        {
                            if (fullPayment.Member.AuthSystemUserId == null)
                            {
                                // The member isn't linked to the auth system yet. Don't let that block marking the
                                // payment as paid; AuthOutboxWorker queues a catch-up Sync task once they do get linked.
                                logger.LogWarning("Member {MemberId} isn't synced with the authentication system yet. Marking payment {PaymentId} paid without queuing an auth sync.", fullPayment.Member.Id, payment.Id);
                            }
                            else
                            {
                                db.AuthOutboxTasks.Add(new AuthOutboxTask
                                {
                                    AuthSystemUserId = fullPayment.Member.AuthSystemUserId.Value,
                                    TaskType = AuthTaskType.Sync,
                                    CreatedAt = DateTimeOffset.UtcNow,
                                    NextAttemptAt = DateTimeOffset.UtcNow
                                });
                            }
                        }
                        payment.PaidAt = paymentResponse.PaidAt;

                        db.AccountingToolOutboxTasks.Add(new AccountingToolOutboxTask
                        {
                            PaymentId = payment.Id,
                            TaskType = payment switch
                            {
                                MembershipPayment => AccountingToolTaskType.MembershipPayment,
                                PaymentServiceFeePayment => AccountingToolTaskType.PaymentServiceFeePayment,
                                BegunstigerPayment => AccountingToolTaskType.BegunstigerPayment,
                                _ => AccountingToolTaskType.EnrollmentPayment
                            }
                        });

                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        logger.LogError("Transaction rollback while processing paid payment {PaymentId}.", payment.Id);
                        await transaction.RollbackAsync();
                    }
                }

                if (paymentResponse.Status == PaymentStatus.Failed)
                {
                    logger.LogInformation("Payment {PaymentId} is {Status}. Removing stale payment records.", payment.Id, paymentResponse.Status);
                    // If the payment is expired or canceled, we can remove it from our database as it can no longer be paid.
                    using var transaction = await db.Database.BeginTransactionAsync();
                    try
                    {
                        db.Remove(payment);

                        // If it's a membership or begunstiger payment, we also want to check if we should remove the member associated with it.
                        if (payment is MembershipPayment or BegunstigerPayment)
                        {
                            var member = (payment as MembershipPayment)?.Member ?? (payment as BegunstigerPayment)?.Member;

                            if (member != null)
                            {
                                // Don't remove the member if they have another payment still in flight - it may yet succeed.
                                bool hasOtherPendingPayments = await db.MembershipPayments.AnyAsync(p => p.MemberId == member.Id && p.PaidAt == null && p.Id != payment.Id)
                                    || await db.BegunstigerPayments.AnyAsync(p => p.MemberId == member.Id && p.PaidAt == null && p.Id != payment.Id);

                                if (!hasOtherPendingPayments && !paymentValidationService.HasEverPaidMembershipPayment(member.Id) && !paymentValidationService.HasEverPaidBegunstigerFee(member.Id))
                                {
                                    db.Members.Remove(member);
                                    authOutboxWorker.EnqueueTask(AuthTaskType.Delete, member.AuthSystemUserId ?? throw new InvalidOperationException("User is not synced with the authsystem yet."), db);
                                }
                            }
                        }
                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        logger.LogError("Transaction rollback while removing stale payment {PaymentId}.", payment.Id);
                        await transaction.RollbackAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync payment {PaymentId}.", payment.Id);
            }
        }
    }
}

using Backend.Database;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Backend.Services.PaymentServices;

/// <summary>
/// Represents the status of a payment transaction.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// The payment is pending and has not yet been processed.
    /// </summary>
    Paid,

    /// <summary>
    /// The payment is pending and is currently being processed, but the final outcome (success or failure) has not yet been determined.
    /// </summary>
    Pending,

    /// <summary>
    /// The payment has failed due to an error during processing, such as insufficient funds, invalid payment details, or a network issue.
    /// </summary>
    Failed
}

/// <summary>
/// Defines an interface for a payment client that can retrieve the status of a payment transaction based on its unique identifier.
/// </summary>
public abstract class AbstractPaymentService(PostgresDbContext db, ILogger<AbstractPaymentService> logger)
{
    private readonly bool _isUsingAccountingTool = Environment.GetEnvironmentVariable("ACCOUNTING_SERVICE") != null;

    /// <summary>
    /// Retrieves the status of a payment transaction using its unique identifier.
    /// </summary>
    /// <param name="paymentId">The unique identifier of the payment transaction.</param>
    /// <returns>The status of the payment transaction.</returns>
    public abstract Task<GetPaymentResponse> GetPaymentAsync(string paymentId);

    /// <summary>
    /// Cancels a payment transaction using its unique identifier.
    /// </summary>
    /// <param name="paymentId">The unique identifier of the payment transaction.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task CancelPaymentAsync(string paymentId);

    /// <summary>
    /// Creates a new payment transaction with the specified amount, description, and optional parameters for redirect URL, webhook URL, and metadata.
    /// </summary>
    /// <param name="amount">The amount to be paid.</param>
    /// <param name="description">A description of the payment transaction.</param>
    /// <param name="redirectUrl">An optional URL to which the user will be redirected after the payment is completed.</param>
    /// <param name="webhookUrl">An optional URL to which payment status updates will be sent via webhooks.</param>
    /// <param name="metadata">Optional metadata associated with the payment transaction.</param>
    /// <returns>A task that represents the asynchronous operation and contains the unique identifier of the created payment transaction.</returns>
    public abstract Task<CreatePaymentResponse> CreatePaymentAsync(decimal amount, string description, string? redirectUrl = null, string? webhookUrl = null, string? metadata = null);

    /// <summary>
    /// Handles a webhook notification for a payment transaction with the specified unique identifier, processing the payment status update and performing necessary actions based on the updated status. This method is responsible for retrieving the current status of the payment transaction, updating local records accordingly, and triggering any necessary follow-up actions such as synchronizing with authentication systems or accounting tools, all within a transactional context to ensure data integrity.
    /// </summary>
    /// <param name="id">The unique identifier of the payment transaction.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown when the payment is not found.</exception>
    public async Task HandleWebhookAsync(string id)
    {
        logger.LogInformation("Handling payment service webhook for payment id {Id}.", id);
        GetPaymentResponse result = await GetPaymentAsync(id);
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

/// <summary>
/// Represents the response from a payment creation operation, including the unique identifier of the created payment transaction and the URL to which the user can be redirected to complete the payment process. This record is used to encapsulate the necessary information returned by a payment client after successfully creating a payment transaction, allowing services to access both the payment ID for tracking purposes and the payment URL for user redirection in a structured way.
/// </summary>
/// <param name="PaymentId">The unique identifier of the created payment transaction.</param>
/// <param name="PaymentUrl">The URL to which the user can be redirected to complete the payment process.</param>
[ExcludeFromCodeCoverage]
public record CreatePaymentResponse(string PaymentId, string PaymentUrl);

/// <summary>
/// Represents the response from a payment status retrieval operation, including the current status of the payment transaction and the timestamp of when the payment was marked as paid (if applicable). This record is used to encapsulate the necessary information returned by a payment client when querying the status of a payment transaction, allowing services to access both the payment status for decision-making purposes and the paid timestamp for any necessary record-keeping or further processing in a structured way.
/// </summary>
/// <param name="PaymentId">The unique identifier of the payment transaction.</param>
/// <param name="Status">The status of the payment transaction.</param>
/// <param name="PaidAt">The timestamp of when the payment was marked as paid (if applicable).</param>
[ExcludeFromCodeCoverage]
public record GetPaymentResponse(string PaymentId, PaymentStatus Status, DateTimeOffset? PaidAt);
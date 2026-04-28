using Backend.Models.Domain;

namespace Backend.Interfaces;

/// <summary>
/// Defines operations for synchronizing Tavern payment data with the external accounting tool.
/// </summary>
public interface IAccountingToolService
{
    /// <summary>
    /// Synchronizes the payment information with the external accounting tool. This method takes a Payment object and updates the corresponding records in the accounting tool to ensure that the financial data is consistent and up-to-date across both systems. The synchronization process may involve creating, updating, or deleting records in the accounting tool based on the changes in the Payment object, and it should handle any necessary transformations or mappings between the internal data model and the format required by the accounting tool. The method returns a Guid that represents the unique identifier of the synchronized payment record in the accounting tool, allowing for tracking and reference in future operations. The synchronization process should be designed to be robust and efficient, handling potential errors or conflicts that may arise during the communication with the external system while maintaining data integrity and consistency.
    /// </summary>
    /// <param name="payment">The payment object to synchronize.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The unique identifier of the synchronized payment record in the accounting tool.</returns>
    Task<Guid> SyncPaymentAsync(Payment payment, CancellationToken ct);
}

using Backend.Models;

namespace Backend.Interfaces;

public interface IAccountingToolService
{
    Task<Guid> SyncPaymentAsync(Payment payment, CancellationToken ct);
}
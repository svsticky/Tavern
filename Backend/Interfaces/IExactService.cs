using Backend.Models;

namespace Backend.Interfaces;

public interface IExactService
{
    Task<Guid> SyncPaymentAsync(Payment payment, CancellationToken ct);
}
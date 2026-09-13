using Backend.Database;
using Backend.Models.Domain;

namespace Backend.Services.AccountingToolServices;

/// <summary>
/// Defines the base class for synchronizing Tavern payment data with external accounting tools.
/// Handles global enabled/disabled feature toggles and database configuration checks.
/// </summary>
public abstract class AbstractAccountingToolService(
    PostgresDbContext db,
    ILogger<AbstractAccountingToolService> logger)
{
    /// <summary>
    /// The database context.
    /// </summary>
    protected readonly PostgresDbContext _db = db;

    /// <summary>
    /// The logger.
    /// </summary>
    protected readonly ILogger<AbstractAccountingToolService> _logger = logger;

    /// <summary>
    /// Synchronizes payment data with the accounting tool if accounting is enabled.
    /// </summary>
    /// <param name="payment">The payment object to synchronize.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The unique identifier of the synchronized record, or Guid.Empty if disabled.</returns>
    public virtual async Task<Guid> SyncPaymentAsync(Payment payment, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payment);

        var isEnvDisabled = string.Equals(Environment.GetEnvironmentVariable("ACCOUNTING_ENABLED"), "false", StringComparison.OrdinalIgnoreCase);
        var isDbConfigured = !string.IsNullOrWhiteSpace(_db.Settings.FirstOrDefault(s => s.Name == "AccountingService")?.Value);

        if (isEnvDisabled || !isDbConfigured)
        {
            _logger.LogInformation("Accounting sync skipped: ACCOUNTING_ENABLED is set to false or no AccountingService setting is configured.");
            return Guid.Empty;
        }

        return await SyncPaymentCoreAsync(payment, ct);
    }

    /// <summary>
    /// Provider-specific core implementation of payment synchronization.
    /// </summary>
    /// <param name="payment">The payment object to synchronize.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The unique identifier of the synchronized payment record in the accounting tool.</returns>
    protected abstract Task<Guid> SyncPaymentCoreAsync(Payment payment, CancellationToken ct);
}

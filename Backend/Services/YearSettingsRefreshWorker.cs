using Backend.Database;
using Backend.Utils.DateTime;

namespace Backend.Services;

/// <summary>
/// Background worker that keeps <see cref="YearUtils.FinancialYearStartDate"/> and
/// <see cref="YearUtils.CommitteeCreationDate"/> in sync with the Settings table.
///
/// GetYearForDate (and everything built on it - organizer authorization tiers, activity
/// visibility, target-audience checks) is called from plenty of places that aren't DI-managed
/// (static extension methods, POCO helpers), which is why these two settings are read through
/// mutable static fields rather than a scoped/injected service. DatabaseSeeder sets them once at
/// startup, and SettingsService additionally updates them the moment a board member changes the
/// setting - but that update only lands on whichever replica handled the request. With more than
/// one backend replica running, every other replica would otherwise keep using the stale value
/// until it happens to restart, which can make the same request succeed or 403 depending on which
/// replica serves it. This worker bounds that staleness to <see cref="_refreshInterval"/> instead,
/// by having every replica independently re-read the settings on a short interval.
/// </summary>
public class YearSettingsRefreshWorker(
    IServiceProvider serviceProvider,
    ILogger<YearSettingsRefreshWorker> logger) : BackgroundService
{
    private static readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Year settings refresh worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshAsync();

            try
            {
                await Task.Delay(_refreshInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Year settings refresh worker stopped.");
    }

    private async Task RefreshAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        try
        {
            var financialYearStartDate = await db.Settings.FindAsync("FinancialYearStartDate");
            if (!string.IsNullOrWhiteSpace(financialYearStartDate?.Value) && financialYearStartDate.Value != YearUtils.FinancialYearStartDate)
            {
                logger.LogInformation("FinancialYearStartDate changed from {Old} to {New}; refreshing cached value.", YearUtils.FinancialYearStartDate, financialYearStartDate.Value);
                YearUtils.FinancialYearStartDate = financialYearStartDate.Value;
            }

            var committeeCreationDate = await db.Settings.FindAsync("CommitteeCreationDate");
            if (!string.IsNullOrWhiteSpace(committeeCreationDate?.Value) && committeeCreationDate.Value != YearUtils.CommitteeCreationDate)
            {
                logger.LogInformation("CommitteeCreationDate changed from {Old} to {New}; refreshing cached value.", YearUtils.CommitteeCreationDate, committeeCreationDate.Value);
                YearUtils.CommitteeCreationDate = committeeCreationDate.Value;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed refreshing year settings.");
        }
    }
}

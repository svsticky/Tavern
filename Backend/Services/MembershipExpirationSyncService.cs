using Backend.Database;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Background worker that periodically re-syncs members' auth-system access level to account for
/// membership payments that expire purely due to elapsed time. Unlike board rotation (a discrete
/// action) or a new payment (a discrete event), a membership silently lapsing once its expiration
/// window passes has no other trigger point in the backend that would refresh the member's cached
/// Keycloak access_level, so this worker exists specifically to catch that case.
/// </summary>
public class MembershipExpirationSyncService(
    IServiceProvider serviceProvider,
    ILogger<MembershipExpirationSyncService> logger) : BackgroundService
{
    private static readonly TimeSpan _syncInterval = TimeSpan.FromHours(24);

    /// <summary>
    /// If the worker hasn't run in longer than this, don't bother reconstructing which exact days
    /// were missed - a missed-day window this wide already spans a full year cycle, so every
    /// possible month/day anniversary falls inside it anyway, making per-day matching pointless.
    /// Past this threshold, just resync every eligible member once as a safety net and resume
    /// normal incremental catch-up from today.
    /// </summary>
    private const int _maxCatchUpDays = 90;

    /// <summary>
    /// Executes the membership expiration sync loop, re-syncing members whose membership anniversary
    /// fell within the days since this worker last ran successfully, once a day, but only when
    /// membership payments are actually configured to expire - otherwise nothing can silently lapse
    /// and there is nothing to catch up on.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit before starting to ensure the application has fully started and all services are available.
        try
        {
            await Task.Delay(5000, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        logger.LogInformation("Membership expiration sync worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncExpiringMemberships();

            try
            {
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Membership expiration sync worker stopped.");
    }

    private async Task SyncExpiringMemberships()
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        var authOutboxWorker = scope.ServiceProvider.GetRequiredService<AuthOutboxWorker>();

        var expirationSetting = await db.Settings.FindAsync("MembershipPaymentExpirationTime");
        if (string.IsNullOrWhiteSpace(expirationSetting?.Value))
        {
            logger.LogInformation("MembershipPaymentExpirationTime is not configured; membership never expires, skipping sync.");
            return;
        }

        // DateTimeOffset.UtcNow.Date returns a Kind=Unspecified DateTime; converting that to
        // DateTimeOffset implicitly would interpret it as local time and apply the server's local
        // offset instead of UTC, silently shifting the stored checkpoint by a day on any server that
        // isn't running in UTC. Constructing explicitly with a zero offset avoids that.
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var lastRunSetting = await db.Settings.FindAsync("MembershipExpirationSyncLastRunAt");
        var lastRunDate = DateTimeOffset.TryParse(lastRunSetting?.Value, out var parsedLastRun)
            ? parsedLastRun.UtcDateTime.Date
            : today.AddDays(-1); // First run ever: only check today, don't backfill history.

        if (lastRunDate >= today)
        {
            logger.LogInformation("Membership expiration sync already ran today; nothing to catch up on.");
            return;
        }

        var daysMissed = (int)(today - lastRunDate).TotalDays;

        // A member's paid status can only flip on the yearly anniversary of the anchor date that
        // PaymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime bases its expiration
        // window on: their earliest study enrollment date if they have one, otherwise their most
        // recent membership payment date. Re-syncing every non-begunstiger member daily is wasteful
        // when only members whose anniversary actually fell in the missed window can have changed.
        var candidates = await db.Members
            .Where(m => m.AuthSystemUserId != null && !m.Begunstiger)
            .Select(m => new
            {
                AuthSystemUserId = m.AuthSystemUserId!.Value,
                HasStudy = m.StudyEnrollments.Any(),
                StudyAnchor = m.StudyEnrollments
                    .OrderBy(se => se.EnrollmentDate)
                    .Select(se => (DateTimeOffset?)se.EnrollmentDate)
                    .FirstOrDefault(),
                PaymentAnchor = db.MembershipPayments
                    .Where(p => p.MemberId == m.Id && p.PaidAt != null)
                    .OrderByDescending(p => p.PaidAt)
                    .Select(p => p.PaidAt)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var anchoredCandidates = candidates
            .Select(c => new { c.AuthSystemUserId, Anchor = c.HasStudy ? c.StudyAnchor : c.PaymentAnchor })
            .Where(c => c.Anchor != null)
            .ToList();

        List<Guid> authSystemUserIds;
        if (daysMissed > _maxCatchUpDays)
        {
            logger.LogWarning("Membership expiration sync last ran {Days} days ago; resyncing every eligible member once instead of reconstructing individual missed days.", daysMissed);
            authSystemUserIds = anchoredCandidates.Select(c => c.AuthSystemUserId).ToList();
        }
        else
        {
            var missedMonthDays = new HashSet<(int Month, int Day)>();
            for (var date = lastRunDate.AddDays(1); date <= today; date = date.AddDays(1))
            {
                missedMonthDays.Add((date.Month, date.Day));
            }

            authSystemUserIds = anchoredCandidates
                .Where(c => missedMonthDays.Contains((c.Anchor!.Value.Month, c.Anchor.Value.Day)))
                .Select(c => c.AuthSystemUserId)
                .ToList();
        }

        logger.LogInformation("Re-syncing auth access level for {Count} members to account for time-based membership expiration.", authSystemUserIds.Count);

        foreach (var authSystemUserId in authSystemUserIds)
        {
            authOutboxWorker.EnqueueTask(AuthTaskType.Sync, authSystemUserId, db);
        }

        await UpsertLastRunSetting(db, today);
    }

    private static async Task UpsertLastRunSetting(PostgresDbContext db, DateTimeOffset today)
    {
        var setting = await db.Settings.FindAsync("MembershipExpirationSyncLastRunAt");
        if (setting == null)
        {
            db.Settings.Add(new Setting { Name = "MembershipExpirationSyncLastRunAt", Value = today.ToString("o") });
        }
        else
        {
            setting.Value = today.ToString("o");
        }

        await db.SaveChangesAsync();
    }
}

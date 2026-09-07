using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Backend.Services;

/// <summary>
/// Service for promoting candidate board members to actual board members at the start of a new financial year. This service checks if the promotion has already been done for the current year to avoid duplicate promotions. If not, it retrieves the candidate board members from the previous year and creates new board memberships for them in the current year, while also enqueuing messages to update their group memberships in the auth system.
/// </summary>
/// <param name="serviceScopeFactory">The service scope factory for creating service scopes.</param>
public class CreateNewBoardService(IServiceScopeFactory serviceScopeFactory) : ICreateNewBoardService
{
    /// <inheritdoc />
    public async Task PromoteCandidateBoardToBoardAsync(Guid? userId = null)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        var authOutboxWorker = scope.ServiceProvider.GetRequiredService<AuthOutboxWorker>();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        var adminStatusWorkers = (scope.ServiceProvider.GetService(typeof(IEnumerable<IAdminStatusUpdateOutboxWorker>)) as IEnumerable<IAdminStatusUpdateOutboxWorker>) ?? [];

        if (userId.HasValue)
        {
            permissionService.EnsureBoardMember(userId.Value);
        }

        var boardGroupId = uint.Parse((await db.Settings.FindAsync("BoardGroupId"))?.Value ?? "1", CultureInfo.InvariantCulture);
        var candidateBoardGroupId = uint.Parse((await db.Settings.FindAsync("CandidateBoardGroupId"))?.Value ?? "2", CultureInfo.InvariantCulture);

        var maxBoardYear = await db.GroupMemberships
            .Where(gm => gm.GroupId == boardGroupId)
            .MaxAsync(gm => (uint?)gm.MembershipYear);

        // Target year is the year for the upcoming/current committee creation date
        var committeeYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var targetYear = maxBoardYear.HasValue && maxBoardYear.Value >= committeeYear
            ? maxBoardYear.Value + 1
            : committeeYear;

        var currentYear = targetYear;
        var lastYear = maxBoardYear ?? (currentYear - 1);

        var candidates = await db.GroupMemberships
            .Include(gm => gm.Member)
            .Where(gm => gm.GroupId == candidateBoardGroupId && gm.MembershipYear == lastYear)
            .ToListAsync();

        if (!candidates.Any())
        {
            throw new InvalidOperationException($"No candidate board members found for year {lastYear}. Cannot promote to board for year {currentYear}.");
        }

        var oldBoardMembers = await db.GroupMemberships
            .Include(gm => gm.Member)
            .Where(gm => gm.GroupId == boardGroupId && gm.MembershipYear == lastYear)
            .ToListAsync();

        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            foreach (var candidate in candidates)
            {
                db.GroupMemberships.Add(new GroupMembership
                {
                    GroupId = boardGroupId,
                    MemberId = candidate.MemberId,
                    RoleAliasId = candidate.RoleAliasId,
                    MembershipYear = currentYear
                });

                EnqueueAuthSync(candidate.Member, authOutboxWorker, db);
                EnqueueAdminStatusSync(candidate.Member, true, adminStatusWorkers, db);
            }

            foreach (var oldMember in oldBoardMembers)
            {
                EnqueueAuthSync(oldMember.Member, authOutboxWorker, db);
                EnqueueAdminStatusSync(oldMember.Member, false, adminStatusWorkers, db);
            }

            // Reset Gratie and Begunstiger status for all active members upon board rotation.
            var specialMembers = await db.Members
                .Where(m => m.Gratie || m.Begunstiger)
                .ToListAsync();

            foreach (var m in specialMembers)
            {
                m.Gratie = false;
                m.Begunstiger = false;

                EnqueueAuthSync(m, authOutboxWorker, db);
            }

            // Stamps when the board last rotated, so begunstiger fees (which are due again every
            // board term, unlike the regular membership expiration window) can be checked against
            // this instead of a fixed date.
            var lastBoardRotationAt = await db.Settings.FindAsync("LastBoardRotationAt");
            if (lastBoardRotationAt == null)
            {
                db.Settings.Add(new Setting { Name = "LastBoardRotationAt", Value = DateTimeOffset.UtcNow.ToString("o") });
            }
            else
            {
                lastBoardRotationAt.Value = DateTimeOffset.UtcNow.ToString("o");
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

    /// <summary>
    /// Queues an auth-system sync for a member, if there is one. AuthOutboxWorker resolves the member's
    /// auth-system user ID itself, creating one first if the member isn't linked yet.
    /// </summary>
    private void EnqueueAuthSync(Member? member, AuthOutboxWorker authOutboxWorker, PostgresDbContext db)
    {
        if (member == null) return;

        authOutboxWorker.EnqueueTask(AuthTaskType.Sync, member.Id, db);
    }

    private void EnqueueAdminStatusSync(Member? member, bool isAdmin, IEnumerable<IAdminStatusUpdateOutboxWorker> adminStatusWorkers, PostgresDbContext db)
    {
        if (member == null) return;

        foreach (var worker in adminStatusWorkers)
        {
            worker.EnqueueAdminStatusUpdate(member.Email, isAdmin, db);
        }
    }
}

using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Utils.DateTime;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for promoting candidate board members to actual board members at the start of a new financial year. This service checks if the promotion has already been done for the current year to avoid duplicate promotions. If not, it retrieves the candidate board members from the previous year and creates new board memberships for them in the current year, while also enqueuing messages to update their group memberships in the auth system.
/// </summary>
/// <param name="serviceScopeFactory">The service scope factory for creating service scopes.</param>
public class CreateNewBoardService(IServiceScopeFactory serviceScopeFactory) : ICreateNewBoardService
{
    /// <inheritdoc />
    public async Task PromoteCandidateBoardToBoardAsync()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        var authOutboxWorker = scope.ServiceProvider.GetRequiredService<AuthOutboxWorker>();

        var boardGroupId = uint.Parse((await db.Settings.FindAsync("BoardGroupId"))?.Value ?? "1");
        var candidateBoardGroupId = uint.Parse((await db.Settings.FindAsync("CandidateBoardGroupId"))?.Value ?? "2");
        
        var currentYear = FinancialYearUtils.GetCurrentFinancialYear();
        var lastYear = currentYear - 1;

        bool alreadyRotated = await db.GroupMemberships.AnyAsync(gm => 
            gm.GroupId == boardGroupId && gm.MembershipYear == currentYear);

        if (alreadyRotated) return;

        var candidates = await db.GroupMemberships
            .Where(gm => gm.GroupId == candidateBoardGroupId && gm.MembershipYear == lastYear)
            .ToListAsync();

        var oldBoardMembers = await db.GroupMemberships
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

                await authOutboxWorker.EnqueueTask(AuthTaskType.Sync, candidate.MemberId);
            }

            foreach(var oldMember in oldBoardMembers)
            {
                await authOutboxWorker.EnqueueTask(AuthTaskType.Sync, oldMember.MemberId);
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
}
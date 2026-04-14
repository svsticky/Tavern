using Backend.Models.Domain;
using Backend.Utils;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database;

public class GroupInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        string boardGroupId = await EnsureSettingExists(db, "BoardGroupId", "1");
        
        await EnsureGroupExists(db, "BoardGroupId", GroupType.Committee, uint.Parse(boardGroupId));

        await EnsureSettingExists(db, "MollieFee", "0.39");

        await EnsureSettingExists(db, "MollieFeeGlAccount", "5007");

        await EnsureSettingExists(db, "MembershipGLAccount", "8000");

        await EnsureSettingExists(db, "MollieApiKey", string.Empty);

        await EnsureBoardAccountExists(db);
    }

    private static async Task<string> EnsureSettingExists(PostgresDbContext db, string name, string defaultValue)
    {
        var setting = await db.Settings.FindAsync(name);

        if (setting != null)
        {
            return setting.Value;
        }

        db.Settings.Add(new Setting
        {
            Name = name,
            Value = defaultValue
        });

        await db.SaveChangesAsync();
        return defaultValue;
    }

    private static async Task EnsureGroupExists(
        PostgresDbContext db,
        string name,
        GroupType type,
        uint id)
    {
        var exists = await db.Groups.AnyAsync(g => g.Id == id);

        if (!exists)
        {
            db.Groups.Add(new Group
            {
                Id = id,
                Name = name,
                Type = type,
                Active = true
            });

            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureBoardAccountExists(PostgresDbContext db)
    {
        uint boardGroupId = uint.Parse((await db.Settings.FindAsync("BoardGroupId"))!.Value);

        string backupEmail = Environment.GetEnvironmentVariable("BACKUP_ACCOUNT_EMAIL") ?? throw new Exception("BACKUP_ACCOUNT_EMAIL environment variable is not set");

        if(string.IsNullOrEmpty(backupEmail))
        {
            return;
        }

        var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            bool hasBoardMembers = await db.GroupMemberships.AnyAsync(gm => gm.GroupId == boardGroupId && gm.MembershipYear == YearUtils.GetCurrentFinancialYear());
            if (!hasBoardMembers)
            {
                var backupMember = new Member
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = "0600000000",
                    Street = "Street",
                    HouseNumber = "1",
                    PostalCode = "1234AB",
                    City = "City",
                    FirstName = "Backup",
                    LastName = "Account",
                    Email = backupEmail
                };

                db.Members.Add(backupMember);
                await db.SaveChangesAsync();

                db.GroupMemberships.Add(new GroupMembership
                {
                    GroupId = boardGroupId,
                    MemberId = backupMember.Id,
                    RoleAliasId = null
                });

                await db.SaveChangesAsync();
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        await transaction.CommitAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
using Backend.Models.Domain;
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
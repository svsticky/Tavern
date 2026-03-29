using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database;

public class GroupInitializer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GroupInitializer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        await EnsureGroupExists(db, "Board", GroupType.Committee, PredefinedGroups.Board);
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
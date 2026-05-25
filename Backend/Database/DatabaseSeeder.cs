using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Utils.DateTime;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database;

/// <summary>
/// The DatabaseSeeder class is responsible for seeding the database with initial data and ensuring that certain essential settings and groups exist when the application starts. It implements the IHostedService interface, allowing it to run as a background service during the application's startup process. The seeder checks for the existence of specific settings and groups, creating them if they do not already exist, and also ensures that a backup board account is created if there are no existing board members. This helps to ensure that the application has the necessary configuration and data in place for proper functionality from the moment it is launched.
/// </summary>
/// <param name="scopeFactory">The factory for creating service scopes.</param>
public class DatabaseSeeder(IServiceScopeFactory scopeFactory) : IHostedService
{
    /// <summary>
    /// Starts the database seeding process. This method is called when the application starts and is responsible for ensuring that essential settings and groups are present in the database. It creates a new service scope to access the database context and performs checks to create default settings and groups if they do not already exist. Additionally, it ensures that a backup board account is created if there are no existing board members, providing a fallback option for administrative access. The seeding process helps to establish a baseline configuration for the application, allowing it to function correctly from the outset.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        string boardGroupId = await EnsureSettingExists(db, "BoardGroupId", "1");
        
        string candidateBoardGroupId = await EnsureSettingExists(db, "CandidateBoardGroupId", "2");
        
        await EnsureGroupExists(db, "Board", GroupType.Committee, uint.Parse(boardGroupId));
        
        await EnsureGroupExists(db, "Candidate Board", GroupType.Committee, uint.Parse(candidateBoardGroupId));

        await EnsureSettingExists(db, "PaymentServiceFee", "0.39");

        await EnsureSettingExists(db, "PaymentServiceFeeGLAccount", "5007");

        await EnsureSettingExists(db, "PaymentServiceFeeCostUnit", "TRX");

        await EnsureSettingExists(db, "MembershipGLAccount", "8000");
        
        await EnsureSettingExists(db, "ActivityGLAccount", "7001");

        await EnsureSettingExists(db, "PaymentServicePaymentsCondition", "2");

        await EnsureSettingExists(db, "PaymentServiceRelationCode", "473");

        await EnsureSettingExists(db, "MembershipVATCode", "0");

        await EnsureSettingExists(db, "PaymentServiceVATCode", "21");

        await EnsureSettingExists(db, "MembershipPrice", "7.50");

        await EnsureSettingExists(db, "FinancialEmailSender", "");

        await EnsureSettingExists(db, "ActivityUpdateEmailSender", "");

        await EnsureSettingExists(db, "MembershipPaymentExpirationTime", ""); // If empty, no expiration time will be set on payments, and they will not automatically expire.
        
        await EnsureSettingExists(db, "MastersShouldPayMembership", "0");

        await EnsureSettingExists(db, "GratieShouldPayMembership", "0");

        await EnsureSettingExists(db, "ErelidShouldPayMembership", "0");

        await EnsureSettingExists(db, "LidVanVerdiensteShouldPayMembership", "0");

        var authOutboxWorker = scope.ServiceProvider.GetRequiredService<AuthOutboxWorker>();

        var createNewBoardService = scope.ServiceProvider.GetRequiredService<ICreateNewBoardService>();

        await EnsureBoardAccountExists(db, authOutboxWorker, createNewBoardService);
    }

    /// <summary>
    /// Ensures that a specific setting exists in the database. If the setting with the given name does not exist, it creates a new setting with the provided default value. This method is used during the database seeding process to establish essential configuration parameters that the application relies on for its operation. By checking for the existence of the setting before creating it, this method helps to prevent duplicate entries and ensures that existing configurations are preserved while still providing necessary defaults when needed.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="name">The name of the setting to ensure.</param>
    /// <param name="defaultValue">The default value for the setting if it does not exist.</param>
    /// <returns>The value of the setting.</returns>
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

    /// <summary>
    /// Ensures that a specific group exists in the database. If a group with the given identifier does not exist, it creates a new group with the specified name and type. This method is used during the database seeding process to establish essential groups that are necessary for the application's organizational structure and permission management. By checking for the existence of the group before creating it, this method helps to prevent duplicate entries and ensures that existing groups are preserved while still providing necessary defaults when needed.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="name">The name of the group to ensure.</param>
    /// <param name="type">The type of the group to ensure.</param>
    /// <param name="id">The unique identifier of the group to ensure.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task EnsureGroupExists(
        PostgresDbContext db,
        string name,
        GroupType type,
        uint id)
    {
        var exists = await db.Groups.AnyAsync(g => g.Id == id);

        if (!exists)
        {
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                db.Groups.Add(new Group
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    Active = true
                });

                await db.SaveChangesAsync();

                await db.Database.ExecuteSqlRawAsync($@"
                    SELECT setval(pg_get_serial_sequence('""Groups""', 'Id'), (SELECT MAX(""Id"") FROM ""Groups""));
                ");
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
            }
        }
    }

    /// <summary>
    /// Ensures that a backup board account exists in the database. This method checks if there are any existing board members in the group specified by the "BoardGroupId" setting. If no board members are found, it creates a backup member with predefined details and adds them to the board group. This is a safety measure to ensure that there is always at least one member in the board group, providing a fallback option for administrative access in case all other board members are removed or become inactive. The method also handles the creation of a membership payment for the backup member and ensures that the database state remains consistent through the use of transactions.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="authOutboxWorker">The authentication outbox worker.</param>
    /// <param name="createNewBoardService">The service for creating new board members.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task EnsureBoardAccountExists(PostgresDbContext db, AuthOutboxWorker authOutboxWorker, ICreateNewBoardService createNewBoardService)
    {
        uint boardGroupId = uint.Parse((await db.Settings.FindAsync("BoardGroupId"))!.Value);


        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            bool hasBoardMembers = await db.GroupMemberships.AnyAsync(gm => gm.GroupId == boardGroupId && gm.MembershipYear == FinancialYearUtils.GetCurrentFinancialYear());
            if (!hasBoardMembers)
            {
                var candidateBoardGroupId = uint.Parse((await db.Settings.FindAsync("CandidateBoardGroupId"))!.Value);
                var candidateBoardMembershipsLastYear = await db.GroupMemberships.Where(gm => gm.GroupId == candidateBoardGroupId && gm.MembershipYear == FinancialYearUtils.GetCurrentFinancialYear() - 1).ToListAsync();
                
                if(candidateBoardMembershipsLastYear.Any())
                {
                    await createNewBoardService.PromoteCandidateBoardToBoardAsync();
                    return;
                }

                string? backupEmail = Environment.GetEnvironmentVariable("BACKUP_ACCOUNT_EMAIL");

                if(string.IsNullOrEmpty(backupEmail))
                {
                    return;
                }

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

                db.GroupMemberships.Add(new GroupMembership
                {
                    GroupId = boardGroupId,
                    MemberId = backupMember.Id,
                    RoleAliasId = null,
                    MembershipYear = FinancialYearUtils.GetCurrentFinancialYear()
                });

                await authOutboxWorker.EnqueueTask(AuthTaskType.Create, backupMember.Id);

                db.MembershipPayments.Add(new MembershipPayment
                {
                    PaymentServiceId = "",
                    PaymentIntentUrl = "",
                    Price = decimal.Parse(db.Settings.Find("MembershipPrice")?.Value ?? "7.50"),
                    PaidAt = DateTime.UtcNow,
                    MemberId = backupMember.Id,
                    ManuallyMarkedAsPaid = true
                });

                await db.SaveChangesAsync();

                await db.Database.ExecuteSqlRawAsync($@"
                    SELECT setval(pg_get_serial_sequence('""GroupMemberships""', 'Id'), 
                    COALESCE((SELECT MAX(""Id"") FROM ""GroupMemberships""), 1));
                ");

                await transaction.CommitAsync();
            }
        }
        catch
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// Stops the database seeding service. This method is called when the application is shutting down and can be used to perform any necessary cleanup operations related to the seeding process. In this implementation, there are no specific cleanup actions required, so the method simply returns a completed task. However, it provides a placeholder for any future cleanup logic that may be needed as the application evolves.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
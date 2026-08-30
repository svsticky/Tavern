using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Utils.DateTime;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Backend.Database;

/// <summary>
/// The DatabaseSeeder class is responsible for seeding the database with initial data and ensuring that certain essential settings and groups exist when the application starts. It implements the IHostedService interface, allowing it to run as a background service during the application's startup process. The seeder checks for the existence of specific settings and groups, creating them if they do not already exist, and also ensures that a backup board account is created if there are no existing board members. This helps to ensure that the application has the necessary configuration and data in place for proper functionality from the moment it is launched.
/// </summary>
/// <param name="scopeFactory">The factory for creating service scopes.</param>
/// <param name="logger">The logger used to surface otherwise-silent seeding failures.</param>
/// <param name="environment">Used to only default local-dev-only settings (e.g. the Mailpit SMTP catcher) when actually running in the devcontainer, never in production.</param>
[ExcludeFromCodeCoverage]
public class DatabaseSeeder(IServiceScopeFactory scopeFactory, ILogger<DatabaseSeeder> logger, IHostEnvironment environment) : IHostedService
{
    private const string _boardPrimaryLightDefault = "#f98f55";
    private const string _boardPrimaryDefault = "#fa6b20";
    private const string _boardPrimaryDarkDefault = "#ca5617";

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

        await EnsureGroupExists(db, "Board", GroupType.Committee, uint.Parse(boardGroupId, CultureInfo.InvariantCulture));

        await EnsureGroupExists(db, "Candidate Board", GroupType.Committee, uint.Parse(candidateBoardGroupId, CultureInfo.InvariantCulture));

        await EnsureSettingExists(db, "PaymentProvider", "MOLLIE");
        await EnsureSettingExists(db, "MollieApiKey", "");

        await EnsureSettingExists(db, "MailService", "SMTP");
        await EnsureSettingExists(db, "MailgunToken", "");
        await EnsureSettingExists(db, "MailgunPublicKey", "");
        await EnsureSettingExists(db, "MailgunApiBaseUrl", "");

        bool useMailpit = environment.IsDevelopment();
        await EnsureSettingExists(db, "SmtpHost", useMailpit ? "mailpit" : "");
        await EnsureSettingExists(db, "SmtpPort", useMailpit ? "1025" : "587");
        await EnsureSettingExists(db, "SmtpStartTls", useMailpit ? "false" : "true");
        await EnsureSettingExists(db, "SmtpUser", "");
        await EnsureSettingExists(db, "SmtpPass", "");

        await EnsureSettingExists(db, "AccountingService", "");
        await EnsureSettingExists(db, "ExactAccessToken", "");
        await EnsureSettingExists(db, "ExactDivision", "");

        await EnsureSettingExists(db, "MailSubscriptionService", "");
        await EnsureSettingExists(db, "MailchimpListKey", "");
        await EnsureSettingExists(db, "MailchimpApiKey", "");

        await EnsureSettingExists(db, "PaymentServiceFee", "0.39");

        await EnsureSettingExists(db, "PaymentServiceFeeGLAccount", "5007");

        await EnsureSettingExists(db, "PaymentServiceFeeCostUnit", "TRX");

        await EnsureSettingExists(db, "PaymentServiceFeeCostCenter", "");

        await EnsureSettingExists(db, "MembershipGLAccount", "8000");

        await EnsureSettingExists(db, "MembershipCostCenter", "");

        await EnsureSettingExists(db, "MembershipCostUnit", "");

        await EnsureSettingExists(db, "ActivityGLAccount", "7001");

        await EnsureSettingExists(db, "PaymentServicePaymentsCondition", "2");

        await EnsureSettingExists(db, "PaymentServiceRelationCode", "473");

        await EnsureSettingExists(db, "MembershipVATCode", "0");

        await EnsureSettingExists(db, "PaymentServiceFeeVATCode", "21");

        await EnsureSettingExists(db, "MembershipPrice", "7.50");

        await EnsureSettingExists(db, "BegunstigerPrice", "10.00");

        await EnsureSettingExists(db, "BegunstigerGLAccount", "8000");

        await EnsureSettingExists(db, "BegunstigerCostCenter", "");

        await EnsureSettingExists(db, "BegunstigerCostUnit", "");

        await EnsureSettingExists(db, "BegunstigerVATCode", "0");

        await EnsureSettingExists(db, "MainBoardMail", "");

        await EnsureSettingExists(db, "FinancialEmailSender", "");

        await EnsureSettingExists(db, "ActivityUpdateEmailSender", "");

        await EnsureSettingExists(db, "MembershipPaymentExpirationTime", ""); // If empty, no expiration time will be set on payments, and they will not automatically expire.

        await EnsureSettingExists(db, "BoardPrimaryLight", _boardPrimaryLightDefault);
        await EnsureSettingExists(db, "BoardPrimary", _boardPrimaryDefault);
        await EnsureSettingExists(db, "BoardPrimaryDark", _boardPrimaryDarkDefault);

        await EnsureSettingExists(db, "MastersShouldPayMembership", "0");

        await EnsureSettingExists(db, "GratieShouldPayMembership", "0");

        await EnsureSettingExists(db, "ErelidShouldPayMembership", "0");

        await EnsureSettingExists(db, "LidVanVerdiensteShouldPayMembership", "0");

        string financialYearStartDateSetting = await EnsureSettingExists(db, "FinancialYearStartDate", "08-01");
        YearUtils.FinancialYearStartDate = financialYearStartDateSetting;

        string committeeCreationDateSetting = await EnsureSettingExists(db, "CommitteeCreationDate", "08-01");
        YearUtils.CommitteeCreationDate = committeeCreationDateSetting;

        await EnsureSettingExists(db, "StudyStartDates", "09-01,02-01");

        await EnsureSettingExists(db, "YearlyMailSendDate", "09-01");

        var authOutboxWorker = scope.ServiceProvider.GetRequiredService<AuthOutboxWorker>();

        var createNewBoardService = scope.ServiceProvider.GetRequiredService<ICreateNewBoardService>();

        await EnsureBoardAccountExists(db, authOutboxWorker, createNewBoardService, logger);

        await EnsureRegisterReasonsSeeded(db);
        await EnsureRegistrationDocumentsSeeded(db);
        await EnsureExternalLinksSeeded(db);
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
    /// Ensures that a backup board account exists in the database and stays in good repair. This method checks if there are any existing board members in the group specified by the "BoardGroupId" setting. If a backup member already exists (matched by the "BACKUP_ACCOUNT_EMAIL" env var) but is currently missing board rights for the current year or a study enrollment - e.g. because a board rotation or manual edit reset it, or because it was created before study enrollments were required - it is repaired in place rather than silently failing to re-create a duplicate. Only when no such member exists yet, and there are no other board members to fall back on, is a brand new backup member created with board rights, a study enrollment, and a paid membership. The method ensures the database state remains consistent through the use of transactions.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="authOutboxWorker">The authentication outbox worker.</param>
    /// <param name="createNewBoardService">The service for creating new board members.</param>
    /// <param name="logger">The logger used to surface otherwise-silent seeding failures.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task EnsureBoardAccountExists(PostgresDbContext db, AuthOutboxWorker authOutboxWorker, ICreateNewBoardService createNewBoardService, ILogger<DatabaseSeeder> logger)
    {
        uint boardGroupId = uint.Parse((await db.Settings.FindAsync("BoardGroupId"))!.Value, CultureInfo.InvariantCulture);
        string? backupEmail = Environment.GetEnvironmentVariable("BACKUP_ACCOUNT_EMAIL");

        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            uint maxBoardYear = await db.GroupMemberships.Where(gm => gm.GroupId == boardGroupId).MaxAsync(gm => (uint?)gm.MembershipYear) ?? YearUtils.GetYearForDate(DateTime.UtcNow, YearUtils.CommitteeCreationDate);

            var existingBackupMember = !string.IsNullOrEmpty(backupEmail)
                ? await db.Members.FirstOrDefaultAsync(m => m.Email == backupEmail)
                : null;

            if (!string.IsNullOrEmpty(backupEmail) && existingBackupMember == null)
            {
                logger.LogInformation("BACKUP_ACCOUNT_EMAIL is set to {BackupEmail} but no member with that email exists yet - it will be created if there are no other board members.", backupEmail);
            }

            if (existingBackupMember != null)
            {
                // The backup account already exists - repair it in place instead of trying to insert
                // a duplicate (which would fail on the unique email index and silently roll back,
                // leaving it without board rights or a study forever).
                bool addedGroupMembership = false;

                bool isCurrentBoardMember = await db.GroupMemberships.AnyAsync(gm => gm.GroupId == boardGroupId && gm.MemberId == existingBackupMember.Id && gm.MembershipYear == maxBoardYear);
                if (!isCurrentBoardMember)
                {
                    db.GroupMemberships.Add(new GroupMembership
                    {
                        GroupId = boardGroupId,
                        MemberId = existingBackupMember.Id,
                        RoleAliasId = null,
                        MembershipYear = maxBoardYear
                    });
                    authOutboxWorker.EnqueueTask(AuthTaskType.Sync, existingBackupMember.Id, db);
                    addedGroupMembership = true;
                }

                bool hasStudy = await db.StudyEnrollments.AnyAsync(se => se.MemberId == existingBackupMember.Id);
                if (!hasStudy)
                {
                    var backupStudy = await EnsureBackupStudyExists(db);
                    db.StudyEnrollments.Add(new StudyEnrollment
                    {
                        MemberId = existingBackupMember.Id,
                        Study = backupStudy,
                        EnrollmentDate = DateTimeOffset.UtcNow,
                        Status = StudyStatus.Enrolled
                    });
                }

                if (addedGroupMembership || !hasStudy)
                {
                    logger.LogInformation("Repairing backup board account {BackupEmail}: addedGroupMembership={AddedGroupMembership}, addedStudyEnrollment={AddedStudyEnrollment}.", backupEmail, addedGroupMembership, !hasStudy);
                }

                await db.SaveChangesAsync();

                if (addedGroupMembership)
                {
                    await db.Database.ExecuteSqlRawAsync($@"
                        SELECT setval(pg_get_serial_sequence('""GroupMemberships""', 'Id'),
                        COALESCE((SELECT MAX(""Id"") FROM ""GroupMemberships""), 1));
                    ");
                }

                await transaction.CommitAsync();
                return;
            }

            bool hasBoardMembers = await db.GroupMemberships.AnyAsync(gm => gm.GroupId == boardGroupId && gm.MembershipYear == maxBoardYear);
            if (!hasBoardMembers)
            {
                var candidateBoardGroupId = uint.Parse((await db.Settings.FindAsync("CandidateBoardGroupId"))!.Value, CultureInfo.InvariantCulture);
                var candidateBoardMembershipsLastYear = await db.GroupMemberships.Where(gm => gm.GroupId == candidateBoardGroupId && gm.MembershipYear == maxBoardYear - 1).ToListAsync();

                if (candidateBoardMembershipsLastYear.Any())
                {
                    await createNewBoardService.PromoteCandidateBoardToBoardAsync();
                    return;
                }

                if (string.IsNullOrEmpty(backupEmail))
                {
                    return;
                }

                var backupMember = new Member
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = "0600000000",
                    StudentNumber = "BackupMember",
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
                    MembershipYear = maxBoardYear
                });

                // The paywall only allows paying membership - and therefore only grants access -
                // to members who are a begunstiger or have (had) a study enrollment. Give the backup
                // account a study enrollment too, so it isn't locked out of the app it needs to administer.
                var backupStudy = await EnsureBackupStudyExists(db);
                db.StudyEnrollments.Add(new StudyEnrollment
                {
                    MemberId = backupMember.Id,
                    Study = backupStudy,
                    EnrollmentDate = DateTimeOffset.UtcNow,
                    Status = StudyStatus.Enrolled
                });

                authOutboxWorker.EnqueueTask(AuthTaskType.Create, backupMember.Id, db);

                db.MembershipPayments.Add(new MembershipPayment
                {
                    PaymentServiceId = "",
                    PaymentIntentUrl = "",
                    Price = decimal.Parse(db.Settings.Find("MembershipPrice")?.Value ?? "7.50", CultureInfo.InvariantCulture),
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure the backup board account exists/is repaired.");
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// Ensures a placeholder study exists for enrolling the backup board account into, so it satisfies
    /// the "has (had) a study" requirement for paying membership without representing a real study program.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <returns>The existing or newly created placeholder study.</returns>
    private static async Task<Study> EnsureBackupStudyExists(PostgresDbContext db)
    {
        var study = await db.Studies.FirstOrDefaultAsync(s => s.Title == "Backup");
        if (study != null)
        {
            return study;
        }

        study = new Study
        {
            Title = "Backup",
            NominalDurationYears = 3,
            Type = StudyType.Bachelor
        };
        db.Studies.Add(study);
        return study;
    }

    private static async Task EnsureRegisterReasonsSeeded(PostgresDbContext db)
    {
        if (await db.RegisterReasons.AnyAsync())
        {
            return;
        }

        db.RegisterReasons.AddRange(
            new RegisterReason
            {
                TitleDutch = "Boekenkortingen",
                TitleEnglish = "Book discounts",
                DescriptionDutch = "Als lid van Sticky kun je korting krijgen op studieboeken.",
                DescriptionEnglish = "As a member of the study association, you can get discounts on study books.",
                SortOrder = 1
            },
            new RegisterReason
            {
                TitleDutch = "Goedkope activiteiten",
                TitleEnglish = "Cheap activities",
                DescriptionDutch = "Onze vereniging organiseert veel activiteiten van gratis borrels en leuke feestjes tot educatieve workshops en lezingen.",
                DescriptionEnglish = "Our association organizes many activities from free drinks and fun parties to educational workshops and lectures.",
                SortOrder = 2
            },
            new RegisterReason
            {
                TitleDutch = "Netwerken",
                TitleEnglish = "Networking",
                DescriptionDutch = "Door mee te doen met onze vereniging, ontmoet je veel medestudenten en breid je netwerk uit.",
                DescriptionEnglish = "By joining our association, you will meet many fellow students and expand your network.",
                SortOrder = 3
            },
            new RegisterReason
            {
                TitleDutch = "Introductie week",
                TitleEnglish = "Introduction week",
                DescriptionDutch = "Onze vereniging organiseert een bachelor introductie week aan het begin van het academische jaar. Dit is een geweldig idee om de vereniging te leren kennen en andere studenten te ontmoeten.",
                DescriptionEnglish = "Our association organizes an bachelor introduction week at the beginning of the academic year. This is a great way to get to know the association and meet other students.",
                SortOrder = 4
            },
            new RegisterReason
            {
                TitleDutch = "Arbeidsmarktoriëntatie",
                TitleEnglish = "Labor market orientation",
                DescriptionDutch = "Onze vereniging biedt arbeidsmarktoriëntatie aan om studenten te helpen zich voor te bereiden op hun toekomstige carrière.",
                DescriptionEnglish = "Our association offers labor market orientation sessions to help students prepare for their future careers.",
                SortOrder = 5
            },
            new RegisterReason
            {
                TitleDutch = "Leden",
                TitleEnglish = "Members",
                DescriptionDutch = "We hebben een divers groep van ongeveer 2000 leden, inclusief bachelor- en masterstudenten uit verschillende studieprogramma's. Onze leden zijn actief in het organiseren van activiteiten, het deelnemen aan commissies en het genieten van het sociale aspect van onze vereniging.",
                DescriptionEnglish = "We have a diverse group of about 2000 members, including bachelor and master students from various study programs. Our members are active in organizing activities, participating in committees and enjoying the social aspect of our association.",
                SortOrder = 6
            }
        );

        await db.SaveChangesAsync();
    }

    private static async Task EnsureRegistrationDocumentsSeeded(PostgresDbContext db)
    {
        if (await db.RegistrationDocuments.AnyAsync())
        {
            return;
        }

        db.RegistrationDocuments.AddRange(
            new RegistrationDocument
            {
                NameDutch = "Privacyverklaring",
                NameEnglish = "Privacy Statement",
                Url = "https://public.svsticky.nl/privacystatement.pdf",
                SortOrder = 1
            }
        );

        await db.SaveChangesAsync();
    }

    private static async Task EnsureExternalLinksSeeded(PostgresDbContext db)
    {
        if (await db.ExternalLinks.AnyAsync())
        {
            return;
        }

        db.ExternalLinks.AddRange(
            new ExternalLink
            {
                TitleDutch = "Mongoose",
                TitleEnglish = "Mongoose",
                DescriptionDutch = "Onze mini supermarkt.",
                DescriptionEnglish = "Our mini supermarket.",
                Url = "https://mongoose.svsticky.nl",
                SortOrder = 1
            },
            new ExternalLink
            {
                TitleDutch = "Foto's",
                TitleEnglish = "Photos",
                DescriptionDutch = "Bekijk onze foto's.",
                DescriptionEnglish = "View our photos.",
                Url = "https://fotos.svsticky.nl",
                SortOrder = 2
            },
            new ExternalLink
            {
                TitleDutch = "Bestanden",
                TitleEnglish = "Files",
                DescriptionDutch = "Bekijk onze bestanden.",
                DescriptionEnglish = "View our files.",
                Url = "https://files.svsticky.nl",
                SortOrder = 3
            },
            new ExternalLink
            {
                TitleDutch = "DigiDecs",
                TitleEnglish = "DigiDecs",
                DescriptionDutch = "Declareer je onkosten snel en eenvoudig via DigiDecs.",
                DescriptionEnglish = "Submit your expense claims quickly and easily via DigiDecs.",
                Url = "https://digidecs.svsticky.nl",
                SortOrder = 4
            },
            new ExternalLink
            {
                TitleDutch = "Boeken",
                TitleEnglish = "Books",
                DescriptionDutch = "Haal boeken met korting.",
                DescriptionEnglish = "Get books with discount.",
                Url = "https://svsticky.nl/boeken",
                SortOrder = 5
            },
            new ExternalLink
            {
                TitleDutch = "Vacatures",
                TitleEnglish = "Jobs",
                DescriptionDutch = "Bekijk de vacatures binnen onze vereniging.",
                DescriptionEnglish = "View vacancies within our association.",
                Url = "https://svsticky.nl/carriere/vacatures",
                SortOrder = 6
            },
            new ExternalLink
            {
                TitleDutch = "GitHub",
                TitleEnglish = "GitHub",
                DescriptionDutch = "Bekijk onze code op GitHub.",
                DescriptionEnglish = "View our code on GitHub.",
                Url = "https://github.com/svsticky",
                SortOrder = 7
            },
            new ExternalLink
            {
                TitleDutch = "Discord",
                TitleEnglish = "Discord",
                DescriptionDutch = "Word lid van onze Discord server.",
                DescriptionEnglish = "Join our Discord server.",
                Url = "https://svsticky.nl/discord",
                SortOrder = 8
            },
            new ExternalLink
            {
                TitleDutch = "Stickypedia",
                TitleEnglish = "Stickypedia",
                DescriptionDutch = "Onze eigen wiki vol met informatie over de vereniging.",
                DescriptionEnglish = "Our own wiki full of association information.",
                Url = "https://wiki.svsticky.nl",
                SortOrder = 9
            },
            new ExternalLink
            {
                TitleDutch = "VoelJeVeilig",
                TitleEnglish = "VoelJeVeilig",
                DescriptionDutch = "Meld ongewenst gedrag anoniem via VoelJeVeilig.",
                DescriptionEnglish = "Report inappropriate behavior anonymously via VoelJeVeilig.",
                Url = "https://voeljeveilig.svsticky.nl",
                SortOrder = 10
            },
            new ExternalLink
            {
                TitleDutch = "Commissiestrijd",
                TitleEnglish = "Commissiestrijd",
                DescriptionDutch = "Bekijk de voortgang van de commissiestrijd.",
                DescriptionEnglish = "View the progress of the committee battle.",
                Url = "https://commissiestrijd.svsticky.nl",
                SortOrder = 11
            }
        );

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Stops the database seeding service. This method is called when the application is shutting down and can be used to perform any necessary cleanup operations related to the seeding process. In this implementation, there are no specific cleanup actions required, so the method simply returns a completed task. However, it provides a placeholder for any future cleanup logic that may be needed as the application evolves.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

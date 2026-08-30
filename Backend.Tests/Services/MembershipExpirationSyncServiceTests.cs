using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services;

public class MembershipExpirationSyncServiceTests
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly AuthOutboxWorker _authOutboxWorker;
    private readonly ILogger<MembershipExpirationSyncService> _logger;
    // DateTimeOffset.UtcNow.Date returns a Kind=Unspecified DateTime; implicitly converting that to
    // DateTimeOffset treats it as local time and applies the local system offset instead of UTC, which
    // shifts the represented instant across midnight whenever the local zone isn't UTC. Constructing
    // explicitly with a zero offset keeps this aligned with the service's own UTC date arithmetic.
    private static readonly DateTimeOffset _today = new(DateTime.UtcNow.Date, TimeSpan.Zero);

    public MembershipExpirationSyncServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _authOutboxWorker = Substitute.For<AuthOutboxWorker>(null, null);
        _logger = NullLogger<MembershipExpirationSyncService>.Instance;
    }

    private ServiceProvider CreateServiceProvider(PostgresDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(_authOutboxWorker);
        return services.BuildServiceProvider();
    }

    private class TestableMembershipExpirationSyncService(IServiceProvider serviceProvider, ILogger<MembershipExpirationSyncService> logger)
        : MembershipExpirationSyncService(serviceProvider, logger)
    {
        public Task PublicSyncExpiringMemberships()
        {
            var method = typeof(MembershipExpirationSyncService)
                .GetMethod("SyncExpiringMemberships", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException("SyncExpiringMemberships method not found");
            }
            return (Task)method.Invoke(this, null)!;
        }
    }

    private static Member CreateTestMember(Guid? authSystemUserId, bool begunstiger, string studentNumber)
    {
        return new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = authSystemUserId,
            Begunstiger = begunstiger,
            FirstName = "Test",
            LastName = "User",
            Email = $"{Guid.NewGuid()}@example.com",
            StudentNumber = studentNumber,
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-25),
            PreferredLanguage = Language.EN
        };
    }

    private static StudyEnrollment CreateEnrollment(Member member, DateTimeOffset enrollmentDate, Study study)
    {
        return new StudyEnrollment
        {
            MemberId = member.Id,
            Member = member,
            StudyId = study.Id,
            Study = study,
            EnrollmentDate = enrollmentDate,
            Status = StudyStatus.Enrolled
        };
    }

    private static MembershipPayment CreateMembershipPayment(Member member, DateTimeOffset paidAt)
    {
        return new MembershipPayment
        {
            MemberId = member.Id,
            Member = member,
            Price = 7.50m,
            PaymentServiceId = "",
            PaymentIntentUrl = "",
            PaidAt = paidAt
        };
    }

    [Fact]
    public async Task SyncExpiringMemberships_ExpirationNotConfigured_DoesNothing()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        db.Members.Add(CreateTestMember(Guid.NewGuid(), begunstiger: false, "s1"));
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestableMembershipExpirationSyncService(provider, _logger);

        // Act
        await service.PublicSyncExpiringMemberships();

        // Assert
        _authOutboxWorker.DidNotReceiveWithAnyArgs().EnqueueTask(default, default, default!);
    }

    [Fact]
    public async Task SyncExpiringMemberships_ExpirationConfiguredButEmpty_DoesNothing()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        db.Settings.Add(new Setting { Name = "MembershipPaymentExpirationTime", Value = "" });
        db.Members.Add(CreateTestMember(Guid.NewGuid(), begunstiger: false, "s1"));
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestableMembershipExpirationSyncService(provider, _logger);

        // Act
        await service.PublicSyncExpiringMemberships();

        // Assert
        _authOutboxWorker.DidNotReceiveWithAnyArgs().EnqueueTask(default, default, default!);
    }

    [Fact]
    public async Task SyncExpiringMemberships_FirstRun_SyncsOnlyMembersWithAnniversaryTodayAndRecordsLastRun()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        db.Settings.Add(new Setting { Name = "MembershipPaymentExpirationTime", Value = "1" });

        var study = new Study { Id = 1, Title = "CS", NominalDurationYears = 3, Type = StudyType.Bachelor };
        db.Studies.Add(study);

        // Anniversary is today - should be synced.
        var matchingAuthId = Guid.NewGuid();
        var matchingMember = CreateTestMember(matchingAuthId, begunstiger: false, "s1");
        db.Members.Add(matchingMember);
        db.StudyEnrollments.Add(CreateEnrollment(matchingMember, _today.AddYears(-2), study));

        // Anniversary is a month from now - should not be synced.
        var nonMatchingAuthId = Guid.NewGuid();
        var nonMatchingMember = CreateTestMember(nonMatchingAuthId, begunstiger: false, "s2");
        db.Members.Add(nonMatchingMember);
        db.StudyEnrollments.Add(CreateEnrollment(nonMatchingMember, _today.AddDays(30).AddYears(-2), study));

        // Anniversary is today, but a begunstiger - should not be synced (their fee expires on board rotation instead).
        var begunstigerAuthId = Guid.NewGuid();
        var begunstigerMember = CreateTestMember(begunstigerAuthId, begunstiger: true, "s3");
        db.Members.Add(begunstigerMember);
        db.StudyEnrollments.Add(CreateEnrollment(begunstigerMember, _today.AddYears(-2), study));

        // Anniversary is today, but no linked auth account - should not be synced.
        var unlinkedMember = CreateTestMember(null, begunstiger: false, "s4");
        db.Members.Add(unlinkedMember);
        db.StudyEnrollments.Add(CreateEnrollment(unlinkedMember, _today.AddYears(-2), study));

        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestableMembershipExpirationSyncService(provider, _logger);

        // Act
        await service.PublicSyncExpiringMemberships();

        // Assert
        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, matchingMember.Id, Arg.Any<PostgresDbContext>());
        _authOutboxWorker.DidNotReceive().EnqueueTask(AuthTaskType.Sync, nonMatchingMember.Id, Arg.Any<PostgresDbContext>());
        _authOutboxWorker.DidNotReceive().EnqueueTask(AuthTaskType.Sync, begunstigerMember.Id, Arg.Any<PostgresDbContext>());
        _authOutboxWorker.Received(1).EnqueueTask(Arg.Any<AuthTaskType>(), Arg.Any<Guid>(), Arg.Any<PostgresDbContext>());

        var lastRun = await db.Settings.FindAsync("MembershipExpirationSyncLastRunAt");
        Assert.NotNull(lastRun);
        Assert.Equal(_today.UtcDateTime.Date, DateTimeOffset.Parse(lastRun.Value).UtcDateTime.Date);
    }

    [Fact]
    public async Task SyncExpiringMemberships_NoStudyEnrollment_FallsBackToMostRecentMembershipPaymentAnchor()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        db.Settings.Add(new Setting { Name = "MembershipPaymentExpirationTime", Value = "1" });

        // No study enrollment, but a past membership payment whose anniversary is today - should be synced.
        var paidAuthId = Guid.NewGuid();
        var paidMember = CreateTestMember(paidAuthId, begunstiger: false, "s5");
        db.Members.Add(paidMember);
        db.MembershipPayments.Add(CreateMembershipPayment(paidMember, _today.AddYears(-3)));

        // No study enrollment and no payment at all - has no anchor, should not be synced.
        var neverPaidAuthId = Guid.NewGuid();
        var neverPaidMember = CreateTestMember(neverPaidAuthId, begunstiger: false, "s6");
        db.Members.Add(neverPaidMember);

        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestableMembershipExpirationSyncService(provider, _logger);

        // Act
        await service.PublicSyncExpiringMemberships();

        // Assert
        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, paidMember.Id, Arg.Any<PostgresDbContext>());
        _authOutboxWorker.DidNotReceive().EnqueueTask(AuthTaskType.Sync, neverPaidMember.Id, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task SyncExpiringMemberships_CatchUpAfterMissedDays_SyncsAnniversariesWithinTheGap()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        db.Settings.Add(new Setting { Name = "MembershipPaymentExpirationTime", Value = "1" });
        db.Settings.Add(new Setting { Name = "MembershipExpirationSyncLastRunAt", Value = _today.AddDays(-3).ToString("o") });

        var study = new Study { Id = 1, Title = "CS", NominalDurationYears = 3, Type = StudyType.Bachelor };
        db.Studies.Add(study);

        // Anniversary was 2 days ago - within the missed window (last run was 3 days ago), should be synced.
        var withinGapAuthId = Guid.NewGuid();
        var withinGapMember = CreateTestMember(withinGapAuthId, begunstiger: false, "s7");
        db.Members.Add(withinGapMember);
        db.StudyEnrollments.Add(CreateEnrollment(withinGapMember, _today.AddDays(-2).AddYears(-2), study));

        // Anniversary was 10 days ago - well outside the missed window, should not be synced.
        var outsideGapAuthId = Guid.NewGuid();
        var outsideGapMember = CreateTestMember(outsideGapAuthId, begunstiger: false, "s8");
        db.Members.Add(outsideGapMember);
        db.StudyEnrollments.Add(CreateEnrollment(outsideGapMember, _today.AddDays(-10).AddYears(-2), study));

        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestableMembershipExpirationSyncService(provider, _logger);

        // Act
        await service.PublicSyncExpiringMemberships();

        // Assert
        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, withinGapMember.Id, Arg.Any<PostgresDbContext>());
        _authOutboxWorker.DidNotReceive().EnqueueTask(AuthTaskType.Sync, outsideGapMember.Id, Arg.Any<PostgresDbContext>());

        var lastRun = await db.Settings.FindAsync("MembershipExpirationSyncLastRunAt");
        Assert.Equal(_today.UtcDateTime.Date, DateTimeOffset.Parse(lastRun!.Value).UtcDateTime.Date);
    }

    [Fact]
    public async Task SyncExpiringMemberships_AlreadyRanToday_DoesNothing()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        db.Settings.Add(new Setting { Name = "MembershipPaymentExpirationTime", Value = "1" });
        db.Settings.Add(new Setting { Name = "MembershipExpirationSyncLastRunAt", Value = _today.ToString("o") });

        var study = new Study { Id = 1, Title = "CS", NominalDurationYears = 3, Type = StudyType.Bachelor };
        db.Studies.Add(study);

        var member = CreateTestMember(Guid.NewGuid(), begunstiger: false, "s9");
        db.Members.Add(member);
        db.StudyEnrollments.Add(CreateEnrollment(member, _today.AddYears(-2), study));

        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestableMembershipExpirationSyncService(provider, _logger);

        // Act
        await service.PublicSyncExpiringMemberships();

        // Assert
        _authOutboxWorker.DidNotReceiveWithAnyArgs().EnqueueTask(default, default, default!);
    }

    [Fact]
    public async Task SyncExpiringMemberships_LastRunLongAgo_ResyncsEveryAnchoredMemberOnce()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        db.Settings.Add(new Setting { Name = "MembershipPaymentExpirationTime", Value = "1" });
        db.Settings.Add(new Setting { Name = "MembershipExpirationSyncLastRunAt", Value = _today.AddDays(-500).ToString("o") });

        var study = new Study { Id = 1, Title = "CS", NominalDurationYears = 3, Type = StudyType.Bachelor };
        db.Studies.Add(study);

        // The gap is well past the catch-up threshold, so per-day anniversary matching is skipped -
        // every anchored member should be resynced once regardless of where their anniversary falls.
        var farAnchorAuthId = Guid.NewGuid();
        var farAnchorMember = CreateTestMember(farAnchorAuthId, begunstiger: false, "s10");
        db.Members.Add(farAnchorMember);
        db.StudyEnrollments.Add(CreateEnrollment(farAnchorMember, _today.AddDays(-40).AddYears(-2), study));

        // Still excluded: no anchor at all (no study enrollment, no membership payment ever).
        var noAnchorAuthId = Guid.NewGuid();
        var noAnchorMember = CreateTestMember(noAnchorAuthId, begunstiger: false, "s11");
        db.Members.Add(noAnchorMember);

        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestableMembershipExpirationSyncService(provider, _logger);

        // Act
        await service.PublicSyncExpiringMemberships();

        // Assert
        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, farAnchorMember.Id, Arg.Any<PostgresDbContext>());
        _authOutboxWorker.DidNotReceive().EnqueueTask(AuthTaskType.Sync, noAnchorMember.Id, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task ExecuteAsync_CanceledImmediately_ReturnsImmediately()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var provider = CreateServiceProvider(db);
        var service = new MembershipExpirationSyncService(provider, _logger);

        // Act
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately so Task.Delay(5000, token) throws TaskCanceledException and exits instantly

        var startTask = service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);
        await startTask;
    }

    [Fact]
    public async Task ExecuteAsync_RunsLoopAndStopsGracefullyAfterStartupDelay()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var service = new MembershipExpirationSyncService(provider, _logger);

        // Act - let the fixed 5s startup delay elapse so the loop body actually runs once,
        // then cancel so the subsequent 24h Task.Delay is interrupted and the loop exits via break.
        var cts = new CancellationTokenSource();
        var startTask = service.StartAsync(cts.Token);

        await Task.Delay(5500);

        cts.Cancel();
        await service.StopAsync(CancellationToken.None);
        await startTask;
    }
}

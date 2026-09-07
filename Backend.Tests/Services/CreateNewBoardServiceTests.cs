using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Utils.DateTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services;

public class CreateNewBoardServiceTests : IDisposable
{
    private readonly PostgresDbContext _db;
    private readonly AuthOutboxWorker _authOutboxWorkerMock;
    private readonly IAdminStatusUpdateOutboxWorker _adminStatusWorkerMock;
    private readonly IServiceScopeFactory _serviceScopeFactoryMock;
    private readonly IPermissionService _permissionServiceMock;
    private readonly CreateNewBoardService _service;

    public CreateNewBoardServiceTests()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(options);
        _db.Database.EnsureCreated();

        // Mock dependencies for the service provider
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        var serviceScopeMock = Substitute.For<IServiceScope>();
        _serviceScopeFactoryMock = Substitute.For<IServiceScopeFactory>();

        _serviceScopeFactoryMock.CreateScope().Returns(serviceScopeMock);
        serviceScopeMock.ServiceProvider.Returns(serviceProviderMock);

        // Mock AuthOutboxWorker
        var loggerMock = Substitute.For<ILogger<AuthOutboxWorker>>();
        _authOutboxWorkerMock = Substitute.For<AuthOutboxWorker>(serviceProviderMock, loggerMock);

        _adminStatusWorkerMock = Substitute.For<IAdminStatusUpdateOutboxWorker>();
        _permissionServiceMock = Substitute.For<IPermissionService>();

        serviceProviderMock.GetService(typeof(PostgresDbContext)).Returns(_db);
        serviceProviderMock.GetService(typeof(AuthOutboxWorker)).Returns(_authOutboxWorkerMock);
        serviceProviderMock.GetService(typeof(IPermissionService)).Returns(_permissionServiceMock);
        serviceProviderMock.GetService(typeof(IEnumerable<IAdminStatusUpdateOutboxWorker>)).Returns(new[] { _adminStatusWorkerMock });

        _service = new CreateNewBoardService(_serviceScopeFactoryMock);
    }

    private Member CreateTestMember(Guid id, Guid? authSystemUserId, string studentNumber)
    {
        return new Member
        {
            Id = id,
            AuthSystemUserId = authSystemUserId,
            FirstName = "Test",
            LastName = "User",
            Email = $"{Guid.NewGuid()}@example.com",
            StudentNumber = studentNumber,
            PhoneNumber = "0600000000",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "City"
        };
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_WithUserId_EnsuresBoardMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var boardGroupId = 10u;
        var candidateBoardGroupId = 20u;
        var currentYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var lastYear = currentYear - 1;

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = candidateBoardGroupId.ToString() });

        var candidate = Guid.NewGuid();
        _db.Members.Add(CreateTestMember(candidate, Guid.NewGuid(), "s901"));
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = candidateBoardGroupId,
            MemberId = candidate,
            MembershipYear = lastYear
        });
        await _db.SaveChangesAsync();

        // Act
        await _service.PromoteCandidateBoardToBoardAsync(userId);

        // Assert
        _permissionServiceMock.Received(1).EnsureBoardMember(userId);
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_NoCandidates_ThrowsInvalidOperationException()
    {
        // Arrange
        var boardGroupId = 10u;
        var candidateBoardGroupId = 20u;

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = candidateBoardGroupId.ToString() });
        await _db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PromoteCandidateBoardToBoardAsync());
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_CandidateWithoutAuthSystemId_StillQueuesSyncForThatMember()
    {
        // Arrange - AuthOutboxWorker resolves/creates the auth-system user itself, so a candidate not
        // linked yet no longer gets silently skipped.
        var boardGroupId = 10u;
        var candidateBoardGroupId = 20u;
        var currentYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var lastYear = currentYear - 1;

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = candidateBoardGroupId.ToString() });

        // Candidate not yet linked to the auth system
        var candidate = Guid.NewGuid();
        _db.Members.Add(CreateTestMember(candidate, null, "s901"));
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = candidateBoardGroupId,
            MemberId = candidate,
            MembershipYear = lastYear
        });
        await _db.SaveChangesAsync();

        // Act
        await _service.PromoteCandidateBoardToBoardAsync();

        // Assert
        _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, candidate, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_WhenMaxBoardYearSet_PromotesCandidatesForMaxBoardYearToTargetYear()
    {
        // Arrange
        var boardGroupId = 10u;
        var candidateBoardGroupId = 20u;
        var committeeYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = candidateBoardGroupId.ToString() });

        // maxBoardYear will be committeeYear + 1. targetYear becomes committeeYear + 2.
        var candidateId = Guid.NewGuid();
        var candidateAuthSystemUserId = Guid.NewGuid();
        _db.Members.Add(CreateTestMember(candidateId, candidateAuthSystemUserId, "s900"));
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = boardGroupId,
            MemberId = Guid.NewGuid(),
            MembershipYear = committeeYear
        });
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = boardGroupId,
            MemberId = Guid.NewGuid(),
            MembershipYear = committeeYear + 1
        });
        // Candidate board member for year (committeeYear + 1)
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = candidateBoardGroupId,
            MemberId = candidateId,
            MembershipYear = committeeYear + 1
        });
        await _db.SaveChangesAsync();

        // Act
        await _service.PromoteCandidateBoardToBoardAsync();

        // Assert
        // Verify candidate was promoted to targetYear (committeeYear + 2)
        var totalMemberships = await _db.GroupMemberships.CountAsync();
        Assert.Equal(4, totalMemberships);
        _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, candidateId, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_WhenNotRotated_PromotesCandidatesAndEnqueuesSyncs()
    {
        // Arrange
        var boardGroupId = 10u;
        var candidateBoardGroupId = 20u;
        var currentYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var lastYear = currentYear - 1;

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = candidateBoardGroupId.ToString() });

        // Add 2 candidates from last year
        var candidate1 = Guid.NewGuid();
        var candidate1AuthSystemUserId = Guid.NewGuid();
        var candidate2 = Guid.NewGuid();
        var candidate2AuthSystemUserId = Guid.NewGuid();
        _db.Members.Add(CreateTestMember(candidate1, candidate1AuthSystemUserId, "s901"));
        _db.Members.Add(CreateTestMember(candidate2, candidate2AuthSystemUserId, "s902"));
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = candidateBoardGroupId,
            MemberId = candidate1,
            MembershipYear = lastYear,
            RoleAliasId = 101u
        });
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = candidateBoardGroupId,
            MemberId = candidate2,
            MembershipYear = lastYear,
            RoleAliasId = null
        });

        // Add 1 old board member from last year
        var oldBoardMember = Guid.NewGuid();
        var oldBoardMemberAuthSystemUserId = Guid.NewGuid();
        _db.Members.Add(CreateTestMember(oldBoardMember, oldBoardMemberAuthSystemUserId, "s903"));
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = boardGroupId,
            MemberId = oldBoardMember,
            MembershipYear = lastYear
        });

        // Add members with Gratie and Begunstiger flags
        var gratieMemberAuthSystemUserId = Guid.NewGuid();
        var gratieMember = new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = gratieMemberAuthSystemUserId,
            Gratie = true,
            FirstName = "Gratie",
            LastName = "User",
            Email = "g@test.local",
            StudentNumber = "s100",
            PhoneNumber = "0600000000",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "City"
        };
        var begunstigerMemberAuthSystemUserId = Guid.NewGuid();
        var begunstigerMember = new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = begunstigerMemberAuthSystemUserId,
            Begunstiger = true,
            FirstName = "Begunstiger",
            LastName = "User",
            Email = "b@test.local",
            StudentNumber = "s101",
            PhoneNumber = "0600000000",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "City"
        };
        _db.Members.AddRange(gratieMember, begunstigerMember);

        await _db.SaveChangesAsync();

        // Act
        await _service.PromoteCandidateBoardToBoardAsync();

        // Assert
        // Verify candidates are promoted to the new board group for current year
        var newBoardMembers = await _db.GroupMemberships
            .Where(gm => gm.GroupId == boardGroupId && gm.MembershipYear == currentYear)
            .ToListAsync();

        Assert.Equal(2, newBoardMembers.Count);
        Assert.Contains(newBoardMembers, m => m.MemberId == candidate1 && m.RoleAliasId == 101u);
        Assert.Contains(newBoardMembers, m => m.MemberId == candidate2 && m.RoleAliasId == null);

        // Verify Gratie and Begunstiger flags were reset to false
        var updatedGratie = await _db.Members.FindAsync(gratieMember.Id);
        var updatedBegunstiger = await _db.Members.FindAsync(begunstigerMember.Id);
        Assert.False(updatedGratie!.Gratie);
        Assert.False(updatedBegunstiger!.Begunstiger);

        // Verify sync tasks were enqueued for all candidates and old board members, keyed by their
        // local Member.Id - AuthOutboxWorker resolves the actual auth-system user itself
        _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, candidate1, Arg.Any<PostgresDbContext>());
        _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, candidate2, Arg.Any<PostgresDbContext>());
        _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, oldBoardMember, Arg.Any<PostgresDbContext>());

        // Verify sync tasks were also enqueued for the members whose Gratie/Begunstiger flag was just
        // reset, so their Keycloak access_level doesn't stay stale as "paid" once it should flip
        _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, gratieMember.Id, Arg.Any<PostgresDbContext>());
        _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, begunstigerMember.Id, Arg.Any<PostgresDbContext>());

        // Verify the last board rotation timestamp was stamped, so begunstiger fee checks can use it
        var lastBoardRotationAt = await _db.Settings.FindAsync("LastBoardRotationAt");
        Assert.NotNull(lastBoardRotationAt);
        Assert.True(DateTimeOffset.TryParse(lastBoardRotationAt!.Value, out var stampedAt));
        Assert.True(stampedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_WhenRotatedBefore_UpdatesExistingLastBoardRotationAtSetting()
    {
        // Arrange
        var boardGroupId = 10u;
        var candidateBoardGroupId = 20u;
        var currentYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var lastYear = currentYear - 1;

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = candidateBoardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "LastBoardRotationAt", Value = DateTimeOffset.UtcNow.AddYears(-1).ToString("o") });

        var candidateId = Guid.NewGuid();
        _db.Members.Add(CreateTestMember(candidateId, Guid.NewGuid(), "s905"));
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = candidateBoardGroupId,
            MemberId = candidateId,
            MembershipYear = lastYear,
            RoleAliasId = null
        });

        await _db.SaveChangesAsync();

        // Act
        await _service.PromoteCandidateBoardToBoardAsync();

        // Assert
        var lastBoardRotationAt = await _db.Settings.FindAsync("LastBoardRotationAt");
        Assert.NotNull(lastBoardRotationAt);
        Assert.True(DateTimeOffset.TryParse(lastBoardRotationAt!.Value, out var stampedAt));
        Assert.True(stampedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_WhenExceptionOccurs_RollsBackTransactionAndThrows()
    {
        // Arrange
        var boardGroupId = 10u;
        var candidateBoardGroupId = 20u;
        var currentYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var lastYear = currentYear - 1;

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = candidateBoardGroupId.ToString() });

        var candidateId = Guid.NewGuid();
        _db.Members.Add(CreateTestMember(candidateId, Guid.NewGuid(), "s904"));
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = candidateBoardGroupId,
            MemberId = candidateId,
            MembershipYear = lastYear
        });
        await _db.SaveChangesAsync();

        // Force EnqueueTask to throw an exception
        _authOutboxWorkerMock.When(x => x.EnqueueTask(Arg.Any<AuthTaskType>(), Arg.Any<Guid>(), Arg.Any<PostgresDbContext>()))
            .Do(x => throw new InvalidOperationException("Simulated auth worker failure"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.PromoteCandidateBoardToBoardAsync()
        );
        Assert.Equal("Simulated auth worker failure", exception.Message);

        // Verify that the candidate was NOT saved as a new board member due to transaction rollback
        var boardMemberships = await _db.GroupMemberships
            .Where(gm => gm.GroupId == boardGroupId && gm.MembershipYear == currentYear)
            .ToListAsync();
        Assert.Empty(boardMemberships);
    }
}

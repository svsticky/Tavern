using Backend.Database;
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
    private readonly IServiceScopeFactory _serviceScopeFactoryMock;
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

        serviceProviderMock.GetService(typeof(PostgresDbContext)).Returns(_db);
        serviceProviderMock.GetService(typeof(AuthOutboxWorker)).Returns(_authOutboxWorkerMock);

        _service = new CreateNewBoardService(_serviceScopeFactoryMock);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_WhenAlreadyRotated_DoesNothing()
    {
        // Arrange
        var boardGroupId = 10u;
        var currentYear = YearUtils.GetCurrentBoardYear();

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        
        // Add a membership in the current year to trigger alreadyRotated
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = boardGroupId,
            MemberId = Guid.NewGuid(),
            MembershipYear = currentYear
        });
        await _db.SaveChangesAsync();

        // Act
        await _service.PromoteCandidateBoardToBoardAsync();

        // Assert
        // Verify no candidates were processed or added
        var totalMemberships = await _db.GroupMemberships.CountAsync();
        Assert.Equal(1, totalMemberships);
        await _authOutboxWorkerMock.DidNotReceiveWithAnyArgs().EnqueueTask(default, default);
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_WhenNotRotated_PromotesCandidatesAndEnqueuesSyncs()
    {
        // Arrange
        var boardGroupId = 10u;
        var candidateBoardGroupId = 20u;
        var currentYear = YearUtils.GetCurrentBoardYear();
        var lastYear = currentYear - 1;

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = candidateBoardGroupId.ToString() });

        // Add 2 candidates from last year
        var candidate1 = Guid.NewGuid();
        var candidate2 = Guid.NewGuid();
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
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = boardGroupId,
            MemberId = oldBoardMember,
            MembershipYear = lastYear
        });

        // Add members with Gratie and Begunstiger flags
        var gratieMember = new Member
        {
            Id = Guid.NewGuid(),
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
        var begunstigerMember = new Member
        {
            Id = Guid.NewGuid(),
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

        // Verify sync tasks were enqueued for all candidates and old board members
        await _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, candidate1);
        await _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, candidate2);
        await _authOutboxWorkerMock.Received(1).EnqueueTask(AuthTaskType.Sync, oldBoardMember);
    }

    [Fact]
    public async Task PromoteCandidateBoardToBoardAsync_WhenExceptionOccurs_RollsBackTransactionAndThrows()
    {
        // Arrange
        var boardGroupId = 10u;
        var candidateBoardGroupId = 20u;
        var currentYear = YearUtils.GetCurrentBoardYear();
        var lastYear = currentYear - 1;

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = boardGroupId.ToString() });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = candidateBoardGroupId.ToString() });

        var candidateId = Guid.NewGuid();
        _db.GroupMemberships.Add(new GroupMembership
        {
            GroupId = candidateBoardGroupId,
            MemberId = candidateId,
            MembershipYear = lastYear
        });
        await _db.SaveChangesAsync();

        // Force EnqueueTask to throw an exception
        _authOutboxWorkerMock.When(x => x.EnqueueTask(Arg.Any<AuthTaskType>(), Arg.Any<Guid>()))
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

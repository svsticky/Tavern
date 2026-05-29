using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Repositories;
using Backend.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Repositories;

public class GroupMembershipRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly AuthOutboxWorker _authOutboxWorker;
    private readonly GroupMembershipRepository _repository;
    private readonly Guid _userId = Guid.NewGuid();

    public GroupMembershipRepositoryTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TestPostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _authOutboxWorker = Substitute.For<AuthOutboxWorker>(null, null);

        _repository = new GroupMembershipRepository(
            _db,
            _permissionService,
            _authOutboxWorker,
            NullLogger<GroupMembershipRepository>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Member CreateTestMember(Guid id, string email = "test@example.com")
    {
        return new Member
        {
            Id = id,
            StudentNumber = "s" + id.ToString("N").Substring(0, 7),
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PhoneNumber = "1234567890",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede",
            AuthSystemUserId = Guid.NewGuid()
        };
    }

    private Group CreateTestGroup(uint id, string name = "Group Name")
    {
        return new Group
        {
            Id = id,
            Name = name,
            Type = GroupType.Committee
        };
    }

    private RoleAlias CreateTestRoleAlias(uint id, string name = "Role Alias")
    {
        return new RoleAlias
        {
            Id = id,
            Name = name,
            RoleId = id,
            Role = new Role { Id = id, Name = "Role " + id }
        };
    }

    [Fact]
    public async Task GetGroupMemberships_FiltersCorrectly()
    {
        // Arrange
        var m1 = CreateTestMember(Guid.NewGuid(), "m1@example.com");
        var m2 = CreateTestMember(Guid.NewGuid(), "m2@example.com");
        var g1 = CreateTestGroup(1, "Group 1");
        var g2 = CreateTestGroup(2, "Group 2");

        _db.Members.AddRange(m1, m2);
        _db.Groups.AddRange(g1, g2);

        var gm1 = new GroupMembership { Id = 1, Member = m1, Group = g1, MembershipYear = 2024 };
        var gm2 = new GroupMembership { Id = 2, Member = m2, Group = g2, MembershipYear = 2025 };
        _db.GroupMemberships.AddRange(gm1, gm2);
        await _db.SaveChangesAsync();

        var dto = new GetGroupMembershipsDTO { GroupId = 1, MembershipYear = 2024, MemberId = m1.Id };

        // Act
        var result = await _repository.GetGroupMemberships(dto, m1.Id, CancellationToken.None);

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal(1u, list[0].Id);
    }

    [Fact]
    public async Task GetGroupMembership_FoundAndAuthorized_ReturnsDto()
    {
        // Arrange
        var m = CreateTestMember(_userId, "me@example.com");
        var g = CreateTestGroup(1);
        _db.Members.Add(m);
        _db.Groups.Add(g);

        var gm = new GroupMembership { Id = 10, Member = m, Group = g, MembershipYear = 2024 };
        _db.GroupMemberships.Add(gm);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);

        // Act
        var result = await _repository.GetGroupMembership(10, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10u, result.Id);
    }

    [Fact]
    public async Task GetGroupMembership_NotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetGroupMembership(999, _userId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetGroupMembership_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var m = CreateTestMember(otherUserId, "other@example.com");
        var g = CreateTestGroup(1);
        _db.Members.Add(m);
        _db.Groups.Add(g);

        var gm = new GroupMembership { Id = 10, Member = m, Group = g, MembershipYear = 2024 };
        _db.GroupMemberships.Add(gm);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.GetGroupMembership(10, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateGroupMembership_ValidRequest_SavesToDbAndSyncsAuth()
    {
        // Arrange
        var m = CreateTestMember(Guid.NewGuid());
        var g = CreateTestGroup(1);
        var r = CreateTestRoleAlias(1);
        _db.Members.Add(m);
        _db.Groups.Add(g);
        _db.RoleAliases.Add(r);
        await _db.SaveChangesAsync();

        var dto = new PostGroupMembershipDTO
        {
            MemberId = m.Id,
            GroupId = g.Id,
            MembershipYear = 2024,
            RoleAliasId = r.Id
        };

        // Act
        var result = await _repository.CreateGroupMembership(dto, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _db.ChangeTracker.Clear();
        var saved = await _db.GroupMemberships.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal(m.Id, saved.MemberId);
        Assert.Equal(g.Id, saved.GroupId);
        await _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, m.AuthSystemUserId!.Value);
    }

    [Fact]
    public async Task CreateGroupMembership_MemberNotFound_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PostGroupMembershipDTO { MemberId = Guid.NewGuid(), GroupId = 1, MembershipYear = 2024 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.CreateGroupMembership(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteGroupMembership_RemovesFromDbAndSyncsAuth()
    {
        // Arrange
        var m = CreateTestMember(Guid.NewGuid());
        var g = CreateTestGroup(1);
        _db.Members.Add(m);
        _db.Groups.Add(g);

        var gm = new GroupMembership { Id = 5, Member = m, Group = g, MembershipYear = 2024 };
        _db.GroupMemberships.Add(gm);
        await _db.SaveChangesAsync();

        // Act
        await _repository.DeleteGroupMembership(5, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var deleted = await _db.GroupMemberships.FindAsync(5u);
        Assert.Null(deleted);
        await _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, m.AuthSystemUserId!.Value);
    }

    [Fact]
    public async Task DeleteGroupMembership_NotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repository.DeleteGroupMembership(999, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroupMembership_NullPatch_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.PatchGroupMembership(1, _userId, null!, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroupMembership_ModifiesRestrictedFields_ThrowsArgumentException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<GroupMembership>(
            new List<Operation<GroupMembership>>
            {
                new Operation<GroupMembership>("replace", "/GroupId", null, 99u)
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.PatchGroupMembership(1, _userId, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroupMembership_ValidPatch_UpdatesDbAndSyncsAuth()
    {
        // Arrange
        var m = CreateTestMember(Guid.NewGuid());
        var g = CreateTestGroup(1);
        _db.Members.Add(m);
        _db.Groups.Add(g);

        var gm = new GroupMembership { Id = 10, Member = m, Group = g, MembershipYear = 2024 };
        _db.GroupMemberships.Add(gm);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<GroupMembership>();
        patchDoc.Replace(x => x.MembershipYear, 2025u);

        // Act
        await _repository.PatchGroupMembership(10, _userId, patchDoc, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.GroupMemberships.FindAsync(10u);
        Assert.NotNull(updated);
        Assert.Equal(2025u, updated.MembershipYear);
        await _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, m.AuthSystemUserId!.Value);
    }

    [Fact]
    public async Task UpdateGroupMembership_UpdatesRoleAliasAndSyncsAuth()
    {
        // Arrange
        var m = CreateTestMember(Guid.NewGuid());
        var g = CreateTestGroup(1);
        var r1 = CreateTestRoleAlias(1);
        var r2 = CreateTestRoleAlias(2);
        _db.Members.Add(m);
        _db.Groups.Add(g);
        _db.RoleAliases.AddRange(r1, r2);

        var gm = new GroupMembership { Id = 10, Member = m, Group = g, MembershipYear = 2024, RoleAliasId = r1.Id };
        _db.GroupMemberships.Add(gm);
        await _db.SaveChangesAsync();

        var dto = new GroupMembershipUpdateDTO { RoleAliasId = r2.Id };

        // Act
        await _repository.UpdateGroupMembership(10, _userId, dto, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.GroupMemberships.FindAsync(10u);
        Assert.NotNull(updated);
        Assert.Equal(r2.Id, updated.RoleAliasId);
        await _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, m.AuthSystemUserId!.Value);
    }
}

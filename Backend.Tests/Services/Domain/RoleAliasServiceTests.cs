using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Backend.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class RoleAliasServiceTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly AuthOutboxWorker _authOutboxWorker;
    private readonly RoleAliasService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public RoleAliasServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _authOutboxWorker = Substitute.For<AuthOutboxWorker>(null, null);

        _service = new RoleAliasService(
            _db,
            _permissionService,
            _authOutboxWorker,
            NullLogger<RoleAliasService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task GetRoleAliases_ReturnsAllRoleAliases()
    {
        // Arrange
        var role = new Role { Id = 1, Name = "Role" };
        _db.Roles.Add(role);
        _db.RoleAliases.Add(new RoleAlias { Id = 1, Name = "Alias 1", RoleId = 1, Role = role });
        _db.RoleAliases.Add(new RoleAlias { Id = 2, Name = "Alias 2", RoleId = 1, Role = role });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetRoleAliases(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Name == "Alias 1");
    }

    [Fact]
    public async Task GetRoleAlias_Found_ReturnsRoleAlias()
    {
        // Arrange
        var role = new Role { Id = 1, Name = "Role" };
        _db.Roles.Add(role);
        var alias = new RoleAlias { Id = 10, Name = "Alias 10", RoleId = 1, Role = role };
        _db.RoleAliases.Add(alias);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetRoleAlias(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Alias 10", result.Name);
    }

    [Fact]
    public async Task GetRoleAlias_NotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetRoleAlias(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateRoleAlias_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new PostRoleAliasDTO { Name = "Alias", RoleId = 1 };
        _permissionService.When(p => p.EnsurePermission(_userId, Permission.ManageRoles))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateRoleAlias(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRoleAlias_RoleNotFound_ThrowsException()
    {
        // Arrange
        var dto = new PostRoleAliasDTO { Name = "Alias", RoleId = 999 };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.CreateRoleAlias(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRoleAlias_ValidData_CreatesRoleAlias()
    {
        // Arrange
        var role = new Role { Id = 5, Name = "Role 5" };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var dto = new PostRoleAliasDTO { Name = "Alias 5", RoleId = 5 };

        // Act
        var result = await _service.CreateRoleAlias(dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ManageRoles);
        Assert.True(result.Id > 0);
        Assert.Equal("Alias 5", result.Name);
        Assert.Equal(5u, result.RoleId);

        var saved = await _db.RoleAliases.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Alias 5", saved.Name);
    }

    [Fact]
    public async Task DeleteRoleAlias_AliasNotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.DeleteRoleAlias(999u, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRoleAlias_AliasExists_DeletesAndQueuesSync()
    {
        // Arrange
        var role = new Role { Id = 1, Name = "Role" };
        _db.Roles.Add(role);

        var alias = new RoleAlias { Id = 15, Name = "Alias", RoleId = 1, Role = role };
        _db.RoleAliases.Add(alias);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhoneNumber = "0612345678",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(member);

        var gm = new GroupMembership
        {
            Id = 100,
            MemberId = member.Id,
            Member = member,
            GroupId = 10,
            RoleAliasId = 15,
            MembershipYear = 2026
        };
        _db.GroupMemberships.Add(gm);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteRoleAlias(15u, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ManageRoles);
        var deleted = await _db.RoleAliases.FindAsync(15u);
        Assert.Null(deleted);

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, member.Id, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task PatchRoleAlias_NullPatch_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.PatchRoleAlias(1u, null!, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchRoleAlias_ModifiesRestrictedFields_ThrowsArgumentException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<RoleAlias>(
            new List<Operation<RoleAlias>>
            {
                new Operation<RoleAlias>("replace", "/role", null, "modified")
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchRoleAlias(1u, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchRoleAlias_ValidPatch_UpdatesAndQueuesSync()
    {
        // Arrange
        var role = new Role { Id = 1, Name = "Role" };
        _db.Roles.Add(role);

        var alias = new RoleAlias { Id = 20, Name = "Old Name", RoleId = 1, Role = role };
        _db.RoleAliases.Add(alias);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhoneNumber = "0612345678",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(member);

        var gm = new GroupMembership
        {
            Id = 101,
            MemberId = member.Id,
            Member = member,
            GroupId = 10,
            RoleAliasId = 20,
            MembershipYear = 2026
        };
        _db.GroupMemberships.Add(gm);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<RoleAlias>();
        patchDoc.Replace(ra => ra.Name, "New Name");

        // Act
        await _service.PatchRoleAlias(20u, patchDoc, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ManageRoles);
        var updated = await _db.RoleAliases.FindAsync(20u);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, member.Id, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task PatchRoleAlias_ResultsInInvalidState_RollsBackTransaction()
    {
        // Arrange
        var role = new Role { Id = 1, Name = "Role" };
        _db.Roles.Add(role);

        var alias = new RoleAlias { Id = 21, Name = "Old Name", RoleId = 1, Role = role };
        _db.RoleAliases.Add(alias);
        await _db.SaveChangesAsync();

        // Name is [Required(AllowEmptyStrings = false)], so replacing it with an empty string
        // fails StateValidator.Validate and should roll back instead of persisting.
        var patchDoc = new JsonPatchDocument<RoleAlias>();
        patchDoc.Replace(ra => ra.Name, "");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.PatchRoleAlias(21u, patchDoc, _userId, CancellationToken.None));

        _db.ChangeTracker.Clear();
        var unchanged = await _db.RoleAliases.FindAsync(21u);
        Assert.NotNull(unchanged);
        Assert.Equal("Old Name", unchanged.Name);
    }

    [Fact]
    public async Task UpdateRoleAlias_AliasExists_UpdatesAndQueuesSync()
    {
        // Arrange
        var role1 = new Role { Id = 1, Name = "Role 1" };
        var role2 = new Role { Id = 2, Name = "Role 2" };
        _db.Roles.Add(role1);
        _db.Roles.Add(role2);

        var alias = new RoleAlias { Id = 30, Name = "Old Name", RoleId = 1, Role = role1 };
        _db.RoleAliases.Add(alias);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhoneNumber = "0612345678",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(member);

        var gm = new GroupMembership
        {
            Id = 102,
            MemberId = member.Id,
            Member = member,
            GroupId = 10,
            RoleAliasId = 30,
            MembershipYear = 2026
        };
        _db.GroupMemberships.Add(gm);
        await _db.SaveChangesAsync();

        var dto = new RoleAliasUpdateDTO { Name = "New Name", RoleId = 2 };

        // Act
        await _service.UpdateRoleAlias(30u, dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ManageRoles);
        var updated = await _db.RoleAliases.FindAsync(30u);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal(2u, updated.RoleId);

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, member.Id, Arg.Any<PostgresDbContext>());
    }
}

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
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Repositories;

public class RoleRepositoryTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly IPermissionService _permissionService;
    private readonly PostgresDbContext _db;
    private readonly RoleRepository _repository;
    private readonly Guid _userId = Guid.NewGuid();

    public RoleRepositoryTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _repository = new RoleRepository(
            _db,
            _permissionService,
            NullLogger<RoleRepository>.Instance
        );
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task GetRoles_ReturnsAllRoles()
    {
        // Arrange
        _db.Roles.Add(new Role { Id = 1, Name = "Admin" });
        _db.Roles.Add(new Role { Id = 2, Name = "User" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _repository.GetRoles(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Name == "Admin");
    }

    [Fact]
    public async Task GetRole_Found_ReturnsRole()
    {
        // Arrange
        var role = new Role { Id = 10, Name = "Editor" };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        // Act
        var result = await _repository.GetRole(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Editor", result.Name);
    }

    [Fact]
    public async Task GetRole_NotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetRole(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateRole_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new PostRoleDTO { Name = "Admin" };
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.CreateRole(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRole_ValidData_CreatesRole()
    {
        // Arrange
        var dto = new PostRoleDTO { Name = "New Role" };

        // Act
        var result = await _repository.CreateRole(dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.True(result.Id > 0);
        Assert.Equal("New Role", result.Name);

        var saved = await _db.Roles.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("New Role", saved.Name);
    }

    [Fact]
    public async Task DeleteRole_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.DeleteRole(1u, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRole_RoleNotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _repository.DeleteRole(999u, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRole_RoleExists_RemovesFromDatabase()
    {
        // Arrange
        var role = new Role { Id = 15, Name = "Temp" };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        // Act
        await _repository.DeleteRole(15u, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var deleted = await _db.Roles.FindAsync(15u);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task PatchRole_NullPatch_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _repository.PatchRole(1u, null!, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchRole_ModifiesId_ThrowsArgumentException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Role>(
            new List<Operation<Role>>
            {
                new Operation<Role>("replace", "/id", null, 999u)
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.PatchRole(1u, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchRole_RoleNotFound_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Role>();
        patchDoc.Replace(r => r.Name, "New Name");

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _repository.PatchRole(999u, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchRole_ValidPatch_UpdatesDatabase()
    {
        // Arrange
        var role = new Role { Id = 20, Name = "Old Name" };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Role>();
        patchDoc.Replace(r => r.Name, "New Name");

        // Act
        await _repository.PatchRole(20u, patchDoc, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var updated = await _db.Roles.FindAsync(20u);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task UpdateRole_RoleNotFound_ThrowsException()
    {
        // Arrange
        var dto = new RoleUpdateDTO { Name = "Updated" };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _repository.UpdateRole(999u, dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRole_RoleExists_UpdatesDatabase()
    {
        // Arrange
        var role = new Role { Id = 30, Name = "Old" };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var dto = new RoleUpdateDTO { Name = "New Name" };

        // Act
        await _repository.UpdateRole(30u, dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var updated = await _db.Roles.FindAsync(30u);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class StudyServiceTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly IPermissionService _permissionService;
    private readonly PostgresDbContext _db;
    private readonly StudyService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public StudyServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _service = new StudyService(
            _db,
            _permissionService,
            NullLogger<StudyService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task GetStudies_ReturnsAllStudies()
    {
        // Arrange
        _db.Studies.Add(new Study { Id = 1, Title = "CS", NominalDurationYears = 3, Type = StudyType.Bachelor });
        _db.Studies.Add(new Study { Id = 2, Title = "AM", NominalDurationYears = 3, Type = StudyType.Bachelor });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetStudies(new GetStudyDTO(), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Title == "CS");
    }

    [Fact]
    public async Task GetStudies_ExcludesInactiveStudiesByDefault()
    {
        // Arrange
        _db.Studies.Add(new Study { Id = 1, Title = "Active Study", NominalDurationYears = 3, Type = StudyType.Bachelor, Active = true });
        _db.Studies.Add(new Study { Id = 2, Title = "Inactive Study", NominalDurationYears = 3, Type = StudyType.Bachelor, Active = false });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetStudies(new GetStudyDTO(), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Study", result.Single().Title);
    }

    [Fact]
    public async Task GetStudies_IncludeInactive_ReturnsInactiveStudies()
    {
        // Arrange
        _db.Studies.Add(new Study { Id = 1, Title = "Active Study", NominalDurationYears = 3, Type = StudyType.Bachelor, Active = true });
        _db.Studies.Add(new Study { Id = 2, Title = "Inactive Study", NominalDurationYears = 3, Type = StudyType.Bachelor, Active = false });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetStudies(new GetStudyDTO { IncludeInactive = true }, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetStudies_OrdersBachelorsFirstThenMastersThenByTitle()
    {
        // Arrange
        _db.Studies.Add(new Study { Id = 1, Title = "Zoology", NominalDurationYears = 2, Type = StudyType.Master });
        _db.Studies.Add(new Study { Id = 2, Title = "Astronomy", NominalDurationYears = 1, Type = StudyType.Master });
        _db.Studies.Add(new Study { Id = 3, Title = "Biology", NominalDurationYears = 3, Type = StudyType.Bachelor });
        _db.Studies.Add(new Study { Id = 4, Title = "Anthropology", NominalDurationYears = 3, Type = StudyType.Bachelor });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetStudies(new GetStudyDTO(), CancellationToken.None);

        // Assert
        Assert.Equal(new[] { "Anthropology", "Biology", "Astronomy", "Zoology" }, result.Select(s => s.Title));
    }

    [Fact]
    public async Task GetStudy_Found_ReturnsStudy()
    {
        // Arrange
        var study = new Study { Id = 10, Title = "CS", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetStudy(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CS", result.Title);
    }

    [Fact]
    public async Task GetStudy_NotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetStudy(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateStudy_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new PostStudyDTO { Title = "CS", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateStudy(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateStudy_ValidData_CreatesStudy()
    {
        // Arrange
        var dto = new PostStudyDTO { Title = "Computer Science", NominalDurationYears = 3, Type = StudyType.Bachelor };

        // Act
        var result = await _service.CreateStudy(dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.True(result.Id > 0);
        Assert.Equal("Computer Science", result.Title);

        var saved = await _db.Studies.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Computer Science", saved.Title);
    }

    [Fact]
    public async Task DeleteStudy_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.DeleteStudy(1u, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteStudy_StudyNotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.DeleteStudy(999u, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteStudy_StudyExists_RemovesFromDatabase()
    {
        // Arrange
        var study = new Study { Id = 15, Title = "CS", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteStudy(15u, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var deleted = await _db.Studies.FindAsync(15u);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task PatchStudy_NullPatch_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.PatchStudy(1u, null!, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchStudy_ModifiesId_ThrowsArgumentException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Study>(
            new List<Operation<Study>>
            {
                new Operation<Study>("replace", "/id", null, 999u)
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchStudy(1u, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchStudy_StudyNotFound_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Study>();
        patchDoc.Replace(s => s.Title, "New Title");

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.PatchStudy(999u, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchStudy_ValidPatch_UpdatesDatabase()
    {
        // Arrange
        var study = new Study { Id = 20, Title = "Old Title", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Study>();
        patchDoc.Replace(s => s.Title, "New Title");

        // Act
        await _service.PatchStudy(20u, patchDoc, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var updated = await _db.Studies.FindAsync(20u);
        Assert.NotNull(updated);
        Assert.Equal("New Title", updated.Title);
    }

    [Fact]
    public async Task UpdateStudy_StudyNotFound_ThrowsException()
    {
        // Arrange
        var dto = new StudyUpdateDTO { Title = "CS", NominalDurationYears = 3, Type = StudyType.Bachelor, Active = true };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.UpdateStudy(999u, dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStudy_StudyExists_UpdatesDatabase()
    {
        // Arrange
        var study = new Study { Id = 30, Title = "Old", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        var dto = new StudyUpdateDTO { Title = "New Title", NominalDurationYears = 4, Type = StudyType.Master, Active = false };

        // Act
        await _service.UpdateStudy(30u, dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var updated = await _db.Studies.FindAsync(30u);
        Assert.NotNull(updated);
        Assert.Equal("New Title", updated.Title);
        Assert.Equal(4u, updated.NominalDurationYears);
        Assert.False(updated.Active);
        Assert.Equal(StudyType.Master, updated.Type);
    }
}

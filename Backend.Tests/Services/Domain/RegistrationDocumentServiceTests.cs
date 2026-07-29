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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class RegistrationDocumentServiceTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly RegistrationDocumentService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public RegistrationDocumentServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new PostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _service = new RegistrationDocumentService(
            _db,
            _permissionService,
            NullLogger<RegistrationDocumentService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task GetRegistrationDocuments_ReturnsSortedDocuments()
    {
        _db.RegistrationDocuments.Add(new RegistrationDocument { Id = 1, NameDutch = "A", NameEnglish = "A", Url = "http://a", SortOrder = 2 });
        _db.RegistrationDocuments.Add(new RegistrationDocument { Id = 2, NameDutch = "B", NameEnglish = "B", Url = "http://b", SortOrder = 1 });
        await _db.SaveChangesAsync();

        var result = (await _service.GetRegistrationDocuments(CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("B", result[0].NameDutch);
        Assert.Equal("A", result[1].NameDutch);
    }

    [Fact]
    public async Task CreateRegistrationDocument_ValidData_CreatesDocument()
    {
        var dto = new PostRegistrationDocumentDTO { NameDutch = "NL", NameEnglish = "EN", Url = "http://doc.nl", SortOrder = 1 };

        var result = await _service.CreateRegistrationDocument(dto, _userId, CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.Equal("NL", result.NameDutch);

        var saved = await _db.RegistrationDocuments.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("NL", saved.NameDutch);
    }

    [Fact]
    public async Task UpdateRegistrationDocument_DocumentExists_UpdatesDatabase()
    {
        var doc = new RegistrationDocument { Id = 1, NameDutch = "Old", NameEnglish = "Old", Url = "http://old", SortOrder = 1 };
        _db.RegistrationDocuments.Add(doc);
        await _db.SaveChangesAsync();

        var dto = new RegistrationDocumentUpdateDTO { NameDutch = "New", NameEnglish = "New", Url = "http://new", SortOrder = 2 };

        await _service.UpdateRegistrationDocument(1, dto, _userId, CancellationToken.None);

        var updated = await _db.RegistrationDocuments.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("New", updated.NameDutch);
        Assert.Equal("http://new", updated.Url);
    }

    [Fact]
    public async Task DeleteRegistrationDocument_DocumentExists_DeletesFromDatabase()
    {
        var doc = new RegistrationDocument { Id = 1, NameDutch = "A", NameEnglish = "A", Url = "http://a", SortOrder = 1 };
        _db.RegistrationDocuments.Add(doc);
        await _db.SaveChangesAsync();

        await _service.DeleteRegistrationDocument(1, _userId, CancellationToken.None);

        var deleted = await _db.RegistrationDocuments.FindAsync(1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetRegistrationDocument_Found_ReturnsDto()
    {
        var doc = new RegistrationDocument { Id = 5, NameDutch = "NL", NameEnglish = "EN", Url = "http://doc.nl", SortOrder = 1 };
        _db.RegistrationDocuments.Add(doc);
        await _db.SaveChangesAsync();

        var result = await _service.GetRegistrationDocument(5, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal("NL", result.NameDutch);
    }

    [Fact]
    public async Task GetRegistrationDocument_NotFound_ReturnsNull()
    {
        var result = await _service.GetRegistrationDocument(999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRegistrationDocument_NotFound_ThrowsKeyNotFoundException()
    {
        var dto = new RegistrationDocumentUpdateDTO { NameDutch = "New", NameEnglish = "New", Url = "http://new", SortOrder = 2 };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateRegistrationDocument(999, dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRegistrationDocument_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteRegistrationDocument(999, _userId, CancellationToken.None));
    }
}

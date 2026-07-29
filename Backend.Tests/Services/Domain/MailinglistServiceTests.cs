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

public class MailinglistServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly MailinglistService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public MailinglistServiceTests()
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TestPostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _service = new MailinglistService(
            _db,
            _permissionService,
            NullLogger<MailinglistService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetMailinglists_ReturnsAllMailinglists()
    {
        // Arrange
        _db.Mailinglists.Add(new Mailinglist { Id = 1, Name = "List 1", ServiceId = "s1", BitValue = 1 });
        _db.Mailinglists.Add(new Mailinglist { Id = 2, Name = "List 2", ServiceId = "s2", BitValue = 2 });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetMailinglists(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, m => m.Name == "List 1");
    }

    [Fact]
    public async Task GetMailinglist_Found_ReturnsMailinglist()
    {
        // Arrange
        var list = new Mailinglist { Id = 10, Name = "List 10", ServiceId = "s10", BitValue = 10 };
        _db.Mailinglists.Add(list);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetMailinglist(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("List 10", result.Name);
    }

    [Fact]
    public async Task GetMailinglist_NotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetMailinglist(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateMailinglist_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "List", ServiceId = "s" };
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateMailinglist(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMailinglist_NoLists_SetsBitValueTo1()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "Newsletter", ServiceId = "news" };

        // Act
        var result = await _service.CreateMailinglist(dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.Equal(1u, result.BitValue);
        Assert.Equal("Newsletter", result.Name);
    }

    [Fact]
    public async Task CreateMailinglist_ExistingLists_ShiftsBitValue()
    {
        // Arrange
        _db.Mailinglists.Add(new Mailinglist { Id = 1, Name = "List 1", ServiceId = "s1", BitValue = 4 });
        await _db.SaveChangesAsync();

        var dto = new PostMailinglistDTO { Name = "List 2", ServiceId = "s2" };

        // Act
        var result = await _service.CreateMailinglist(dto, _userId, CancellationToken.None);

        // Assert
        Assert.Equal(8u, result.BitValue); // 4 shifted left by 1 is 8
    }

    [Fact]
    public async Task UpdateMailinglist_MailinglistNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "New", ServiceId = "new" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateMailinglist(999, dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMailinglist_MailinglistExists_UpdatesDatabase()
    {
        // Arrange
        var list = new Mailinglist { Id = 100, Name = "Old Name", ServiceId = "old", BitValue = 1 };
        _db.Mailinglists.Add(list);
        await _db.SaveChangesAsync();

        var dto = new PostMailinglistDTO { Name = "New Name", ServiceId = "new" };

        // Act
        await _service.UpdateMailinglist(100, dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var updated = await _db.Mailinglists.FindAsync(100);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("new", updated.ServiceId);
    }

    [Fact]
    public async Task DeleteMailinglist_MailinglistExists_RemovesAndClearsMemberSubscriptions()
    {
        // Arrange
        var list = new Mailinglist { Id = 5, Name = "To Delete", ServiceId = "del", BitValue = 2 };
        _db.Mailinglists.Add(list);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhoneNumber = "0612345678",
            Street = "Main St",
            HouseNumber = "1",
            MailSubscriptions = 7, // binary 111, has bit 2 (second bit)
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteMailinglist(5, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        
        _db.ChangeTracker.Clear();
        var deleted = await _db.Mailinglists.FindAsync(5);
        Assert.Null(deleted);

        var updatedMember = await _db.Members.FindAsync(member.Id);
        Assert.NotNull(updatedMember);
        // BitValue is 2. 7 (111) & ~2 (101) = 5 (101)
        Assert.Equal(5u, updatedMember.MailSubscriptions);
    }

    [Fact]
    public async Task PatchMailinglist_NullPatch_ThrowsArgumentException()
    {
        // Arrange
        var list = new Mailinglist { Id = 1, Name = "List", ServiceId = "s", BitValue = 1 };
        _db.Mailinglists.Add(list);
        await _db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchMailinglist(1, null!, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchMailinglist_ModifiesRestrictedFields_ThrowsArgumentException()
    {
        // Arrange
        var list = new Mailinglist { Id = 1, Name = "List", ServiceId = "s", BitValue = 1 };
        _db.Mailinglists.Add(list);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Mailinglist>(
            new List<Operation<Mailinglist>>
            {
                new Operation<Mailinglist>("replace", "/BitValue", null, 999u)
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchMailinglist(1, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchMailinglist_ValidPatch_UpdatesDatabase()
    {
        // Arrange
        var list = new Mailinglist { Id = 20, Name = "Old Name", ServiceId = "old", BitValue = 2 };
        _db.Mailinglists.Add(list);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Mailinglist>();
        patchDoc.Replace(m => m.Name, "New Name");

        // Act
        await _service.PatchMailinglist(20, patchDoc, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var updated = await _db.Mailinglists.FindAsync(20);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
    }
}

public class TestPostgresDbContext : PostgresDbContext
{
    public TestPostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MembershipPayment>()
            .HasIndex(p => p.MemberId)
            .IsUnique()
            .HasFilter("MemberId IS NOT NULL");
    }
}

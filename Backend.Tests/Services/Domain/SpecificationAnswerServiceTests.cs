using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

public class SpecificationAnswerServiceTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly IPermissionService _permissionService;
    private readonly SpecificationAnswerService _service;
    private readonly PostgresDbContext _db;

    public SpecificationAnswerServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _service = new SpecificationAnswerService(
            _db,
            _permissionService,
            NullLogger<SpecificationAnswerService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private SpecificationAnswer CreateTestAnswer(uint id, Guid memberId, string answerText, DateTimeOffset? deadline = null)
    {
        var activity = new Activity
        {
            Id = 10,
            Name = "Test Activity",
            Location = "Enschede",
            PosterPath = "poster.png",
            PosterFileName = "poster.png",
            EnrollmentDeadline = deadline,
            DateTimeStart = DateTimeOffset.UtcNow.AddDays(1),
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5),
            DutchDescription = "Beschrijving",
            EnglishDescription = "Description"
        };

        var question = new SpecificationQuestion
        {
            Id = 20,
            ActivityId = 10,
            Activity = activity,
            QuestionDutch = "Vraag",
            QuestionEnglish = "Question",
            Type = QuestionType.String
        };

        var member = new Member
        {
            Id = memberId,
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

        return new SpecificationAnswer
        {
            Id = id,
            MemberId = memberId,
            Member = member,
            Answer = answerText,
            Question = question,
            SpecificationQuestionId = 20
        };
    }

    [Fact]
    public async Task PatchSpecificationAnswersAsync_OtherUserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var answer = CreateTestAnswer(1, memberId, "Old Answer");
        _db.SpecificationAnswers.Add(answer);
        await _db.SaveChangesAsync();

        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(otherUserId))
            .Do(x => throw new UnauthorizedAccessException());

        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();
        patchDoc.Replace(a => a.Answer, "New Answer");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchSpecificationAnswersAsync(memberId, 1, patchDoc, otherUserId));
    }

    [Fact]
    public async Task PatchSpecificationAnswersAsync_PatchDocNull_ThrowsArgumentException()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchSpecificationAnswersAsync(memberId, 1, null!, memberId));
    }

    [Fact]
    public async Task PatchSpecificationAnswersAsync_ModifiesRestrictedFields_ThrowsArgumentException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var patchDoc = new JsonPatchDocument<SpecificationAnswer>(
            new List<Operation<SpecificationAnswer>>
            {
                new Operation<SpecificationAnswer>("replace", "/id", null, 999u)
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchSpecificationAnswersAsync(memberId, 1, patchDoc, memberId));
    }

    [Fact]
    public async Task PatchSpecificationAnswersAsync_AnswerNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();
        patchDoc.Replace(a => a.Answer, "New Answer");

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.PatchSpecificationAnswersAsync(memberId, 999, patchDoc, memberId));
    }

    [Fact]
    public async Task PatchSpecificationAnswersAsync_DifferentAnswerOwner_ThrowsUnauthorized()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var answer = CreateTestAnswer(1, memberId, "Old Answer");
        _db.SpecificationAnswers.Add(answer);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();
        patchDoc.Replace(a => a.Answer, "New Answer");

        // Act & Assert (fromUserId is Guid.NewGuid(), different from answer.MemberId)
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchSpecificationAnswersAsync(Guid.NewGuid(), 1, patchDoc, Guid.NewGuid()));
    }

    [Fact]
    public async Task PatchSpecificationAnswersAsync_AfterDeadline_ThrowsInvalidOperationException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var pastDeadline = DateTimeOffset.UtcNow.AddMinutes(-5);
        var answer = CreateTestAnswer(1, memberId, "Old Answer", pastDeadline);
        _db.SpecificationAnswers.Add(answer);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();
        patchDoc.Replace(a => a.Answer, "New Answer");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.PatchSpecificationAnswersAsync(memberId, 1, patchDoc, memberId));
    }

    [Fact]
    public async Task PatchSpecificationAnswersAsync_UnsupportedPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var answer = CreateTestAnswer(1, memberId, "Old Answer");
        _db.SpecificationAnswers.Add(answer);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<SpecificationAnswer>(
            new List<Operation<SpecificationAnswer>>
            {
                new Operation<SpecificationAnswer>("replace", "/nonexistent", null, Guid.NewGuid())
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.PatchSpecificationAnswersAsync(memberId, 1, patchDoc, memberId));
    }

    [Fact]
    public async Task PatchSpecificationAnswersAsync_SuccessfulPatch_UpdatesDatabase()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var answer = CreateTestAnswer(1, memberId, "Old Answer");
        _db.SpecificationAnswers.Add(answer);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();
        patchDoc.Replace(a => a.Answer, "New Answer");

        // Act
        await _service.PatchSpecificationAnswersAsync(memberId, 1, patchDoc, memberId);

        // Assert
        var updated = await _db.SpecificationAnswers.FindAsync(1u);
        Assert.NotNull(updated);
        Assert.Equal("New Answer", updated.Answer);
    }
}

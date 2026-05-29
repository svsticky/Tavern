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

public class StudyEnrollmentRepositoryTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly StudyEnrollmentRepository _repository;
    private readonly Guid _userId = Guid.NewGuid();

    public StudyEnrollmentRepositoryTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _repository = new StudyEnrollmentRepository(
            _db,
            _permissionService,
            NullLogger<StudyEnrollmentRepository>.Instance
        );
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private (Member Member, Study Study) SetUpDependencies()
    {
        var member = new Member
        {
            Id = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "John",
            LastName = "Doe",
            Email = $"{Guid.NewGuid()}@example.com",
            PhoneNumber = "0612345678",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };

        var study = new Study
        {
            Id = 1,
            Title = "Computer Science",
            NominalDurationYears = 3,
            Type = StudyType.Bachelor
        };

        _db.Members.Add(member);
        _db.Studies.Add(study);
        _db.SaveChanges();

        return (member, study);
    }

    [Fact]
    public async Task GetStudyEnrollments_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        var dto = new GetStudyEnrollmentsDTO();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.GetStudyEnrollments(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task GetStudyEnrollments_BoardUser_ReturnsEnrollments()
    {
        // Arrange
        var (member, study) = SetUpDependencies();
        var se = new StudyEnrollment
        {
            Id = 1,
            MemberId = member.Id,
            Member = member,
            StudyId = study.Id,
            Study = study,
            EnrollmentDate = DateTimeOffset.UtcNow,
            Status = StudyStatus.Enrolled
        };
        _db.StudyEnrollments.Add(se);
        await _db.SaveChangesAsync();

        var dto = new GetStudyEnrollmentsDTO();

        // Act
        var result = await _repository.GetStudyEnrollments(dto, _userId, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("John Doe", result[0].MemberName);
        Assert.Equal("Computer Science", result[0].StudyTitle);
    }

    [Fact]
    public async Task GetStudyEnrollment_NotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetStudyEnrollment(999u, _userId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStudyEnrollment_OwnEnrollment_ReturnsEnrollmentWithoutPermissionCheck()
    {
        // Arrange
        var (member, study) = SetUpDependencies();
        var se = new StudyEnrollment
        {
            Id = 2,
            MemberId = member.Id,
            Member = member,
            StudyId = study.Id,
            Study = study,
            EnrollmentDate = DateTimeOffset.UtcNow,
            Status = StudyStatus.Enrolled
        };
        _db.StudyEnrollments.Add(se);
        await _db.SaveChangesAsync();

        // Act
        var result = await _repository.GetStudyEnrollment(2u, member.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _permissionService.DidNotReceiveWithAnyArgs().EnsureBoardOrCandidateBoardMember(default);
    }

    [Fact]
    public async Task GetStudyEnrollment_OtherUserEnrollment_EnsuresBoardMember()
    {
        // Arrange
        var (member, study) = SetUpDependencies();
        var se = new StudyEnrollment
        {
            Id = 3,
            MemberId = member.Id,
            Member = member,
            StudyId = study.Id,
            Study = study,
            EnrollmentDate = DateTimeOffset.UtcNow,
            Status = StudyStatus.Enrolled
        };
        _db.StudyEnrollments.Add(se);
        await _db.SaveChangesAsync();

        // Act
        var result = await _repository.GetStudyEnrollment(3u, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
    }

    [Fact]
    public async Task CreateStudyEnrollment_MemberNotFound_ThrowsException()
    {
        // Arrange
        var dto = new PostStudyEnrollmentDTO
        {
            MemberId = Guid.NewGuid(),
            StudyId = 1,
            EnrollmentDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _repository.CreateStudyEnrollment(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateStudyEnrollment_StudyNotFound_ThrowsException()
    {
        // Arrange
        var (member, _) = SetUpDependencies();
        var dto = new PostStudyEnrollmentDTO
        {
            MemberId = member.Id,
            StudyId = 999,
            EnrollmentDate = DateTimeOffset.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _repository.CreateStudyEnrollment(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateStudyEnrollment_ValidData_CreatesAndReturnsDto()
    {
        // Arrange
        var (member, study) = SetUpDependencies();
        var dto = new PostStudyEnrollmentDTO
        {
            MemberId = member.Id,
            StudyId = study.Id,
            EnrollmentDate = DateTimeOffset.UtcNow,
            Status = StudyStatus.Enrolled
        };

        // Act
        var result = await _repository.CreateStudyEnrollment(dto, _userId, CancellationToken.None);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("John Doe", result.MemberName);
        Assert.Equal("Computer Science", result.StudyTitle);
    }

    [Fact]
    public async Task DeleteStudyEnrollment_NotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _repository.DeleteStudyEnrollment(999u, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteStudyEnrollment_Exists_DeletesFromDatabase()
    {
        // Arrange
        var (member, study) = SetUpDependencies();
        var se = new StudyEnrollment
        {
            Id = 5,
            MemberId = member.Id,
            Member = member,
            StudyId = study.Id,
            Study = study,
            EnrollmentDate = DateTimeOffset.UtcNow,
            Status = StudyStatus.Enrolled
        };
        _db.StudyEnrollments.Add(se);
        await _db.SaveChangesAsync();

        // Act
        await _repository.DeleteStudyEnrollment(5u, _userId, CancellationToken.None);

        // Assert
        var deleted = await _db.StudyEnrollments.FindAsync(5u);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task PatchStudyEnrollment_NullPatch_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.PatchStudyEnrollment(1u, null!, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchStudyEnrollment_ModifiesRestrictedFields_ThrowsArgumentException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<StudyEnrollment>(
            new List<Operation<StudyEnrollment>>
            {
                new Operation<StudyEnrollment>("replace", "/studyId", null, 999u)
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _repository.PatchStudyEnrollment(1u, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchStudyEnrollment_DifferentUserAndDurationPassed_DoesNotEnsureBoard()
    {
        // Arrange
        var (member, study) = SetUpDependencies();
        // Nominal duration is 3 years. Enrollment date is 4 years ago.
        var enrollmentDate = DateTimeOffset.UtcNow.AddYears(-4);
        var se = new StudyEnrollment
        {
            Id = 10,
            MemberId = member.Id,
            Member = member,
            StudyId = study.Id,
            Study = study,
            EnrollmentDate = enrollmentDate,
            Status = StudyStatus.Enrolled
        };
        _db.StudyEnrollments.Add(se);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<StudyEnrollment>();
        patchDoc.Replace(e => e.Status, StudyStatus.Completed);

        // Act
        await _repository.PatchStudyEnrollment(10u, patchDoc, member.Id, CancellationToken.None);

        // Assert
        var updated = await _db.StudyEnrollments.FindAsync(10u);
        Assert.NotNull(updated);
        Assert.Equal(StudyStatus.Completed, updated.Status);
        Assert.NotNull(updated.CompletionDate);

        _permissionService.DidNotReceiveWithAnyArgs().EnsureBoardOrCandidateBoardMember(default);
    }

    [Fact]
    public async Task PatchStudyEnrollment_OwnUserButDurationNotPassed_EnsuresBoard()
    {
        // Arrange
        var (member, study) = SetUpDependencies();
        // Enrollment date is recent
        var enrollmentDate = DateTimeOffset.UtcNow.AddMonths(-1);
        var se = new StudyEnrollment
        {
            Id = 11,
            MemberId = member.Id,
            Member = member,
            StudyId = study.Id,
            Study = study,
            EnrollmentDate = enrollmentDate,
            Status = StudyStatus.Enrolled
        };
        _db.StudyEnrollments.Add(se);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<StudyEnrollment>();
        patchDoc.Replace(e => e.Status, StudyStatus.DroppedOut);

        // Act
        await _repository.PatchStudyEnrollment(11u, patchDoc, member.Id, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(member.Id);
        var updated = await _db.StudyEnrollments.FindAsync(11u);
        Assert.NotNull(updated);
        Assert.Equal(StudyStatus.DroppedOut, updated.Status);
        Assert.NotNull(updated.CompletionDate);
    }
}

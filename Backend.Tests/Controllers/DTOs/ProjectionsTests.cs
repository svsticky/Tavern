using System;
using System.Collections.Generic;
using Backend.Controllers.DTOs;
using Backend.Models;
using Backend.Models.Domain;
using Xunit;

namespace Backend.Tests.Controllers.DTOs;

public class ProjectionsTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private Member CreateDummyMember()
    {
        return new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            StudentNumber = "s1111111",
            PhoneNumber = "+31611111111",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1111AA",
            City = "Enschede"
        };
    }

    [Fact]
    public void GroupProjections_ToDto_Works()
    {
        var group = new Group
        {
            Id = 123u,
            Name = "Web Committee",
            Type = GroupType.Committee,
            Active = true,
            GroupPicturePath = "/path/img.png"
        };

        var dto = GroupResponseDTO.ToDto().Compile()(group);

        Assert.Equal(123u, dto.Id);
        Assert.Equal("Web Committee", dto.Name);
        Assert.Equal(GroupType.Committee, dto.Type);
        Assert.True(dto.Active);
        Assert.Equal("/path/img.png", dto.GroupPicturePath);
    }

    [Fact]
    public void SpecificationQuestionProjections_ToDto_Works()
    {
        var question = new SpecificationQuestion
        {
            Id = 10u,
            QuestionDutch = "NL Q",
            QuestionEnglish = "EN Q",
            Type = QuestionType.MultipleChoice,
            IsMandatory = true,
            IsPublic = true,
            Options = "A;B"
        };

        var dto = GetSpecificationQuestionResponseDTO.ToDto().Compile()(question);

        Assert.Equal(10u, dto.Id);
        Assert.Equal("NL Q", dto.QuestionDutch);
        Assert.Equal("EN Q", dto.QuestionEnglish);
        Assert.Equal(QuestionType.MultipleChoice, dto.Type);
        Assert.True(dto.IsMandatory);
        Assert.True(dto.IsPublic);
        Assert.Equal(new List<string> { "A", "B" }, dto.Options);
    }

    [Fact]
    public void SpecificationAnswerProjections_ToDto_Works()
    {
        var answer = new SpecificationAnswer
        {
            Id = 1u,
            SpecificationQuestionId = 5u,
            MemberId = _userId,
            Answer = "My Answer"
        };

        var dto = SpecificationAnswerResponseDTO.ToDto().Compile()(answer);

        Assert.Equal(5u, dto.QuestionId);
        Assert.Equal(1u, dto.AnswerId);
        Assert.Equal("My Answer", dto.Answer);
    }

    [Fact]
    public void StudyEnrollmentProjections_ToDto_Works()
    {
        var studyEnrollment = new StudyEnrollment
        {
            Id = 8u,
            MemberId = _userId,
            StudyId = 2u,
            EnrollmentDate = DateTimeOffset.UtcNow,
            CompletionDate = null,
            Status = StudyStatus.Enrolled,
            Study = new Study
            {
                Id = 2u,
                Title = "Maths",
                NominalDurationYears = 3,
                Type = StudyType.Bachelor
            }
        };

        var dto = StudyEnrollmentResponseDTO.ToDto().Compile()(studyEnrollment);

        Assert.Equal(8u, dto.Id);
        Assert.Equal(2u, dto.StudyId);
        Assert.Equal("Maths", dto.StudyTitle);
        Assert.Equal(StudyType.Bachelor, dto.StudyType);
        Assert.Equal(StudyStatus.Enrolled, dto.Status);
    }

    [Fact]
    public void GroupMembershipProjections_ToDto_Works()
    {
        var member = CreateDummyMember();
        var membership = new GroupMembership
        {
            GroupId = 1u,
            MemberId = member.Id,
            MembershipYear = 2026,
            RoleAlias = new RoleAlias { Id = 9u, RoleId = 2u, Name = "Chairperson" },
            Member = member
        };

        var dto = GroupMembershipResponseDTO.ToDto(_userId, hasViewMembers: true).Compile()(membership);

        Assert.Equal(1u, dto.GroupId);
        Assert.Equal(member.Id, dto.MemberId);
        Assert.Equal("Alice Smith", dto.MemberName);
        Assert.Equal("Chairperson", dto.RoleAliasName);
    }

    [Fact]
    public void MemberProjections_ToDto_Works()
    {
        var member = CreateDummyMember();
        member.Begunstiger = true;
        member.StudyEnrollments = new List<StudyEnrollment>
        {
            new StudyEnrollment
            {
                Id = 1,
                StudyId = 1,
                Study = new Study { Id = 1, Title = "CS", Type = StudyType.Bachelor, NominalDurationYears = 3 },
                Status = StudyStatus.Enrolled
            }
        };

        var dto = MemberResponseDTO.ToDto(_userId, hasViewMembers: true, isBoardOrCandidateBoard: true).Compile()(member);

        Assert.Equal(member.Id, dto.Id);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
        Assert.True(dto.Begunstiger);
        Assert.Single(dto.StudyEnrollments ?? []);
    }

    [Fact]
    public void EnrollmentProjections_ToDto_Works()
    {
        var member = CreateDummyMember();
        var enrollment = new Enrollment
        {
            ActivityId = 1u,
            MemberId = member.Id,
            Price = 10.00m,
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow,
            SpecificationAnswers = new List<SpecificationAnswer>(),
            Member = member,
            Activity = new Activity
            {
                Id = 1u,
                Name = "Party",
                Price = 10.00m,
                DutchDescription = "NL desc",
                EnglishDescription = "EN desc",
                DateTimeStart = DateTime.UtcNow.AddDays(1),
                DateTimeEnd = DateTime.UtcNow.AddDays(2),
                Location = "Tavern",
                AreParticipantsVisible = true,
                ShowInKoala = true,
                IsEnrollable = true,
                AllowedAudience = TargetAudience.All,
                PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5),
                Enrollments = new List<Enrollment>(),
                SpecificationQuestions = new List<SpecificationQuestion>()
            }
        };

        var dto = EnrollmentResponseDTO.ToDto(_userId, hasViewMembers: true, hasViewFinances: true, isBoardOrCandidateBoard: true, includeActivity: true).Compile()(enrollment);

        Assert.NotNull(dto.Activity);
        Assert.Equal(1u, dto.Activity.Id);
        Assert.Equal(member.Id, dto.Member?.Id);
        Assert.Equal(10.00m, dto.Price);
    }

    [Fact]
    public void ActivityProjections_ToDto_Works()
    {
        var activity = new Activity
        {
            Id = 2u,
            Name = "Party",
            Price = 5.00m,
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(3),
            Location = "Tavern",
            DutchDescription = "NL desc",
            EnglishDescription = "EN desc",
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7),
            AreParticipantsVisible = true,
            Enrollments = new List<Enrollment>(),
            SpecificationQuestions = new List<SpecificationQuestion>()
        };

        var dto = ActivityResponseDTO.ToDto(_userId, hasViewFinances: true, hasViewMembers: true, isBoardOrCandidateBoard: true).Compile()(activity);

        Assert.Equal(2u, dto.Id);
        Assert.Equal("Party", dto.Name);
        Assert.Equal(5.00m, dto.Price);
        Assert.True(dto.AreParticipantsVisible);
    }

    [Fact]
    public void ActivityProjections_ToDto_HidesFinancialFieldsForNonBoardMembers()
    {
        var activity = new Activity
        {
            Id = 3u,
            Name = "Party",
            Price = 5.00m,
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(3),
            Location = "Tavern",
            DutchDescription = "NL desc",
            EnglishDescription = "EN desc",
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7),
            AreParticipantsVisible = true,
            Enrollments = new List<Enrollment>(),
            SpecificationQuestions = new List<SpecificationQuestion>(),
            VatRate = 21u,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123"
        };

        var boardDto = ActivityResponseDTO.ToDto(_userId, hasViewFinances: true, hasViewMembers: true, isBoardOrCandidateBoard: true).Compile()(activity);
        var nonBoardDto = ActivityResponseDTO.ToDto(_userId, hasViewFinances: false, hasViewMembers: false, isBoardOrCandidateBoard: false).Compile()(activity);

        Assert.Equal(21u, boardDto.VatRate);
        Assert.Equal("GL123", boardDto.GLAccountId);
        Assert.Equal("CC123", boardDto.CostCenterId);
        Assert.Equal("CU123", boardDto.CostUnitId);

        Assert.Null(nonBoardDto.VatRate);
        Assert.Null(nonBoardDto.GLAccountId);
        Assert.Null(nonBoardDto.CostCenterId);
        Assert.Null(nonBoardDto.CostUnitId);
    }
}

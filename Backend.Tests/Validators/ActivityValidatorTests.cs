using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Backend.Tests.Validators;

public class ActivityValidatorTests
{
    private class TestActivityDTO : BaseActivityDTO<SpecificationQuestionDTO>
    {
    }

    private readonly IPermissionService _permissionServiceMock;
    private readonly Guid _userId;

    public ActivityValidatorTests()
    {
        _permissionServiceMock = Substitute.For<IPermissionService>();
        _userId = Guid.NewGuid();
    }

    [Fact]
    public void ValidateRequest_Valid_DoesNotThrow()
    {
        var dto = new TestActivityDTO
        {
            Name = "Drinks",
            DutchDescription = "NL Desc",
            EnglishDescription = "EN Desc",
            Location = "Tavern",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = false,
            AreParticipantsVisible = false,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            OrganizerId = 1,
            PaymentDeadline = null,
            EnrollOpenDate = null
        };
        _permissionServiceMock.IsInGroupInCurrentYear(_userId, 1).Returns(true);

        var exception = Record.Exception(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateRequest_EndBeforeStart_ThrowsArgumentException()
    {
        var dto = new TestActivityDTO
        {
            Name = "Drinks",
            DutchDescription = "NL Desc",
            EnglishDescription = "EN Desc",
            Location = "Tavern",
            DateTimeStart = DateTimeOffset.UtcNow.AddHours(2),
            DateTimeEnd = DateTimeOffset.UtcNow,
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = false,
            AreParticipantsVisible = false,
            IsAdultOnly = false,
            IsWeeklyDrinks = false
        };

        var exception = Assert.Throws<ArgumentException>(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));
        Assert.Equal("Activity cannot end before it starts.", exception.Message);
    }

    [Fact]
    public void ValidateRequest_EndEqualToStart_ThrowsArgumentException()
    {
        // A zero-duration activity has no meaningful representation in a calendar feed, so it is rejected
        // outright rather than published as an instantaneous event.
        var moment = DateTimeOffset.UtcNow;
        var dto = new TestActivityDTO
        {
            Name = "Drinks",
            DutchDescription = "NL Desc",
            EnglishDescription = "EN Desc",
            Location = "Tavern",
            DateTimeStart = moment,
            DateTimeEnd = moment,
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = false,
            AreParticipantsVisible = false,
            IsAdultOnly = false,
            IsWeeklyDrinks = false
        };

        var exception = Assert.Throws<ArgumentException>(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));
        Assert.Equal("Activity cannot end before it starts.", exception.Message);
    }

    [Fact]
    public void ValidateRequest_ShowInKoala_ChecksBoardPermission()
    {
        var dto = new TestActivityDTO
        {
            Name = "Drinks",
            DutchDescription = "NL Desc",
            EnglishDescription = "EN Desc",
            Location = "Tavern",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            ShowInKoala = true, // triggers board check
            ShowOnWebsite = false,
            IsEnrollable = false,
            AreParticipantsVisible = false,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            OrganizerId = 1
        };
        _permissionServiceMock.IsInGroupInCurrentYear(_userId, 1).Returns(true);
        _permissionServiceMock.When(x => x.EnsurePermission(_userId, Permission.EditAllActivities, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        Assert.Throws<UnauthorizedAccessException>(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));
    }

    [Fact]
    public void ValidateRequest_InvalidPosterFile_ThrowsArgumentException()
    {
        var posterFile = Substitute.For<IFormFile>();
        posterFile.FileName.Returns("doc.pdf");
        posterFile.ContentType.Returns("application/pdf");

        var dto = new TestActivityDTO
        {
            Name = "Drinks",
            DutchDescription = "NL Desc",
            EnglishDescription = "EN Desc",
            Location = "Tavern",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = false,
            AreParticipantsVisible = false,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            OrganizerId = 1,
            Poster = posterFile
        };
        _permissionServiceMock.IsInGroupInCurrentYear(_userId, 1).Returns(true);

        Assert.Throws<ArgumentException>(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));
    }

    [Fact]
    public void NormalizeCreateRequest_IsEnrollableTrue_SetsEnrollOpenDateNull()
    {
        var dto = new PostActivityDTO
        {
            Name = "Drinks",
            DutchDescription = "NL Desc",
            EnglishDescription = "EN Desc",
            Location = "Tavern",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = true,
            AreParticipantsVisible = false,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            EnrollOpenDate = DateTimeOffset.UtcNow.AddDays(-1)
        };

        ActivityValidator.NormalizeCreateRequest(dto);

        Assert.Null(dto.EnrollOpenDate);
    }

    [Fact]
    public void ParseCreateQuestions_NullOrEmptyJson_ReturnsEmptyList()
    {
        var result1 = ActivityValidator.ParseCreateQuestions(null);
        var result2 = ActivityValidator.ParseCreateQuestions("");

        Assert.Empty(result1);
        Assert.Empty(result2);
    }

    [Fact]
    public void ParseCreateQuestions_ValidJson_ReturnsParsedList()
    {
        var json = "[{\"QuestionDutch\":\"Vraag\",\"QuestionEnglish\":\"Question\",\"Type\":0,\"IsMandatory\":true,\"IsPublic\":true}]";

        var result = ActivityValidator.ParseCreateQuestions(json);

        Assert.Single(result);
        Assert.Equal("Vraag", result[0].QuestionDutch);
        Assert.Equal(QuestionType.String, result[0].Type);
    }

    [Fact]
    public void ParseCreateQuestions_InvalidJson_ThrowsJsonReaderException()
    {
        var json = "invalid-json";

        Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => ActivityValidator.ParseCreateQuestions(json));
    }

    [Fact]
    public void ParseUpdateQuestions_NullOrEmptyJson_ReturnsEmptyList()
    {
        var result1 = ActivityValidator.ParseUpdateQuestions(null);
        var result2 = ActivityValidator.ParseUpdateQuestions("");

        Assert.Empty(result1);
        Assert.Empty(result2);
    }

    [Fact]
    public void ParseUpdateQuestions_ValidJson_ReturnsParsedList()
    {
        var json = "[{\"Id\":1,\"QuestionDutch\":\"Vraag\",\"QuestionEnglish\":\"Question\",\"Type\":1,\"IsMandatory\":false,\"IsPublic\":false}]";

        var result = ActivityValidator.ParseUpdateQuestions(json);

        Assert.Single(result);
        Assert.Equal(1u, result[0].Id);
        Assert.Equal(QuestionType.Boolean, result[0].Type);
    }

    [Fact]
    public void ParseUpdateQuestions_InvalidJson_ThrowsJsonReaderException()
    {
        var json = "invalid-json";

        Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => ActivityValidator.ParseUpdateQuestions(json));
    }

    [Fact]
    public void MapSpecificationQuestion_UpdatesPropertiesAndJoinsOptions()
    {
        var entity = new SpecificationQuestion
        {
            QuestionDutch = "Oud",
            QuestionEnglish = "Old",
            Type = QuestionType.String,
            IsMandatory = false,
            IsPublic = false,
            Options = null
        };
        var dto = new UpdateSpecificationQuestionDTO
        {
            QuestionDutch = "Nieuw",
            QuestionEnglish = "New",
            Type = QuestionType.MultipleChoice,
            IsMandatory = true,
            IsPublic = true,
            Options = new List<string> { "Ja", "Nee" }
        };

        ActivityValidator.MapSpecificationQuestion(entity, dto);

        Assert.Equal("Nieuw", entity.QuestionDutch);
        Assert.Equal("New", entity.QuestionEnglish);
        Assert.Equal(QuestionType.MultipleChoice, entity.Type);
        Assert.True(entity.IsMandatory);
        Assert.True(entity.IsPublic);
        Assert.Equal("Ja;Nee", entity.Options);
    }

    private TestActivityDTO CreateValidDTO()
    {
        return new TestActivityDTO
        {
            Name = "Drinks",
            DutchDescription = "NL Desc",
            EnglishDescription = "EN Desc",
            Location = "Tavern",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = false,
            AreParticipantsVisible = false,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            OrganizerId = 1,
            PaymentDeadline = null,
            EnrollOpenDate = null
        };
    }

    [Fact]
    public void ValidateRequest_ShowOnWebsite_ChecksBoardPermission()
    {
        var dto = CreateValidDTO();
        dto.ShowOnWebsite = true;

        _permissionServiceMock.IsInGroupInCurrentYear(_userId, 1).Returns(true);
        _permissionServiceMock.When(x => x.EnsurePermission(_userId, Permission.EditAllActivities, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        Assert.Throws<UnauthorizedAccessException>(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));
    }

    [Fact]
    public void ValidateRequest_PaymentDeadlineNotNull_ChecksBoardPermission()
    {
        var dto = CreateValidDTO();
        dto.PaymentDeadline = DateTimeOffset.UtcNow.AddDays(1);

        _permissionServiceMock.IsInGroupInCurrentYear(_userId, 1).Returns(true);
        _permissionServiceMock.When(x => x.EnsurePermission(_userId, Permission.EditAllActivities, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        Assert.Throws<UnauthorizedAccessException>(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));
    }

    [Fact]
    public void ValidateRequest_EnrollOpenDateNotNull_ChecksBoardPermission()
    {
        var dto = CreateValidDTO();
        dto.EnrollOpenDate = DateTimeOffset.UtcNow.AddDays(-1);

        _permissionServiceMock.IsInGroupInCurrentYear(_userId, 1).Returns(true);
        _permissionServiceMock.When(x => x.EnsurePermission(_userId, Permission.EditAllActivities, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        Assert.Throws<UnauthorizedAccessException>(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));
    }

    [Fact]
    public void ValidateRequest_OrganizerIdNull_ChecksBoardPermission()
    {
        var dto = CreateValidDTO();
        dto.OrganizerId = null;

        _permissionServiceMock.When(x => x.EnsurePermission(_userId, Permission.EditAllActivities, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        Assert.Throws<UnauthorizedAccessException>(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));
    }

    [Fact]
    public void ValidateRequest_NotInOrganizerGroup_ChecksBoardPermission()
    {
        var dto = CreateValidDTO();
        dto.OrganizerId = 1;

        _permissionServiceMock.IsInGroupInCurrentYear(_userId, 1).Returns(false);
        _permissionServiceMock.When(x => x.EnsurePermission(_userId, Permission.EditAllActivities, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        Assert.Throws<UnauthorizedAccessException>(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));
    }

    [Fact]
    public void ValidateRequest_ValidPosterFile_DoesNotThrow()
    {
        var posterFile = Substitute.For<IFormFile>();
        posterFile.FileName.Returns("poster.png");
        posterFile.ContentType.Returns("image/png");

        var dto = CreateValidDTO();
        dto.Poster = posterFile;
        _permissionServiceMock.IsInGroupInCurrentYear(_userId, 1).Returns(true);

        var exception = Record.Exception(() => ActivityValidator.ValidateRequest(dto, _userId, _permissionServiceMock));

        Assert.Null(exception);
    }
}



using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using NSubstitute;

namespace Backend.Tests.Validators;

public class EnrollmentValidatorTests
{
    private readonly IPaymentValidationService _paymentServiceMock;
    private readonly Member _member;
    private readonly Activity _activity;

    public EnrollmentValidatorTests()
    {
        _paymentServiceMock = Substitute.For<IPaymentValidationService>();
        _member = new Member
        {
            Id = Guid.NewGuid(),
            Suspended = false,
            DateOfBirth = new DateTime(2000, 1, 1),
            StudentNumber = "s1234567",
            FirstName = "John",
            LastName = "Doe",
            Email = "test@example.com",
            PhoneNumber = "+31612345678",
            Street = "Main Street",
            HouseNumber = "42",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _activity = new Activity
        {
            Id = 1,
            IsAdultOnly = false,
            DateTimeStart = DateTime.UtcNow.AddDays(5),
            Enrollments = new List<Enrollment>(),
            SpecificationQuestions = new List<SpecificationQuestion>(),
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(10)
        };
    }

    [Fact]
    public void ValidateEnrollment_NoPaidMembership_ThrowsArgumentException()
    {
        _paymentServiceMock.HasPaidMembershipPaymentBeforeExpirationTime(_member.Id).Returns(false);

        var exception = Assert.Throws<ArgumentException>(() =>
            EnrollmentValidator.ValidateEnrollment(null, _member, _activity, false, _paymentServiceMock));

        Assert.Equal("Member does not have a paid membership payment.", exception.Message);
    }

    [Fact]
    public void ValidateEnrollment_MemberSuspended_ThrowsArgumentException()
    {
        _paymentServiceMock.HasPaidMembershipPaymentBeforeExpirationTime(_member.Id).Returns(true);
        _member.Suspended = true;

        var exception = Assert.Throws<ArgumentException>(() =>
            EnrollmentValidator.ValidateEnrollment(null, _member, _activity, false, _paymentServiceMock));

        Assert.Equal("Member is suspended and cannot enroll in activities.", exception.Message);
    }

    [Fact]
    public void ValidateEnrollment_MemberAlreadyEnrolled_ThrowsArgumentException()
    {
        _paymentServiceMock.HasPaidMembershipPaymentBeforeExpirationTime(_member.Id).Returns(true);
        _activity.Enrollments.Add(new Enrollment { MemberId = _member.Id, ActivityId = _activity.Id });

        var exception = Assert.Throws<ArgumentException>(() =>
            EnrollmentValidator.ValidateEnrollment(null, _member, _activity, false, _paymentServiceMock));

        Assert.Equal("Member is already enrolled (or on waiting list).", exception.Message);
    }

    [Fact]
    public void ValidateEnrollment_AdultOnlyAndUnderage_ThrowsArgumentException()
    {
        _paymentServiceMock.HasPaidMembershipPaymentBeforeExpirationTime(_member.Id).Returns(true);
        _activity.IsAdultOnly = true;
        _activity.DateTimeStart = new DateTime(2026, 5, 30);
        // DateOfBirth must be greater than or equal to DateTimeStart to trigger the condition
        _member.DateOfBirth = _activity.DateTimeStart.AddDays(1);

        var exception = Assert.Throws<ArgumentException>(() =>
            EnrollmentValidator.ValidateEnrollment(null, _member, _activity, false, _paymentServiceMock));

        Assert.Equal("Member does not meet the age requirement for this activity.", exception.Message);
    }

    [Fact]
    public void ValidateEnrollment_ValidEnrollment_DoesNotThrow()
    {
        _paymentServiceMock.HasPaidMembershipPaymentBeforeExpirationTime(_member.Id).Returns(true);

        var exception = Record.Exception(() =>
            EnrollmentValidator.ValidateEnrollment(null, _member, _activity, false, _paymentServiceMock));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateAnswers_NullAnswers_ReturnsEarly()
    {
        var questions = new List<SpecificationQuestion>
        {
            new() { Id = 1, IsMandatory = false, QuestionDutch = "Vraag", QuestionEnglish = "Question" }
        };

        var exception = Record.Exception(() =>
            EnrollmentValidator.ValidateAnswers(null, questions, false));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateAnswers_MissingMandatoryAnswers_ThrowsArgumentException()
    {
        var questions = new List<SpecificationQuestion>
        {
            new() { Id = 1, IsMandatory = true, QuestionDutch = "Vraag", QuestionEnglish = "Question" }
        };
        var provided = new List<PostSpecificationAnswerDTO>();

        var exception = Assert.Throws<ArgumentException>(() =>
            EnrollmentValidator.ValidateAnswers(provided, questions, false));

        Assert.Equal("Missing mandatory answers.", exception.Message);
    }

    [Fact]
    public void ValidateAnswers_MissingMandatoryAnswersByBoard_DoesNotThrow()
    {
        var questions = new List<SpecificationQuestion>
        {
            new() { Id = 1, IsMandatory = true, QuestionDutch = "Vraag", QuestionEnglish = "Question" }
        };
        var provided = new List<PostSpecificationAnswerDTO>();

        var exception = Record.Exception(() =>
            EnrollmentValidator.ValidateAnswers(provided, questions, true)); // isBoard = true

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateAnswers_InvalidQuestionId_ThrowsArgumentException()
    {
        var questions = new List<SpecificationQuestion>
        {
            new() { Id = 1, IsMandatory = false, QuestionDutch = "Vraag", QuestionEnglish = "Question" }
        };
        var provided = new List<PostSpecificationAnswerDTO>
        {
            new() { QuestionId = 99, Answer = "text" }
        };

        var exception = Assert.Throws<ArgumentException>(() =>
            EnrollmentValidator.ValidateAnswers(provided, questions, false));

        Assert.Equal("Invalid specification question(s).", exception.Message);
    }

    [Fact]
    public void ValidateAnswers_ValidMandatoryAnswers_DoesNotThrow()
    {
        var questions = new List<SpecificationQuestion>
        {
            new() { Id = 1, IsMandatory = true, Type = QuestionType.String, QuestionDutch = "Vraag", QuestionEnglish = "Question" }
        };
        var provided = new List<PostSpecificationAnswerDTO>
        {
            new() { QuestionId = 1, Answer = "Some string" }
        };

        var exception = Record.Exception(() =>
            EnrollmentValidator.ValidateAnswers(provided, questions, false));

        Assert.Null(exception);
    }
}

using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json.Serialization;

namespace Backend.Tests.Validators;

public class SpecificationAnswerValidatorTests
{
    private SpecificationAnswer CreateAnswer(string answerValue = "42", Guid? memberId = null)
    {
        return new SpecificationAnswer
        {
            MemberId = memberId ?? Guid.NewGuid(),
            Answer = answerValue,
            Question = new SpecificationQuestion
            {
                QuestionDutch = "Vraag",
                QuestionEnglish = "Question",
                Type = QuestionType.Number,
                Activity = new Activity
                {
                    Name = "Test Activity",
                    DutchDescription = "Beschrijving",
                    EnglishDescription = "Description",
                    Location = "Enschede",
                    PaymentDeadline = DateTimeOffset.UtcNow.AddDays(10)
                }
            }
        };
    }

    [Fact]
    public void ValidateOwnership_UserIsOwner_DoesNotThrow()
    {
        var userId = Guid.NewGuid();
        var answer = CreateAnswer(memberId: userId);

        var exception = Record.Exception(() => SpecificationAnswerValidator.ValidateOwnership(answer, userId));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateOwnership_UserIsNotOwner_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var answer = CreateAnswer(memberId: userId);

        var exception = Assert.Throws<UnauthorizedAccessException>(() =>
            SpecificationAnswerValidator.ValidateOwnership(answer, otherUserId));

        Assert.Equal("Users can only modify their own specification answers.", exception.Message);
    }

    [Fact]
    public void ValidateWithinEnrollmentDeadline_NoDeadline_DoesNotThrow()
    {
        var answer = CreateAnswer();
        answer.Question.Activity.EnrollmentDeadline = null;

        var exception = Record.Exception(() => SpecificationAnswerValidator.ValidateWithinEnrollmentDeadline(answer));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateWithinEnrollmentDeadline_DeadlineInFuture_DoesNotThrow()
    {
        var answer = CreateAnswer();
        answer.Question.Activity.EnrollmentDeadline = DateTimeOffset.UtcNow.AddHours(1);

        var exception = Record.Exception(() => SpecificationAnswerValidator.ValidateWithinEnrollmentDeadline(answer));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateWithinEnrollmentDeadline_DeadlineInPast_ThrowsInvalidOperationException()
    {
        var answer = CreateAnswer();
        answer.Question.Activity.EnrollmentDeadline = DateTimeOffset.UtcNow.AddHours(-1);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SpecificationAnswerValidator.ValidateWithinEnrollmentDeadline(answer));

        Assert.Equal("Cannot modify specification answers after the enrollment deadline.", exception.Message);
    }

    [Fact]
    public void ValidatePatchOperations_OnlyAnswerModified_DoesNotThrow()
    {
        var patchDoc = new JsonPatchDocument<SpecificationAnswer>(
            new List<Operation<SpecificationAnswer>>
            {
                new("replace", "/answer", null, "New Answer Value")
            },
            new DefaultContractResolver()
        );

        var exception = Record.Exception(() => SpecificationAnswerValidator.ValidatePatchOperations(patchDoc));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidatePatchOperations_OtherFieldsModified_ThrowsInvalidOperationException()
    {
        var patchDoc = new JsonPatchDocument<SpecificationAnswer>(
            new List<Operation<SpecificationAnswer>>
            {
                new("replace", "/answer", null, "New Answer Value"),
                new("replace", "/memberId", null, Guid.NewGuid())
            },
            new DefaultContractResolver()
        );

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SpecificationAnswerValidator.ValidatePatchOperations(patchDoc));

        Assert.Equal("Only the 'answer' field can be modified.", exception.Message);
    }

    [Fact]
    public void ValidatePatchedAnswer_ValidAnswer_DoesNotThrow()
    {
        var answer = CreateAnswer();
        answer.Question.Type = QuestionType.Number;

        var patchDoc = new JsonPatchDocument<SpecificationAnswer>(
            new List<Operation<SpecificationAnswer>>
            {
                new("replace", "/answer", null, "42")
            },
            new DefaultContractResolver()
        );

        var exception = Record.Exception(() => SpecificationAnswerValidator.ValidatePatchedAnswer(answer, patchDoc));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidatePatchedAnswer_InvalidAnswer_ThrowsArgumentException()
    {
        var answer = CreateAnswer();
        answer.Question.Type = QuestionType.Number;

        var patchDoc = new JsonPatchDocument<SpecificationAnswer>(
            new List<Operation<SpecificationAnswer>>
            {
                new("replace", "/answer", null, "Not A Number")
            },
            new DefaultContractResolver()
        );

        Assert.Throws<ArgumentException>(() => SpecificationAnswerValidator.ValidatePatchedAnswer(answer, patchDoc));
    }

    [Fact]
    public void ValidatePatchedAnswer_NoAnswerOperation_DoesNotThrow()
    {
        var answer = CreateAnswer();
        answer.Question.Type = QuestionType.Number;

        var patchDoc = new JsonPatchDocument<SpecificationAnswer>(
            new List<Operation<SpecificationAnswer>>(),
            new DefaultContractResolver()
        );

        var exception = Record.Exception(() => SpecificationAnswerValidator.ValidatePatchedAnswer(answer, patchDoc));

        Assert.Null(exception);
    }
}

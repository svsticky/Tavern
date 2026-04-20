using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Validators;

public static class SpecificationAnswerValidator
{
    public static void ValidateOwnership(SpecificationAnswer answer, Guid userId)
    {
        if (answer.MemberId != userId)
        {
            throw new UnauthorizedAccessException("Users can only modify their own specification answers.");
        }
    }

    public static void ValidateWithinEnrollmentDeadline(SpecificationAnswer answer)
    {
        if (answer.Question.Activity.EnrollmentDeadline != null
            && DateTimeOffset.UtcNow > answer.Question.Activity.EnrollmentDeadline)
        {
            throw new InvalidOperationException("Cannot modify specification answers after the enrollment deadline.");
        }
    }

    public static void ValidatePatchOperations(JsonPatchDocument<SpecificationAnswer> patchDoc)
    {
        if (patchDoc.Operations.Any(op => !op.path.Equals("/answer", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Only the 'answer' field can be modified.");
        }
    }

    public static void ValidatePatchedAnswer(SpecificationAnswer answer, JsonPatchDocument<SpecificationAnswer> patchDoc)
    {
        var newAnswer = patchDoc.Operations
            .FirstOrDefault(op => op.path.Equals("/answer", StringComparison.OrdinalIgnoreCase))
            ?.value
            ?.ToString();

        if (newAnswer != null)
        {
            AnswerValidator.IsValidAnswer(newAnswer, answer.Question.Type, answer.Question.Options);
        }
    }
}

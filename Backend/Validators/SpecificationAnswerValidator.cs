using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Validators;

/// <summary>
/// Provides validation helpers for mutating specification answers.
/// </summary>
public static class SpecificationAnswerValidator
{
    /// <summary>
    /// Validates that the requesting user owns the specification answer.
    /// </summary>
    /// <param name="answer">The specification answer to check.</param>
    /// <param name="userId">The requesting user ID.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user does not own the answer.</exception>
    public static void ValidateOwnership(SpecificationAnswer answer, Guid userId)
    {
        if (answer.MemberId != userId)
        {
            throw new UnauthorizedAccessException("Users can only modify their own specification answers.");
        }
    }

    /// <summary>
    /// Validates that the related enrollment deadline has not passed.
    /// </summary>
    /// <param name="answer">The specification answer to check.</param>
    /// <exception cref="InvalidOperationException">Thrown when the enrollment deadline has passed.</exception>
    public static void ValidateWithinEnrollmentDeadline(SpecificationAnswer answer)
    {
        if (answer.Question.Activity.EnrollmentDeadline != null
            && DateTimeOffset.UtcNow > answer.Question.Activity.EnrollmentDeadline)
        {
            throw new InvalidOperationException("Cannot modify specification answers after the enrollment deadline.");
        }
    }

    /// <summary>
    /// Validates that the patch only modifies the <c>answer</c> field.
    /// </summary>
    /// <param name="patchDoc">The patch document to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when unsupported patch operations are present.</exception>
    public static void ValidatePatchOperations(JsonPatchDocument<SpecificationAnswer> patchDoc)
    {
        if (patchDoc.Operations.Any(op => !op.path.Equals("/answer", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Only the 'answer' field can be modified.");
        }
    }

    /// <summary>
    /// Validates that the patched answer value is valid for the question type.
    /// </summary>
    /// <param name="answer">The current specification answer entity.</param>
    /// <param name="patchDoc">The patch document containing the new answer value.</param>
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

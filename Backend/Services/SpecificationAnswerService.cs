using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Utils;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Services;

public class SpecificationAnswerService(
        PostgresDbContext db
) : ISpecificationAnswerService
{
    public async Task PatchSpecificationAnswersAsync(Guid userId, uint answerId, JsonPatchDocument<SpecificationAnswer> patchDoc)
    {
        var answer = db.SpecificationAnswers.FirstOrDefault(a => a.Id == answerId);
        if (answer == null)
        {
            throw new KeyNotFoundException();
        }
        
        if (answer.MemberId != userId)
        {
            throw new UnauthorizedAccessException("Users can only modify their own specification answers.");
        }

        if(answer.Question.Activity.EnrollmentDeadline != null && DateTimeOffset.UtcNow > answer.Question.Activity.EnrollmentDeadline)
        {
            throw new InvalidOperationException("Cannot modify specification answers after the enrollment deadline.");
        }

        var operations = patchDoc.Operations;
        if (operations.Any(op => op.path.ToLower() != "/answer"))
        {
            throw new InvalidOperationException("Only the 'answer' field can be modified.");
        }

        patchDoc.ApplyTo(answer);

        var newAnswer = patchDoc.Operations.FirstOrDefault(op => op.path.ToLower() == "/answer")?.value?.ToString();
        if (newAnswer != null && !AnswerValidateUtils.IsValidAnswer(newAnswer, answer.Question.Type, answer.Question.Options))
        {
            throw new InvalidOperationException("Invalid answer provided.");
        }

        StateValidateUtils.Validate(answer);

        await db.SaveChangesAsync();
    }
}
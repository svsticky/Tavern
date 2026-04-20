using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Services;

public class SpecificationAnswerService(
        PostgresDbContext db
) : ISpecificationAnswerService
{
    public async Task PatchSpecificationAnswersAsync(Guid userId, uint answerId, JsonPatchDocument<SpecificationAnswer> patchDoc)
    {
        var answer = GetAnswerOrThrow(answerId);
        SpecificationAnswerValidator.ValidateOwnership(answer, userId);
        SpecificationAnswerValidator.ValidateWithinEnrollmentDeadline(answer);
        SpecificationAnswerValidator.ValidatePatchOperations(patchDoc);

        patchDoc.ApplyTo(answer);

        SpecificationAnswerValidator.ValidatePatchedAnswer(answer, patchDoc);

        StateValidator.Validate(answer);

        await db.SaveChangesAsync();
    }

    private SpecificationAnswer GetAnswerOrThrow(uint answerId)
    {
        var answer = db.SpecificationAnswers.FirstOrDefault(a => a.Id == answerId);
        return answer ?? throw new KeyNotFoundException();
    }
}

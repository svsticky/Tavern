using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Services;

public class SpecificationAnswerService(
        PostgresDbContext db,
        IPermissionService permissionService
) : ISpecificationAnswerService
{
    public async Task PatchSpecificationAnswersAsync(Guid fromUserId, uint answerId, JsonPatchDocument<SpecificationAnswer> patchDoc, Guid userId)
    {
        if(userId != fromUserId)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
        }

        if(patchDoc == null)
            throw new ArgumentException("Patch document is null");
        
        if(patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase) 
            || op.path.Equals("/memberId", StringComparison.OrdinalIgnoreCase) 
            || op.path.Equals("/member", StringComparison.OrdinalIgnoreCase) 
            || op.path.Equals("/questionId", StringComparison.OrdinalIgnoreCase)
            || op.path.Equals("/question", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Cannot modify Id, EnrollmentId or QuestionId fields.");

        var answer = GetAnswerOrThrow(answerId);
        SpecificationAnswerValidator.ValidateOwnership(answer, fromUserId);
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

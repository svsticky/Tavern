using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

/// <summary>
/// Defines operations for updating member specification answers.
/// </summary>
public interface ISpecificationAnswerService
{
    /// <summary>
    /// Applies a JSON Patch document to a specification answer.
    /// </summary>
    /// <param name="fromUserId">The ID of the user performing the update.</param>
    /// <param name="answerId">The specification answer ID.</param>
    /// <param name="patchDoc">The patch document to apply.</param>
    /// <param name="userId">The ID of the member owning the answer.</param>
    public Task PatchSpecificationAnswersAsync(Guid fromUserId, uint answerId, JsonPatchDocument<SpecificationAnswer> patchDoc, Guid userId);
}

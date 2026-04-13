using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface ISpecificationAnswerService
{
    public Task PatchSpecificationAnswersAsync(Guid userId, uint answerId, JsonPatchDocument<SpecificationAnswer> patchDoc);
}
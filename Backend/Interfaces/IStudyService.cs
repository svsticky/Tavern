using Backend.Controllers.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    public interface IStudyService
    {
        Task<List<Study>> GetStudies(CancellationToken ct);
        Task<Study?> GetStudy(uint id, CancellationToken ct);

        Task<Study> CreateStudy(PostStudyDTO dto, Guid userId, CancellationToken ct);

        Task DeleteStudy(uint id, Guid userId, CancellationToken ct);

        Task PatchStudy(uint id, JsonPatchDocument<Study> patchDoc, Guid userId, CancellationToken ct);

        Task UpdateStudy(uint id, StudyUpdateDTO dto, Guid userId, CancellationToken ct);
    }
}
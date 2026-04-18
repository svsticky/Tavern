using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    public interface IStudyEnrollmentService
    {
        Task<List<StudyEnrollmentResponseDTO>> GetStudyEnrollments(GetStudyEnrollmentsDTO dto, Guid userId, CancellationToken ct);

        Task<StudyEnrollmentResponseDTO?> GetStudyEnrollment(uint id, Guid userId, CancellationToken ct);

        Task<StudyEnrollmentResponseDTO> CreateStudyEnrollment(PostStudyEnrollmentDTO dto, Guid userId, CancellationToken ct);

        Task DeleteStudyEnrollment(uint id, Guid userId, CancellationToken ct);

        Task PatchStudy(uint id, JsonPatchDocument<StudyEnrollment> patchDoc, Guid userId, CancellationToken ct);
    }
}
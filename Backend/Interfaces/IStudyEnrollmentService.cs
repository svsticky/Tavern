using Backend.Controllers.DTOs;
using Backend.Models;

namespace Backend.Interfaces
{
    public interface IStudyEnrollmentService
    {
        Task<List<StudyEnrollmentResponseDTO>> GetStudyEnrollments(Guid userId, CancellationToken ct);

        Task<StudyEnrollmentResponseDTO?> GetStudyEnrollment(uint id, Guid userId, CancellationToken ct);

        Task<StudyEnrollmentResponseDTO> CreateStudyEnrollment(PostStudyEnrollmentDTO dto, Guid userId, CancellationToken ct);

        Task DeleteStudyEnrollment(uint id, Guid userId, CancellationToken ct);

        Task UpdateStatus(uint id, StudyStatus newStatus, Guid userId, CancellationToken ct);
    }
}
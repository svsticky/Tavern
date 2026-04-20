using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface IEnrollmentService
{
    Task<IEnumerable<EnrollmentResponseDTO>> GetEnrollments(GetEnrollmentsDTO dto, Guid userId,CancellationToken cancellationToken);

    Task<EnrollmentResponseDTO?> GetEnrollment(EnrollmentKeyDTO dto, Guid userId, CancellationToken cancellationToken);

    Task<EnrollmentResponseDTO> CreateEnrollment(PostEnrollmentDTO dto, Guid userId, CancellationToken cancellationToken);

    Task DeleteEnrollment(EnrollmentKeyDTO dto, Guid userId, CancellationToken cancellationToken);

    Task UpdateEnrollment(PostEnrollmentDTO dto, Guid userId, CancellationToken cancellationToken);

    Task PatchEnrollment(EnrollmentKeyDTO dto, JsonPatchDocument<Enrollment> patchDoc, Guid userId, CancellationToken cancellationToken);
    void PromoteFromWaitingList(uint activityId, CancellationToken ct);
    void PromoteFromWaitingList(uint activityId, int numberToPromote, CancellationToken ct);
}
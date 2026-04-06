using Backend.Controllers.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface IEnrollmentService
{
    Task<IEnumerable<Enrollment>> GetEnrollments(CancellationToken cancellationToken);

    Task<Enrollment?> GetEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken);

    Task<Enrollment> CreateEnrollment(PostEnrollmentDTO dto, Guid userId, CancellationToken cancellationToken);

    Task DeleteEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken);

    Task UpdateEnrollment(uint activityId, Guid memberId, PostEnrollmentDTO dto, CancellationToken cancellationToken);

    Task PatchEnrollment(uint activityId, Guid memberId, JsonPatchDocument<Enrollment> patchDoc, CancellationToken cancellationToken);
}
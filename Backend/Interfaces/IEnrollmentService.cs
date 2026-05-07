using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for managing enrollments and waiting-list promotion.
/// </summary>
public interface IEnrollmentService
{
    /// <summary>
    /// Retrieves enrollments visible to the requesting user.
    /// </summary>
    /// <param name="dto">The enrollment query filters.</param>
    /// <param name="userId">The ID of the requesting user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The enrollments matching the supplied filters.</returns>
    Task<IEnumerable<EnrollmentResponseDTO>> GetEnrollments(GetEnrollmentsDTO dto, Guid userId,CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single enrollment by its composite key.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="userToEnroll">The member ID.</param>
    /// <param name="userId">The ID of the requesting user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The enrollment when found; otherwise <c>null</c>.</returns>
    Task<EnrollmentResponseDTO?> GetEnrollment(uint activityId, Guid userToEnroll, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new enrollment.
    /// </summary>
    /// <param name="dto">The enrollment payload.</param>
    /// <param name="userId">The ID of the user creating the enrollment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created enrollment response.</returns>
    Task<EnrollmentResponseDTO> CreateEnrollment(PostEnrollmentDTO dto, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an enrollment identified by its composite key.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="enrolledUser">The member ID.</param>
    /// <param name="userId">The ID of the user deleting the enrollment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteEnrollment(uint activityId, Guid enrolledUser, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces an existing enrollment.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="userId">The ID of the user updating the enrollment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateEnrollment(PostEnrollmentDTO dto, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Applies a JSON Patch document to an enrollment.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="enrolledUser">The member ID.</param>
    /// <param name="patchDoc">The patch document to apply.</param>
    /// <param name="userId">The ID of the user updating the enrollment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PatchEnrollment(uint activityId, Guid enrolledUser, JsonPatchDocument<Enrollment> patchDoc, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Promotes members from the waiting list for an activity.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PromoteFromWaitingList(uint activityId, CancellationToken ct);

    /// <summary>
    /// Promotes a fixed number of members from the waiting list for an activity.
    /// </summary>
    /// <param name="activityId">The activity ID.</param>
    /// <param name="numberToPromote">The number of members to promote.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PromoteFromWaitingList(uint activityId, int numberToPromote, CancellationToken ct);
}

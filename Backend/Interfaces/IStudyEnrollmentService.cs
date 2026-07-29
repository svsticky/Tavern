using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    /// <summary>
    /// Defines the contract for managing study enrollments.
    /// </summary>
    public interface IStudyEnrollmentService
    {
        /// <summary>
        /// Retrieves study enrollments visible to the requesting user.
        /// </summary>
        /// <param name="dto">The study enrollment query filters.</param>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The study enrollments matching the supplied filters.</returns>
        Task<List<StudyEnrollmentResponseDTO>> GetStudyEnrollments(GetStudyEnrollmentsDTO dto, Guid userId, CancellationToken ct);

        /// <summary>
        /// Retrieves a single study enrollment by ID.
        /// </summary>
        /// <param name="id">The study enrollment ID.</param>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The study enrollment when found; otherwise <c>null</c>.</returns>
        Task<StudyEnrollmentResponseDTO?> GetStudyEnrollment(uint id, Guid userId, CancellationToken ct);

        /// <summary>
        /// Creates a new study enrollment.
        /// </summary>
        /// <param name="dto">The study enrollment payload.</param>
        /// <param name="userId">The ID of the user creating the study enrollment.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The created study enrollment response.</returns>
        Task<StudyEnrollmentResponseDTO> CreateStudyEnrollment(PostStudyEnrollmentDTO dto, Guid userId, CancellationToken ct);

        /// <summary>
        /// Deletes a study enrollment by ID.
        /// </summary>
        /// <param name="id">The study enrollment ID.</param>
        /// <param name="userId">The ID of the user deleting the study enrollment.</param>
        /// <param name="ct">The cancellation token.</param>
        Task DeleteStudyEnrollment(uint id, Guid userId, CancellationToken ct);

        /// <summary>
        /// Applies a JSON Patch document to a study enrollment.
        /// </summary>
        /// <param name="id">The study enrollment ID.</param>
        /// <param name="patchDoc">The patch document to apply.</param>
        /// <param name="userId">The ID of the user updating the study enrollment.</param>
        /// <param name="ct">The cancellation token.</param>
        Task PatchStudyEnrollment(uint id, JsonPatchDocument<StudyEnrollment> patchDoc, Guid userId, CancellationToken ct);
    }
}

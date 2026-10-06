using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    /// <summary>
    /// Defines the contract for managing study entities.
    /// </summary>
    public interface IStudyService
    {
        /// <summary>
        /// Retrieves all studies, ordered with Bachelor programs first, then Master programs, then alphabetically by title.
        /// </summary>
        /// <param name="dto">The filter criteria, e.g. whether to include inactive studies.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The studies.</returns>
        Task<List<Study>> GetStudies(GetStudyDTO dto, CancellationToken ct);

        /// <summary>
        /// Retrieves a study by ID.
        /// </summary>
        /// <param name="id">The study ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The study when found; otherwise <c>null</c>.</returns>
        Task<Study?> GetStudy(uint id, CancellationToken ct);

        /// <summary>
        /// Creates a new study.
        /// </summary>
        /// <param name="dto">The study payload.</param>
        /// <param name="userId">The ID of the user creating the study.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The created study entity.</returns>
        Task<Study> CreateStudy(PostStudyDTO dto, Guid userId, CancellationToken ct);

        /// <summary>
        /// Deletes a study by ID.
        /// </summary>
        /// <param name="id">The study ID.</param>
        /// <param name="userId">The ID of the user deleting the study.</param>
        /// <param name="ct">The cancellation token.</param>
        Task DeleteStudy(uint id, Guid userId, CancellationToken ct);

        /// <summary>
        /// Applies a JSON Patch document to a study.
        /// </summary>
        /// <param name="id">The study ID.</param>
        /// <param name="patchDoc">The patch document to apply.</param>
        /// <param name="userId">The ID of the user updating the study.</param>
        /// <param name="ct">The cancellation token.</param>
        Task PatchStudy(uint id, JsonPatchDocument<Study> patchDoc, Guid userId, CancellationToken ct);

        /// <summary>
        /// Replaces a study with the provided values.
        /// </summary>
        /// <param name="id">The study ID.</param>
        /// <param name="dto">The replacement study payload.</param>
        /// <param name="userId">The ID of the user updating the study.</param>
        /// <param name="ct">The cancellation token.</param>
        Task UpdateStudy(uint id, StudyUpdateDTO dto, Guid userId, CancellationToken ct);
    }
}

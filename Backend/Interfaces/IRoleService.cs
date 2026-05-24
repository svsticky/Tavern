using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    /// <summary>
    /// Defines the contract for managing role definitions.
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        /// Retrieves all roles.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The roles.</returns>
        Task<List<Role>> GetRoles(CancellationToken ct);

        /// <summary>
        /// Retrieves a role by ID.
        /// </summary>
        /// <param name="id">The role ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The role when found; otherwise <c>null</c>.</returns>
        Task<Role?> GetRole(uint id, CancellationToken ct);

        /// <summary>
        /// Creates a new role.
        /// </summary>
        /// <param name="dto">The role payload.</param>
        /// <param name="userId">The ID of the user creating the role.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The created role entity.</returns>
        Task<Role> CreateRole(PostRoleDTO dto, Guid userId, CancellationToken ct);

        /// <summary>
        /// Deletes a role by ID.
        /// </summary>
        /// <param name="id">The role ID.</param>
        /// <param name="userId">The ID of the user deleting the role.</param>
        /// <param name="ct">The cancellation token.</param>
        Task DeleteRole(uint id, Guid userId, CancellationToken ct);

        /// <summary>
        /// Applies a JSON Patch document to a role.
        /// </summary>
        /// <param name="id">The role ID.</param>
        /// <param name="patchDoc">The patch document to apply.</param>
        /// <param name="userId">The ID of the user updating the role.</param>
        /// <param name="ct">The cancellation token.</param>
        Task PatchRole(uint id, JsonPatchDocument<Role> patchDoc, Guid userId, CancellationToken ct);

        /// <summary>
        /// Replaces a role with the provided values.
        /// </summary>
        /// <param name="id">The role ID.</param>
        /// <param name="dto">The replacement role payload.</param>
        /// <param name="userId">The ID of the user updating the role.</param>
        /// <param name="ct">The cancellation token.</param>
        Task UpdateRole(uint id, RoleUpdateDTO dto, Guid userId, CancellationToken ct);
    }
}

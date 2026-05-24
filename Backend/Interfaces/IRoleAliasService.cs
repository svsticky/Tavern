using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    /// <summary>
    /// Defines the contract for managing role-alias metadata.
    /// </summary>
    public interface IRoleAliasRepository
    {
        /// <summary>
        /// Retrieves all role aliases.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The role aliases.</returns>
        Task<List<RoleAlias>> GetRoleAliases(CancellationToken ct);

        /// <summary>
        /// Retrieves a role alias by ID.
        /// </summary>
        /// <param name="id">The role alias ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The role alias when found; otherwise <c>null</c>.</returns>
        Task<RoleAlias?> GetRoleAlias(uint id, CancellationToken ct);

        /// <summary>
        /// Creates a new role alias.
        /// </summary>
        /// <param name="dto">The role alias payload.</param>
        /// <param name="userId">The ID of the user creating the role alias.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The created role alias entity.</returns>
        Task<RoleAlias> CreateRoleAlias(PostRoleAliasDTO dto, Guid userId, CancellationToken ct);

        /// <summary>
        /// Deletes a role alias by ID.
        /// </summary>
        /// <param name="id">The role alias ID.</param>
        /// <param name="userId">The ID of the user deleting the role alias.</param>
        /// <param name="ct">The cancellation token.</param>
        Task DeleteRoleAlias(uint id, Guid userId, CancellationToken ct);

        /// <summary>
        /// Applies a JSON Patch document to a role alias.
        /// </summary>
        /// <param name="id">The role alias ID.</param>
        /// <param name="patchDoc">The patch document to apply.</param>
        /// <param name="userId">The ID of the user updating the role alias.</param>
        /// <param name="ct">The cancellation token.</param>
        Task PatchRoleAlias(uint id, JsonPatchDocument<RoleAlias> patchDoc, Guid userId, CancellationToken ct);

        /// <summary>
        /// Replaces a role alias with the provided values.
        /// </summary>
        /// <param name="id">The role alias ID.</param>
        /// <param name="dto">The replacement role alias payload.</param>
        /// <param name="userId">The ID of the user updating the role alias.</param>
        /// <param name="ct">The cancellation token.</param>
        Task UpdateRoleAlias(uint id, RoleAliasUpdateDTO dto, Guid userId, CancellationToken ct);
    }
}

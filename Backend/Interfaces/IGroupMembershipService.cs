using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for managing memberships of members within groups.
/// </summary>
public interface IGroupMembershipRepository
{
    /// <summary>
    /// Retrieves group memberships visible to the requesting user.
    /// </summary>
    /// <param name="dto">The group membership query filters.</param>
    /// <param name="userId">The ID of the requesting user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The group memberships matching the supplied filters.</returns>
    Task<IEnumerable<GroupMembershipResponseDTO>> GetGroupMemberships(GetGroupMembershipsDTO dto, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single group membership by ID.
    /// </summary>
    /// <param name="id">The group membership ID.</param>
    /// <param name="userId">The ID of the requesting user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The group membership when found; otherwise <c>null</c>.</returns>
    Task<GroupMembershipResponseDTO?> GetGroupMembership(uint id, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new group membership.
    /// </summary>
    /// <param name="dto">The group membership payload.</param>
    /// <param name="userId">The ID of the user creating the group membership.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created group membership entity.</returns>
    Task<GroupMembership> CreateGroupMembership(PostGroupMembershipDTO dto, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a group membership by ID.
    /// </summary>
    /// <param name="id">The group membership ID.</param>
    /// <param name="userId">The ID of the user deleting the group membership.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteGroupMembership(uint id, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Applies a JSON Patch document to a group membership.
    /// </summary>
    /// <param name="id">The group membership ID.</param>
    /// <param name="userId">The ID of the user updating the group membership.</param>
    /// <param name="patchDoc">The patch document to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PatchGroupMembership(uint id, Guid userId, JsonPatchDocument<GroupMembership> patchDoc, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces a group membership with the provided values.
    /// </summary>
    /// <param name="id">The group membership ID.</param>
    /// <param name="userId">The ID of the user updating the group membership.</param>
    /// <param name="dto">The replacement group membership payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateGroupMembership(uint id, Guid userId, GroupMembershipUpdateDTO dto, CancellationToken cancellationToken);
}

using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface IGroupMembershipService
{
    Task<IEnumerable<GroupMembershipResponseDTO>> GetGroupMemberships(GetGroupMembershipsDTO dto, Guid userId, CancellationToken cancellationToken);

    Task<GroupMembershipResponseDTO?> GetGroupMembership(uint id, Guid userId, CancellationToken cancellationToken);

    Task<GroupMembership> CreateGroupMembership(PostGroupMembershipDTO dto, Guid userId, CancellationToken cancellationToken);

    Task DeleteGroupMembership(uint id, Guid userId, CancellationToken cancellationToken);

    Task PatchGroupMembership(uint id, Guid userId, JsonPatchDocument<GroupMembership> patchDoc, CancellationToken cancellationToken);

    Task UpdateGroupMembership(uint id, Guid userId, GroupMembershipUpdateDTO dto, CancellationToken cancellationToken);
}
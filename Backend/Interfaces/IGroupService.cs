using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for managing groups and group profile images.
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Retrieves groups visible to the requesting user.
    /// </summary>
    /// <param name="userId">The ID of the requesting user.</param>
    /// <param name="dto">The group query filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The groups matching the supplied filters.</returns>
    Task<IEnumerable<GroupResponseDTO>> GetGroups(Guid userId, GetGroupDTO dto, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single group by ID.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The group when found; otherwise <c>null</c>.</returns>
    Task<GroupResponseDTO?> GetGroup(uint id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new group.
    /// </summary>
    /// <param name="dto">The group payload.</param>
    /// <param name="userId">The ID of the user creating the group.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created group entity.</returns>
    Task<Group> CreateGroup(PostGroupDTO dto, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a group picture file by storage path.
    /// </summary>
    /// <param name="path">The storage path of the group picture.</param>
    /// <returns>The file metadata and stream when found; otherwise <c>null</c>.</returns>
    Task<FileResultDto?> GetGroupPictureFile(string path);

    /// <summary>
    /// Deletes a group by ID.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="userId">The ID of the user deleting the group.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteGroup(uint id, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Applies a JSON Patch document to a group.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="userId">The ID of the user updating the group.</param>
    /// <param name="patchDoc">The patch document to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PatchGroup(uint id, Guid userId, JsonPatchDocument<Group> patchDoc, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces a group with the provided values.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="userId">The ID of the user updating the group.</param>
    /// <param name="dto">The replacement group payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateGroup(uint id, Guid userId, GroupUpdateDTO dto, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads and assigns a picture to a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="userId">The ID of the user uploading the picture.</param>
    /// <param name="image">The image file to upload.</param>
    /// <returns>The stored file path when uploaded; otherwise <c>null</c>.</returns>
    Task<string?> UploadGroupPicture(uint groupId, Guid userId, IFormFile? image);

    /// <summary>
    /// Retrieves the configured board group ID.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The board group ID.</returns>
    Task<uint> GetBoardGroupId(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the configured candidate board group ID.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The candidate board group ID.</returns>
    Task<uint> GetCandidateBoardGroupId(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the permissions granted to a group.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The permissions granted to the group.</returns>
    Task<List<string>> GetGroupPermissions(uint id, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the permissions granted to a group.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="permissions">The full set of permissions the group should have.</param>
    /// <param name="userId">The ID of the user updating the permissions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SetGroupPermissions(uint id, List<string> permissions, Guid userId, CancellationToken cancellationToken);
}

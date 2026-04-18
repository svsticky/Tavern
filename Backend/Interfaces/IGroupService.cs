using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface IGroupService
{
    Task<IEnumerable<GroupResponseDTO>> GetGroups(Guid userId, GetGroupDTO dto, CancellationToken cancellationToken);

    Task<GroupResponseDTO?> GetGroup(uint id, CancellationToken cancellationToken);

    Task<Group> CreateGroup(PostGroupDTO dto, Guid userId, CancellationToken cancellationToken);

    Task<FileResultDto?> GetGroupPictureFile(string path);

    Task DeleteGroup(uint id, Guid userId, CancellationToken cancellationToken);

    Task PatchGroup(uint id, Guid userId, JsonPatchDocument<Group> patchDoc, CancellationToken cancellationToken);

    Task UpdateGroup(uint id, Guid userId, GroupUpdateDTO dto, CancellationToken cancellationToken);
    
    Task<string?> UploadGroupPicture(uint groupId, Guid userId, IFormFile? image);

    Task<uint> GetBoardGroupId(CancellationToken cancellationToken);

    Task<uint> GetCandidateBoardGroupId(CancellationToken cancellationToken);
}
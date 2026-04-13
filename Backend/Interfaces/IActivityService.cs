using Backend.Controllers.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface IActivityService
{
    Task<IEnumerable<ActivityResponseDTO>> GetActivities(Guid userId, GetActivitiesDTO dto);

    Task<ActivityResponseDTO?> GetActivity(Guid userId, uint id);

    Task<Activity> CreateActivity(Guid userId, PostActivityDTO dto);

    Task DeleteActivity(Guid userId, uint id);

    Task PatchActivity(Guid userId, uint id, JsonPatchDocument<Activity> patchDoc, CancellationToken ct);
    
    Task UploadPoster(Guid userId, uint id, IFormFile? poster);

    Task PutActivity(Guid userId, uint id, PutActivityDTO dto);

    Task<(Stream Stream, string ContentType, string? FileName)?> GetPoster(Guid userId, uint id, bool download);
}
using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface IMailinglistService
{
    Task<IEnumerable<Mailinglist>> GetMailinglists(CancellationToken ct);
    Task<Mailinglist?> GetMailinglist(int id, CancellationToken ct);
    Task<Mailinglist> CreateMailinglist(PostMailinglistDTO dto, Guid userId, CancellationToken ct);
    Task UpdateMailinglist(int id, PostMailinglistDTO dto, Guid userId, CancellationToken ct);
    Task DeleteMailinglist(int id, Guid userId, CancellationToken ct);
    Task PatchMailinglist(int id, JsonPatchDocument<Mailinglist> patchDoc, Guid userId, CancellationToken ct);
}
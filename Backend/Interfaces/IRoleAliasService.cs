using Backend.Controllers.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    public interface IRoleAliasService
    {
        Task<List<RoleAlias>> GetRoleAliases(CancellationToken ct);
        Task<RoleAlias?> GetRoleAlias(uint id, CancellationToken ct);

        Task<RoleAlias> CreateRoleAlias(PostRoleAliasDTO dto, Guid userId, CancellationToken ct);

        Task DeleteRoleAlias(uint id, Guid userId, CancellationToken ct);

        Task PatchRoleAlias(uint id, JsonPatchDocument<RoleAlias> patchDoc, Guid userId, CancellationToken ct);

        Task UpdateRoleAlias(uint id, RoleAliasUpdateDTO dto, Guid userId, CancellationToken ct);
    }
}
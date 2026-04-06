using Backend.Controllers.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces
{
    public interface IRoleService
    {
        Task<List<Role>> GetRoles(CancellationToken ct);
        Task<Role?> GetRole(uint id, CancellationToken ct);

        Task<Role> CreateRole(PostRoleDTO dto, Guid userId, CancellationToken ct);

        Task DeleteRole(uint id, Guid userId, CancellationToken ct);

        Task PatchRole(uint id, JsonPatchDocument<Role> patchDoc, Guid userId, CancellationToken ct);

        Task UpdateRole(uint id, RoleUpdateDTO dto, Guid userId, CancellationToken ct);
    }
}
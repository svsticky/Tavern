using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class RoleService(
        PostgresDbContext db,
        IPermissionService permissionService
    ) : IRoleService
    {
        public async Task<List<Role>> GetRoles(CancellationToken ct)
        {
            return await db.Roles.ToListAsync(ct);
        }

        public async Task<Role?> GetRole(uint id, CancellationToken ct)
        {
            return await db.Roles.FindAsync(id, ct);
        }

    public async Task<Role> CreateRole(PostRoleDTO dto, Guid userId, CancellationToken ct)
    {
        EnsureBoardMember(userId);

        var role = BuildRole(dto);

            StateValidator.Validate(role);

            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);

            return role;
        }

    public async Task DeleteRole(uint id, Guid userId, CancellationToken ct)
    {
        EnsureBoardMember(userId);

        var role = await GetRoleOrThrow(id, ct);

            db.Roles.Remove(role);
            await db.SaveChangesAsync(ct);
        }

    public async Task PatchRole(uint id, JsonPatchDocument<Role> patchDoc, Guid userId, CancellationToken ct)
    {
        EnsureBoardMember(userId);

            if (patchDoc == null)
                throw new Exception("Patch document is null");

        var role = await GetRoleOrThrow(id, ct);

            patchDoc.ApplyTo(role);

            StateValidator.Validate(role);

            await db.SaveChangesAsync(ct);
        }

    public async Task UpdateRole(uint id, RoleUpdateDTO dto, Guid userId, CancellationToken ct)
    {
        EnsureBoardMember(userId);

        var role = await GetRoleOrThrow(id, ct);
        ApplyUpdate(role, dto);

            StateValidator.Validate(role);

            await db.SaveChangesAsync(ct);
        }

    private void EnsureBoardMember(Guid userId)
    {
        if (!permissionService.IsBoardOrCandidateBoardMember(userId))
        {
            throw new UnauthorizedAccessException("Only board members can perform this action.");
        }
    }

    private static Role BuildRole(PostRoleDTO dto)
    {
        return new Role
        {
            Name = dto.Name
        };
    }

    private static void ApplyUpdate(Role role, RoleUpdateDTO dto)
    {
        role.Name = dto.Name;
    }

    private async Task<Role> GetRoleOrThrow(uint id, CancellationToken ct)
    {
        var role = await db.Roles.FindAsync(new object[] { id }, ct);
        return role ?? throw new Exception("Role not found");
    }
}
}

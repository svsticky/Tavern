using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
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

            var role = new Role
            {
                Name = dto.Name
            };

            StateValidateUtils.Validate(role);

            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);

            return role;
        }

        public async Task DeleteRole(uint id, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            var role = await db.Roles.FindAsync(id, ct);
            if (role == null)
                throw new Exception("Role not found");

            db.Roles.Remove(role);
            await db.SaveChangesAsync(ct);
        }

        public async Task PatchRole(uint id, JsonPatchDocument<Role> patchDoc, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            if (patchDoc == null)
                throw new Exception("Patch document is null");

            var role = await db.Roles.FindAsync(new object[] { id }, ct);
            if (role == null)
                throw new Exception("Role not found");

            patchDoc.ApplyTo(role);

            StateValidateUtils.Validate(role);

            await db.SaveChangesAsync(ct);
        }

        public async Task UpdateRole(uint id, RoleUpdateDTO dto, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            var role = await db.Roles.FindAsync(id, ct);
            if (role == null)
                throw new Exception("Role not found");

            role.Name = dto.Name;

            StateValidateUtils.Validate(role);

            await db.SaveChangesAsync(ct);
        }

        private void EnsureBoardMember(Guid userId)
        {
            if (!permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board))
            {
                throw new UnauthorizedAccessException("Only board members can perform this action.");
            }
        }
    }
}
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    /// <summary>
    /// Implements role management operations.
    /// </summary>
    public class RoleRepository(
        PostgresDbContext db,
        IPermissionService permissionService,
        ILogger<RoleRepository> logger
    ) : IRoleRepository
    {
        /// <inheritdoc />
        public async Task<List<Role>> GetRoles(CancellationToken ct)
        {
            return await db.Roles.ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<Role?> GetRole(uint id, CancellationToken ct)
        {
            return await db.Roles.FindAsync(id, ct);
        }

    /// <inheritdoc />
    public async Task<Role> CreateRole(PostRoleDTO dto, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Creating role by user {UserId}.", userId);

        var role = BuildRole(dto);

            StateValidator.Validate(role);

            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Created role {RoleId}.", role.Id);

            return role;
        }

        /// <inheritdoc />
        public async Task DeleteRole(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
            logger.LogInformation("Deleting role {RoleId} by user {UserId}.", id, userId);

            var role = await GetRoleOrThrow(id, ct);

                db.Roles.Remove(role);
                await db.SaveChangesAsync(ct);
            }

        /// <inheritdoc />
        public async Task PatchRole(uint id, JsonPatchDocument<Role> patchDoc, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
            logger.LogInformation("Patching role {RoleId} by user {UserId}.", id, userId);

            if (patchDoc == null)
                throw new Exception("Patch document is null");

            if(patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Cannot modify Id field.");

            var role = await GetRoleOrThrow(id, ct);

            patchDoc.ApplyTo(role);

            StateValidator.Validate(role);

            await db.SaveChangesAsync(ct);
        }

        /// <inheritdoc />
        public async Task UpdateRole(uint id, RoleUpdateDTO dto, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
            logger.LogInformation("Updating role {RoleId} by user {UserId}.", id, userId);

            var role = await GetRoleOrThrow(id, ct);
            ApplyUpdate(role, dto);

            StateValidator.Validate(role);

            await db.SaveChangesAsync(ct);
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

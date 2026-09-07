using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Domain
{
    /// <summary>
    /// Implements role management operations.
    /// </summary>
    public class RoleService(
        PostgresDbContext db,
        IPermissionService permissionService,
        AuthOutboxWorker authOutboxWorker,
        ILogger<RoleService> logger
    ) : IRoleService
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
            permissionService.EnsurePermission(userId, Permission.ManageRoles);
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
            permissionService.EnsurePermission(userId, Permission.ManageRoles);
            logger.LogInformation("Deleting role {RoleId} by user {UserId}.", id, userId);

            var role = await GetRoleOrThrow(id, ct);

            db.Roles.Remove(role);
            await db.SaveChangesAsync(ct);
        }

        /// <inheritdoc />
        public async Task PatchRole(uint id, JsonPatchDocument<Role> patchDoc, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ManageRoles);
            logger.LogInformation("Patching role {RoleId} by user {UserId}.", id, userId);

            if (patchDoc == null)
                throw new Exception("Patch document is null");

            if (patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Cannot modify Id field.");

            var role = await GetRoleOrThrow(id, ct);

            patchDoc.ApplyTo(role);

            StateValidator.Validate(role);

            await db.SaveChangesAsync(ct);
        }

        /// <inheritdoc />
        public async Task UpdateRole(uint id, RoleUpdateDTO dto, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ManageRoles);
            logger.LogInformation("Updating role {RoleId} by user {UserId}.", id, userId);

            var role = await GetRoleOrThrow(id, ct);
            ApplyUpdate(role, dto);

            StateValidator.Validate(role);

            await db.SaveChangesAsync(ct);
        }

        /// <inheritdoc />
        public async Task<List<string>> GetRolePermissions(uint id, CancellationToken ct)
        {
            await GetRoleOrThrow(id, ct);

            return await db.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionKey)
                .ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task SetRolePermissions(uint id, List<string> permissions, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ManageRolePermissions);
            logger.LogInformation("Setting permissions for role {RoleId} by user {UserId}.", id, userId);

            await GetRoleOrThrow(id, ct);

            var distinctPermissions = permissions.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
            PermissionValidator.ValidateCustomPermissions(distinctPermissions);

            using var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                var existing = await db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(ct);
                db.RolePermissions.RemoveRange(existing);

                foreach (var permission in distinctPermissions)
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = id, PermissionKey = permission });
                }

                var affectedMembers = await GetMembersHoldingRole(id, ct);
                QueueSyncTasks(affectedMembers);

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                logger.LogError(ex, "Failed setting permissions for role {RoleId}.", id);
                throw;
            }
        }

        private async Task<List<Guid?>> GetMembersHoldingRole(uint roleId, CancellationToken ct)
        {
            var currentYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);

            return await db.GroupMemberships
                .Where(gm => gm.MembershipYear == currentYear && gm.RoleAliasId != null && gm.RoleAlias!.RoleId == roleId)
                .Select(gm => gm.Member.AuthSystemUserId)
                .Distinct()
                .ToListAsync(ct);
        }

        private void QueueSyncTasks(IEnumerable<Guid?> authSystemIds)
        {
            foreach (var authSystemId in authSystemIds)
            {
                if (authSystemId.HasValue)
                {
                    authOutboxWorker.EnqueueTask(AuthTaskType.Sync, authSystemId.Value, db);
                }
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

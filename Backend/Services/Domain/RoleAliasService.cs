using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Domain
{
    /// <summary>
    /// Implements role-alias management operations.
    /// </summary>
    public class RoleAliasService(
        PostgresDbContext db,
        IPermissionService permissionService,
        AuthOutboxWorker authOutboxWorker,
        ILogger<RoleAliasService> logger
    ) : IRoleAliasService
    {
        /// <inheritdoc />
        public async Task<List<RoleAlias>> GetRoleAliases(CancellationToken ct)
        {
            return await db.RoleAliases
                .Include(ra => ra.Role)
                .ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<RoleAlias?> GetRoleAlias(uint id, CancellationToken ct)
        {
            return await db.RoleAliases
                .Include(ra => ra.Role)
                .FirstOrDefaultAsync(ra => ra.Id == id, ct);
        }

        /// <inheritdoc />
        public async Task<RoleAlias> CreateRoleAlias(PostRoleAliasDTO dto, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ManageRoles);
            logger.LogInformation("Creating role alias for role {RoleId} by user {UserId}.", dto.RoleId, userId);

            var role = await db.Roles.FindAsync(dto.RoleId, ct);
            if (role == null)
                throw new Exception($"Role with ID {dto.RoleId} does not exist.");

            var entity = new RoleAlias
            {
                Name = dto.Name,
                RoleId = dto.RoleId
            };

            StateValidator.Validate(entity);

            db.RoleAliases.Add(entity);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Created role alias {RoleAliasId}.", entity.Id);

            return entity;
        }

        /// <inheritdoc />
        public async Task DeleteRoleAlias(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ManageRoles);
            logger.LogInformation("Deleting role alias {RoleAliasId} by user {UserId}.", id, userId);

            var roleAlias = await GetRoleAliasOrThrow(id, ct);

            await ExecuteInTransaction(ct, async () =>
            {
                var affectedMembers = await GetAffectedMemberIds(id, ct);

                db.RoleAliases.Remove(roleAlias);
                QueueSyncTasks(affectedMembers);
                await db.SaveChangesAsync(ct);
            });
        }

        /// <inheritdoc />
        public async Task PatchRoleAlias(uint id, JsonPatchDocument<RoleAlias> patchDoc, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ManageRoles);
            logger.LogInformation("Patching role alias {RoleAliasId} by user {UserId}.", id, userId);

            if (patchDoc == null)
                throw new Exception("Patch document is null");

            if (patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase)
                || op.path.Equals("/role", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Cannot modify Id or RoleId fields.");

            var roleAlias = await GetRoleAliasOrThrow(id, ct);

            await ExecuteInTransaction(ct, async () =>
            {
                patchDoc.ApplyTo(roleAlias);

                StateValidator.Validate(roleAlias);

                var affectedMembers = await GetAffectedMemberIds(id, ct);
                QueueSyncTasks(affectedMembers);

                await db.SaveChangesAsync(ct);
            });
        }

        /// <inheritdoc />
        public async Task UpdateRoleAlias(uint id, RoleAliasUpdateDTO dto, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ManageRoles);
            logger.LogInformation("Updating role alias {RoleAliasId} by user {UserId}.", id, userId);

            var roleAlias = await GetRoleAliasOrThrow(id, ct);

            await ExecuteInTransaction(ct, async () =>
            {
                roleAlias.Name = dto.Name;
                roleAlias.RoleId = dto.RoleId;

                StateValidator.Validate(roleAlias);

                var affectedMembers = await GetAffectedMemberIds(id, ct);
                QueueSyncTasks(affectedMembers);

                await db.SaveChangesAsync(ct);
            });
        }

        private async Task<RoleAlias> GetRoleAliasOrThrow(uint id, CancellationToken ct)
        {
            var roleAlias = await db.RoleAliases.FindAsync(new object[] { id }, ct);
            return roleAlias ?? throw new Exception("Role alias not found");
        }

        private async Task<List<Guid>> GetAffectedMemberIds(uint roleAliasId, CancellationToken ct)
        {
            return await db.GroupMemberships
                .Where(gm => gm.RoleAliasId == roleAliasId)
                .Select(gm => gm.MemberId)
                .Distinct()
                .ToListAsync(ct);
        }

        private void QueueSyncTasks(IEnumerable<Guid> memberIds)
        {
            foreach (var memberId in memberIds)
            {
                authOutboxWorker.EnqueueTask(AuthTaskType.Sync, memberId, db);
            }
        }

        private async Task ExecuteInTransaction(CancellationToken ct, Func<Task> action)
        {
            using var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                await action();
                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                logger.LogError(ex, "Transaction failed in role alias service.");
                throw;
            }
        }
    }
}

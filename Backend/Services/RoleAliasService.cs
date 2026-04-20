using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class RoleAliasService(
        PostgresDbContext db,
        IPermissionService permissionService
    ) : IRoleAliasService
    {
        public async Task<List<RoleAlias>> GetRoleAliases(CancellationToken ct)
        {
            return await db.RoleAliases
                .Include(ra => ra.Role)
                .ToListAsync(ct);
        }

        public async Task<RoleAlias?> GetRoleAlias(uint id, CancellationToken ct)
        {
            return await db.RoleAliases
                .Include(ra => ra.Role)
                .FirstOrDefaultAsync(ra => ra.Id == id, ct);
        }

        public async Task<RoleAlias> CreateRoleAlias(PostRoleAliasDTO dto, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

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

            return entity;
        }

        public async Task DeleteRoleAlias(uint id, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            var roleAlias = await GetRoleAliasOrThrow(id, ct);

            await ExecuteInTransaction(ct, async () =>
            {
                var affectedMembers = await GetAffectedMemberKeycloakIds(id, ct);

                db.RoleAliases.Remove(roleAlias);
                QueueSyncTasks(affectedMembers);
                await db.SaveChangesAsync(ct);
            });
        }

        public async Task PatchRoleAlias(uint id, JsonPatchDocument<RoleAlias> patchDoc, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            if (patchDoc == null)
                throw new Exception("Patch document is null");

            var roleAlias = await GetRoleAliasOrThrow(id, ct);

            await ExecuteInTransaction(ct, async () =>
            {
                patchDoc.ApplyTo(roleAlias);

                StateValidator.Validate(roleAlias);

                var affectedMembers = await GetAffectedMemberKeycloakIds(id, ct);
                QueueSyncTasks(affectedMembers);

                await db.SaveChangesAsync(ct);
            });
        }

        public async Task UpdateRoleAlias(uint id, RoleAliasUpdateDTO dto, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            var roleAlias = await GetRoleAliasOrThrow(id, ct);

            await ExecuteInTransaction(ct, async () =>
            {
                roleAlias.Name = dto.Name;
                roleAlias.RoleId = dto.RoleId;

                StateValidator.Validate(roleAlias);

                var affectedMembers = await GetAffectedMemberKeycloakIds(id, ct);
                QueueSyncTasks(affectedMembers);

                await db.SaveChangesAsync(ct);
            });
        }

        private void EnsureBoardMember(Guid userId)
        {
            if (!permissionService.IsBoardOrCandidateBoardMember(userId))
            {
                throw new UnauthorizedAccessException("Only board members can perform this action.");
            }
        }

        private async Task<RoleAlias> GetRoleAliasOrThrow(uint id, CancellationToken ct)
        {
            var roleAlias = await db.RoleAliases.FindAsync(new object[] { id }, ct);
            return roleAlias ?? throw new Exception("Role alias not found");
        }

        private async Task<List<Guid?>> GetAffectedMemberKeycloakIds(uint roleAliasId, CancellationToken ct)
        {
            return await db.GroupMemberships
                .Where(gm => gm.RoleAliasId == roleAliasId)
                .Select(gm => gm.Member.KeycloakId)
                .Distinct()
                .ToListAsync(ct);
        }

        private void QueueSyncTasks(IEnumerable<Guid?> keycloakIds)
        {
            foreach (var keycloakId in keycloakIds)
            {
                db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
                {
                    KeycloakId = keycloakId ?? throw new Exception("Member with null KeycloakId found"),
                    TaskType = KeycloakTaskType.Sync
                });
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
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }
}

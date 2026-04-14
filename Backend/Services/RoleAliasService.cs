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

            StateValidateUtils.Validate(entity);

            db.RoleAliases.Add(entity);
            await db.SaveChangesAsync(ct);

            return entity;
        }

        public async Task DeleteRoleAlias(uint id, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            var roleAlias = await db.RoleAliases.FindAsync(id, ct);
            if (roleAlias == null) throw new Exception("Role alias not found");

            using var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                var affectedMembers = await db.GroupMemberships
                    .Where(gm => gm.RoleAliasId == id)
                    .Select(gm => gm.Member.KeycloakId)
                    .Distinct()
                    .ToListAsync(ct);

                db.RoleAliases.Remove(roleAlias);

                foreach (var keycloakId in affectedMembers)
                {
                    db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
                    {
                        KeycloakId = keycloakId ?? throw new Exception("Member with null KeycloakId found"),
                        TaskType = KeycloakTaskType.Sync
                    });
                }

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task PatchRoleAlias(uint id, JsonPatchDocument<RoleAlias> patchDoc, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            if (patchDoc == null)
                throw new Exception("Patch document is null");

            var roleAlias = await db.RoleAliases.FindAsync(new object[] { id }, ct);
            if (roleAlias == null) throw new Exception("Role alias not found");

            using var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                patchDoc.ApplyTo(roleAlias);

                StateValidateUtils.Validate(roleAlias);

                var affectedMembers = await db.GroupMemberships
                    .Where(gm => gm.RoleAliasId == id)
                    .Select(gm => gm.Member.KeycloakId)
                    .Distinct()
                    .ToListAsync(ct);

                foreach (var memberId in affectedMembers)
                {
                    db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
                    {
                        KeycloakId = memberId ?? throw new Exception("Member with null KeycloakId found"),
                        TaskType = KeycloakTaskType.Sync
                    });
                }

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task UpdateRoleAlias(uint id, RoleAliasUpdateDTO dto, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            var roleAlias = await db.RoleAliases.FindAsync(id, ct);
            if (roleAlias == null) throw new Exception("Role alias not found");

            using var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                roleAlias.Name = dto.Name;
                roleAlias.RoleId = dto.RoleId;

                StateValidateUtils.Validate(roleAlias);

                var affectedMembers = await db.GroupMemberships
                    .Where(gm => gm.RoleAliasId == id)
                    .Select(gm => gm.Member.KeycloakId)
                    .Distinct()
                    .ToListAsync(ct);

                foreach (var memberId in affectedMembers)
                {
                    db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
                    {
                        KeycloakId = memberId ?? throw new Exception("Member with null KeycloakId found"),
                        TaskType = KeycloakTaskType.Sync
                    });
                }

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        private void EnsureBoardMember(Guid userId)
        {
            if (!permissionService.IsBoardOrCandidateBoardMember(userId))
            {
                throw new UnauthorizedAccessException("Only board members can perform this action.");
            }
        }
    }
}
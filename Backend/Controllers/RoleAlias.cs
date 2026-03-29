using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;
using Microsoft.AspNetCore.Authorization;
using Backend.Utils;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RoleAliases(PostgresDbContext db) : ControllerBase
{
    // GET: api/rolealiases
    /// <summary>
    /// Lists all role aliases in the database.
    /// </summary>
    /// <returns>Said list.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleAlias>>> GetRoleAliases(CancellationToken cancellationToken)
    {
        return await db.RoleAliases
            .Include(ra => ra.Role)
            .ToListAsync(cancellationToken);
    }

    // GET: api/rolealiases/5
    /// <summary>
    /// Fetches a single role alias.
    /// </summary>
    /// <param name="id">The id of the role alias to fetch.</param>
    /// <returns>The full role alias.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoleAlias>> GetRoleAlias(uint id, CancellationToken cancellationToken)
    {
        var roleAlias = await db.RoleAliases
            .Include(ra => ra.Role)
            .FirstOrDefaultAsync(ra => ra.Id == id, cancellationToken);

        return roleAlias != null ? roleAlias : NotFound();
    }

    // POST: api/rolealiases
    /// <summary>
    /// Creates a new role alias.
    /// </summary>
    /// <param name="roleAliasDto">The role alias to be added.</param>
    /// <returns>The created role alias.</returns>
    [HttpPost]
    public async Task<ActionResult<RoleAlias>> PostRoleAlias(PostRoleAliasDTO roleAliasDto, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can post role aliases.");
        }

        var role = await db.Roles.FindAsync(roleAliasDto.RoleId, cancellationToken);
        if (role == null) return BadRequest($"Role with ID {roleAliasDto.RoleId} does not exist.");

        var newEntry = db.RoleAliases.Add(new RoleAlias
        {
            Name = roleAliasDto.Name,
            RoleId = roleAliasDto.RoleId
        });

        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRoleAlias), new { id = newEntry.Entity.Id }, newEntry.Entity);
    }

    // DELETE: api/rolealiases/5
    /// <summary>
    /// Deletes a role alias.
    /// </summary>
    /// <param name="id">The id of the role alias to delete.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoleAlias(uint id, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can delete role aliases.");
        }

        var roleAlias = await db.RoleAliases.FindAsync(id, cancellationToken);
        if (roleAlias == null) return NotFound();

        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var affectedMembers = await db.GroupMemberships
                .Where(gm => gm.RoleAliasId == id)
                .Select(gm => gm.Member.KeycloakId)
                .Distinct()
                .ToListAsync(cancellationToken);

            db.RoleAliases.Remove(roleAlias);

            foreach (var keycloakId in affectedMembers)
            {
                db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask { 
                    KeycoakId = keycloakId ?? throw new Exception("Member with null KeycloakId found in affected members list."),
                    TaskType = KeycloakTaskType.Sync
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, $"Error during delete: {ex.Message}");
        }
    }

    // PATCH: api/rolealiases/5
    /// <summary>
    /// Partially updates a role alias.
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchRoleAlias(uint id, [FromBody] JsonPatchDocument<RoleAlias> patchDoc, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can change role aliases.");
        }

        if (patchDoc == null) return BadRequest();

        var roleAlias = await db.RoleAliases.FindAsync(new [] { id }, cancellationToken);
        if (roleAlias == null) return NotFound();

        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            patchDoc.ApplyTo(roleAlias, ModelState);

            TryValidateModel(roleAlias);

            if (!ModelState.IsValid) 
            {                
                await transaction.RollbackAsync(cancellationToken);
                return BadRequest(ModelState);
            }

            var affectedMembers = await db.GroupMemberships
                .Where(gm => gm.RoleAliasId == id)
                .Select(gm => gm.Member.KeycloakId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var memberId in affectedMembers)
            {
                db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask { 
                    KeycoakId = memberId ?? throw new Exception("Member with null KeycloakId found in affected members list."),
                    TaskType = KeycloakTaskType.Sync
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, ex.Message);
        }
    }

    // PUT: api/rolealiases/5
    /// <summary>
    /// Updates a role alias details.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> PutRoleAlias(uint id, RoleAliasUpdateDTO roleAliasDto, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can change role aliases.");
        }

        var roleAlias = await db.RoleAliases.FindAsync(id, cancellationToken);
        if (roleAlias == null) return NotFound();

        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            roleAlias.Name = roleAliasDto.Name;
            roleAlias.RoleId = roleAliasDto.RoleId;

            var affectedMembers = await db.GroupMemberships
                .Where(gm => gm.RoleAliasId == id)
                .Select(gm => gm.Member.KeycloakId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var memberId in affectedMembers)
            {
                db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask { 
                    KeycoakId = memberId ?? throw new Exception("Member with null KeycloakId found in affected members list."), 
                    TaskType = KeycloakTaskType.Sync 
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, ex.Message);
        }
    }
}
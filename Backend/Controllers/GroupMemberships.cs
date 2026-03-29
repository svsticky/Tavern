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
public class GroupMemberships(PostgresDbContext db) : ControllerBase
{
    // GET: api/groupMemberships
    /// <summary>
    /// Lists all group memberships in the database.
    /// </summary>
    /// <param name="onlyOwnMemberships">If true, only returns the group memberships of the currently authenticated user. If false, returns all group memberships. Defaults to false.</param>
    /// <returns>Said list.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupMembershipResponseDTO>>> GetGroupMemberships([FromQuery] bool onlyOwnMemberships = false, CancellationToken cancellationToken = default)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!onlyOwnMemberships && !PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can view group memberships.");
        }

        var result = await db.GroupMemberships
            .Where(gm => !onlyOwnMemberships || gm.MemberId == userId)
            .Select(cm => new GroupMembershipResponseDTO
            {
                Id = cm.Id,
                MemberId = cm.MemberId,
                MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                GroupId = cm.GroupId,
                GroupName = cm.Group.Name,
                GroupType = cm.Group.Type,
                MembershipYear = cm.MembershipYear,
                RoleAliasId = cm.RoleAlias != null ? cm.RoleAlias.Id : null,
                RoleAliasName = cm.RoleAlias != null ? cm.RoleAlias.Name : null
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    // GET: api/groupMemberships/5
    /// <summary>
    /// Fetches a single group membership.
    /// </summary>
    /// <param name="id">The id of the group membership to fetch.</param>
    /// <returns>The full group membership.</returns> 
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupMembership>> GetGroupMembership(uint id, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        var result = await db.GroupMemberships
            .Where(cm => cm.Id == id)
            .Select(cm => new GroupMembershipResponseDTO
            {
                Id = cm.Id,
                MemberId = cm.MemberId,
                MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                GroupId = cm.GroupId,
                GroupName = cm.Group.Name,
                GroupType = cm.Group.Type,
                MembershipYear = cm.MembershipYear,
                RoleAliasId = cm.RoleAlias != null ? cm.RoleAlias.Id : null,
                RoleAliasName = cm.RoleAlias != null ? cm.RoleAlias.Name : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null) return NotFound();

        
        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db) && result.MemberId != userId)
        {
            return Forbid("Only board members can view group memberships.");
        }

        return Ok(result);
    }

    // POST: api/groupMemberships
    /// <summary>
    /// Creates a new group membership with a unique ID assigned by the database.
    /// </summary>
    /// <param name="membershipDto">The group membership to be added to the database.</param>
    /// <returns>Fully created group membership in body and api route of where to fetch it in the headers.</returns>
    [HttpPost]
    public async Task<ActionResult<GroupMembership>> PostGroupMembership(PostGroupMembershipDTO membershipDto, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        // if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        // {
        //     return Forbid();
        // }

        Member? member = await db.Members.FindAsync(membershipDto.MemberId, cancellationToken);
        if (member is null)
            return BadRequest($"Member with ID {membershipDto.MemberId} does not exist.");

        Group? group = await db.Groups.FindAsync(membershipDto.GroupId, cancellationToken);
        if (group is null)
            return BadRequest($"Group with ID {membershipDto.GroupId} does not exist.");

        if (membershipDto.RoleAliasId.HasValue)
        {
            RoleAlias? roleAlias = await db.RoleAliases.FindAsync(membershipDto.RoleAliasId.Value, cancellationToken);
            if (roleAlias is null)
                return BadRequest($"Role alias with ID {membershipDto.RoleAliasId.Value} does not exist.");
        }

        using var transaction = await db. Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var newMembership = new GroupMembership
            {
                Member = member,
                Group = group,
                MembershipYear = membershipDto.MembershipYear,
                RoleAliasId = membershipDto.RoleAliasId
            };

            var newEntry = db.GroupMemberships.Add(newMembership);

            db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
            {
                KeycoakId = member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                TaskType = KeycloakTaskType.Sync
            });

            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            
            return CreatedAtAction(
                nameof(GetGroupMembership),
                new { id = newEntry.Entity.Id },
                newEntry.Entity
            );
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, $"An error occurred while saving the group membership: {ex.Message}");
        }
    }

    // DELETE: api/groupmemberships/5
    /// <summary>
    /// Deletes a group membership.
    /// </summary>
    /// <param name="id">The id of the group membership to delete.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroupMembership(uint id, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can delete group memberships.");
        }

        var membership = await db.GroupMemberships.FindAsync(id, cancellationToken);
        if (membership is null)
            return NotFound();

        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            db.GroupMemberships.Remove(membership);
            await db.SaveChangesAsync(cancellationToken);

            db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
            {
                KeycoakId = membership.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                TaskType = KeycloakTaskType.Sync
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, $"An error occurred while deleting the group membership: {ex.Message}");
        }
    }

    // PATCH: api/groupmemberships/5
    /// <summary>
    /// Partially updates a group membership's details.
    /// </summary>
    /// <param name="id">The id of the group membership to update.</param>
    /// <param name="patchDoc">The patch document containing the changes.</param>
    /// <returns>No Content.</returns>
    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchGroupMembership(uint id, [FromBody] JsonPatchDocument<GroupMembership> patchDoc, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can update group memberships.");
        }

        if (patchDoc == null)
            return BadRequest();

        GroupMembership? membership = await db.GroupMemberships.FindAsync(new object[] { id }, cancellationToken);
        if (membership == null)
            return NotFound();

        var oldMemberId = membership.MemberId;

        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            patchDoc.ApplyTo(membership, ModelState);

            TryValidateModel(membership);

            if (!ModelState.IsValid)
            {
                await transaction.RollbackAsync(cancellationToken);
                return BadRequest(ModelState);
            }

            db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
            {
                KeycoakId = membership.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                TaskType = KeycloakTaskType.Sync
            });

            if(oldMemberId != membership.MemberId)
            {
                Member? oldMember = await db.Members.FindAsync(new object[] { oldMemberId }, cancellationToken);
                if (oldMember is not null)
                {
                    db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
                    {
                        KeycoakId = oldMember.KeycloakId ?? throw new Exception("Old member does not have a Keycloak ID."),
                        TaskType = KeycloakTaskType.Sync
                    });
                }
            }


            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, $"An error occurred while updating the group membership: {ex.Message}");
        }
    }

    // PUT: api/groupmemberships/5
    /// <summary>
    /// Updates a group membership's details.
    /// </summary>
    /// <param name="id">The id of the group membership to update.</param>
    /// <param name="membershipDto">The new details of the group membership.</param>
    /// <returns>No Content.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> PutGroupMembership(uint id, GroupMembershipUpdateDTO membershipDto, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can update group memberships.");
        }

        var membership = await db.GroupMemberships.FindAsync(id, cancellationToken);
        if (membership is null)
            return NotFound();

        var oldMemberId = membership.MemberId;

        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (membershipDto.RoleAliasId.HasValue)
            {
                RoleAlias? roleAlias = await db.RoleAliases.FindAsync(membershipDto.RoleAliasId.Value, cancellationToken);
                if (roleAlias is null)
                    return BadRequest($"Role alias with ID {membershipDto.RoleAliasId.Value} does not exist.");
            }

            membership.RoleAliasId = membershipDto.RoleAliasId;

            db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
            {
                KeycoakId = membership.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                TaskType = KeycloakTaskType.Sync
            });

            if(oldMemberId != membership.MemberId)
            {
                Member? oldMember = await db.Members.FindAsync(new object[] { oldMemberId }, cancellationToken);
                if (oldMember is not null)                {
                    db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
                    {
                        KeycoakId = oldMember.KeycloakId ?? throw new Exception("Old member does not have a Keycloak ID."),
                        TaskType = KeycloakTaskType.Sync
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(500, $"An error occurred while updating the group membership: {ex.Message}");
        }
    }
}
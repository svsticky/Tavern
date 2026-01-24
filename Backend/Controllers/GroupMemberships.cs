using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GroupMemberships(PostgresDbContext db) : ControllerBase
{
    // GET: api/groupMemberships
    /// <summary>
    /// Lists all group memberships in the database.
    /// </summary>
    /// <returns>Said list.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupMembership>>> GetGroupMemberships(CancellationToken cancellationToken)
    {

        var result = await db.GroupMemberships
            .Select(cm => new GroupMembershipResponseDTO
            {
                Id = cm.Id,
                MemberId = cm.MemberId,
                MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                GroupId = cm.GroupId,
                GroupName = cm.Group.Name,
                GroupType = cm.Group.Type,
                MembershipYear = cm.MembershipYear,
                RoleId = cm.Role != null ? cm.Role.Id : null,
                RoleName = cm.Role != null ? cm.Role.Name : null
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
                RoleId = cm.Role != null ? cm.Role.Id : null,
                RoleName = cm.Role != null ? cm.Role.Name : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null) return NotFound();

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
        Member? member = await db.Members.FindAsync(membershipDto.MemberId, cancellationToken);
        if (member is null)
            return BadRequest($"Member with ID {membershipDto.MemberId} does not exist.");

        Group? group = await db.Groups.FindAsync(membershipDto.GroupId, cancellationToken);
        if (group is null)
            return BadRequest($"Group with ID {membershipDto.GroupId} does not exist.");

        var newMembership = new GroupMembership
        {
            Member = member,
            Group = group,
            MembershipYear = membershipDto.MembershipYear,
            RoleId = membershipDto.RoleId
        };

        var newEntry = db.GroupMemberships.Add(newMembership);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(
            nameof(GetGroupMembership),
            new { id = newEntry.Entity.Id },
            newEntry.Entity
        );
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
        var membership = await db.GroupMemberships.FindAsync(id, cancellationToken);
        if (membership is null)
            return NotFound();

        db.GroupMemberships.Remove(membership);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
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
        if (patchDoc == null)
            return BadRequest();

        GroupMembership? membership = await db.GroupMemberships.FindAsync(new object[] { id }, cancellationToken);
        if (membership == null)
            return NotFound();

        patchDoc.ApplyTo(membership, ModelState);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
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
        var membership = await db.GroupMemberships.FindAsync(id, cancellationToken);
        if (membership is null)
            return NotFound();

        membership.RoleId = membershipDto.RoleId;

        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
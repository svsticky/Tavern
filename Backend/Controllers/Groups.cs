using Microsoft.AspNetCore.Mvc;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Groups(PostgresDbContext db) : ControllerBase
    {
        // GET: api/groups
        /// <summary>
        /// Lists all groups in the database.
        /// </summary>
        /// <returns>Said list.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Group>>> GetGroups(CancellationToken cancellationToken)
        {
            var groups = await db.Groups
                .Include(c => c.GroupMemberships)
                .ThenInclude(cm => cm.Member)
                .ToListAsync(cancellationToken);

            var result = groups.Select(c => new GroupResponseDTO
            {
                Id = c.Id,
                Name = c.Name,
                GroupMemberships = c.GroupMemberships.Select(cm => new GroupMembershipResponseDTO
                {
                    Id = cm.Id,
                    GroupId = cm.GroupId,
                    GroupName = cm.Group.Name,
                    GroupType = cm.Group.Type,
                    MemberId = cm.MemberId,
                    MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                    MembershipYear = cm.MembershipYear,
                    RoleId = cm.Role?.Id,
                    RoleName = cm.Role?.Name
                }).ToList()
            });

            return Ok(result);
        }

        // GET: api/groups/5
        /// <summary>
        /// Fetches a single group.
        /// </summary>
        /// <param name="id">The id of the group to fetch.</param>
        /// <returns>The full group.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Group>> GetGroup(uint id, CancellationToken cancellationToken)
        {
            var group = await db.Groups
                .Include(c => c.GroupMemberships)
                .ThenInclude(cm => cm.Member)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (group is null) return NotFound();

            var result = new GroupResponseDTO
            {
                Id = group.Id,
                Name = group.Name,
                Active = group.Active,
                Type = group.Type,
                GroupMemberships = group.GroupMemberships.Select(cm => new GroupMembershipResponseDTO
                {
                    Id = cm.Id,
                    GroupId = cm.GroupId,
                    GroupName = cm.Group.Name,
                    GroupType = cm.Group.Type,
                    MemberId = cm.MemberId,
                    MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                    MembershipYear = cm.MembershipYear,
                    RoleId = cm.Role?.Id,
                    RoleName = cm.Role?.Name
                }).ToList()
            };

            return Ok(result);
        }

        // POST: api/groups
        /// <summary>
        /// Creates a new group with a unique ID assigned by the database.
        /// </summary>
        /// <param name="group">The group to be added to the database.</param>
        /// <returns>Fully created group in body and api route of where to fetch it in the headers.</returns>
        [HttpPost]
        public async Task<ActionResult<Group>> PostGroup(PostGroupDTO groupDto, CancellationToken cancellationToken)
        {
            var newEntry = db.Groups.Add(new Group
            {
                Name = groupDto.Name,
                Type = groupDto.Type
            });
            await db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetGroup), new { id = newEntry.Entity.Id }, newEntry.Entity);
        }

        // DELETE: api/groups/5
        /// <summary>
        /// Deletes a group.
        /// </summary>
        /// <param name="id">The id of the group to delete.</param>
        /// <returns>Nothing, really.</returns>
        /// <remarks>
        /// Deleting a group will also delete all enrollments and group enrollments associated with said
        /// group.
        /// </remarks>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(uint id, CancellationToken cancellationToken)
        {
            Group? group = await db.Groups.FindAsync(id, cancellationToken);
            if (group == null) return NotFound();

            db.Groups.Remove(group);
            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/groups/5/name
        /// <summary>
        /// Updates a group's name.
        /// </summary>
        /// <param name="id">The id of the group to update.</param>
        /// <param name="nameDto">The new name of the group.</param>
        /// <returns>No Content.</returns>
        [HttpPatch("{id}/name")]
        public async Task<IActionResult> PatchGroupName(uint id, string newName, CancellationToken cancellationToken)
        {
            Group? group = await db.Groups.FindAsync(id, cancellationToken);
            if (group == null) return NotFound();

            group.Name = newName;
            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/groups/5/active
        /// <summary>
        /// Updates a group's active status.
        /// </summary>
        /// <param name="id">The id of the group to update.</param>
        /// <param name="activeDto">The new active status of the group.</param>
        /// <returns>No Content.</returns>
        [HttpPatch("{id}/active")]
        public async Task<IActionResult> PatchGroupActive(uint id, bool newActive, CancellationToken cancellationToken)
        {
            Group? group = await db.Groups.FindAsync(id, cancellationToken);
            if (group == null) return NotFound();

            group.Active = newActive;
            await db.SaveChangesAsync(cancellationToken);
            
            return NoContent();
        }

        // PATCH: api/groups/5/type
        /// <summary>
        /// Updates a group's type.
        /// </summary>
        /// <param name="id">The id of the group to update.</param>
        /// <param name="typeDto">The new type of the group.</param>
        /// <returns>No Content.</returns>
        [HttpPatch("{id}/type")]
        public async Task<IActionResult> PatchGroupType(uint id, Models.GroupType newType, CancellationToken cancellationToken)
        {
            Group? group = await db.Groups.FindAsync(id, cancellationToken);
            if (group == null) return NotFound();

            group.Type = newType;
            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PUT: api/groups/5
        /// <summary>
        /// Updates a group's details.
        /// </summary>
        /// <param name="id">The id of the group to update.</param>
        /// <param name="groupDto">The new details of the group.</param>
        /// <returns>No Content.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGroup(uint id, GroupUpdateDTO groupDto, CancellationToken cancellationToken)
        {
            Group? group = await db.Groups.FindAsync(id, cancellationToken);
            if (group == null) return NotFound();

            group.Name = groupDto.Name;
            group.Active = groupDto.Active;
            group.Type = groupDto.Type;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}

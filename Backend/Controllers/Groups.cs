using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;
using Microsoft.AspNetCore.Authorization;
using Backend.Utils;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db))
            {
                return Forbid("Only board members can view groups.");
            }

            var result = await db.Groups
                .Select(c => new GroupResponseDTO
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
                        RoleAliasId = cm.RoleAlias != null ? cm.RoleAlias.Id : null,
                        RoleAliasName = cm.RoleAlias != null ? cm.RoleAlias.Name : null
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

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
            var result = await db.Groups
                .Where(g => g.Id == id)
                .Select(g => new GroupResponseDTO
                {
                    Id = g.Id,
                    Name = g.Name,
                    Active = g.Active,
                    Type = g.Type,
                    GroupMemberships = g.GroupMemberships.Select(gm => new GroupMembershipResponseDTO
                    {
                        Id = gm.Id,
                        GroupId = gm.GroupId,
                        GroupName = g.Name,
                        GroupType = g.Type,
                        MemberId = gm.MemberId,
                        MemberName = $"{gm.Member.FirstName} {gm.Member.LastName}",
                        MembershipYear = gm.MembershipYear,
                        RoleAliasId = gm.RoleAlias != null ? gm.RoleAlias.Id : null,
                        RoleAliasName = gm.RoleAlias != null ? gm.RoleAlias.Name : null
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (result == null) return NotFound();

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
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db))
            {
                return Forbid("Only board members can create groups.");
            }

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
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db))
            {
                return Forbid("Only board members can delete groups.");
            }

            Group? group = await db.Groups.FindAsync(id, cancellationToken);
            if (group == null) return NotFound();

            db.Groups.Remove(group);
            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/groups/5
        /// <summary>
        /// Partially updates an groups details.
        /// </summary>
        /// <param name="id">The id of the group to update.</param>
        /// <param name="patchDoc">The patch document containing the changes.</param>
        /// <returns>No Content.</returns>
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchGroup(uint id, [FromBody] JsonPatchDocument<Group> patchDoc, CancellationToken cancellationToken)
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db))
            {
                return Forbid("Only board members can update groups.");
            }

            if (patchDoc == null)
                return BadRequest();

            Group? group = await db.Groups.FindAsync(new object[] { id }, cancellationToken);
            if (group == null)
                return NotFound();

            patchDoc.ApplyTo(group, ModelState);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db))
            {
                return Forbid("Only board members can update groups.");
            }
            
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

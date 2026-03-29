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
        public async Task<ActionResult<IEnumerable<GroupResponseDTO>>> GetGroups([FromQuery] GetGroupDTO dto, CancellationToken cancellationToken)
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if((dto.MembershipYear == null || dto.IncludeInactive) && !PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

            var result = await db.Groups
                .Select(c => new GroupResponseDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    GroupMemberships = c.GroupMemberships.Select(cm => new GroupMembershipSummaryDTO
                    {
                        Member = new MemberSummaryDTO
                        {
                            Id = cm.Member.Id,
                            FirstName = cm.Member.FirstName,
                            LastName = cm.Member.LastName,
                        },
                        GroupName = c.Name,
                        MembershipYear = cm.MembershipYear
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
        public async Task<ActionResult<GroupResponseDTO>> GetGroup(uint id, CancellationToken cancellationToken)
        {
            var result = await db.Groups
                .Where(g => g.Id == id)
                .Select(g => new GroupResponseDTO
                {
                    Id = g.Id,
                    Name = g.Name,
                    Active = g.Active,
                    Type = g.Type,
                    GroupMemberships = g.GroupMemberships.Select(gm => new GroupMembershipSummaryDTO
                    {
                        Member = new MemberSummaryDTO
                        {
                            Id = gm.Member.Id,
                            FirstName = gm.Member.FirstName,
                            LastName = gm.Member.LastName
                        },
                        GroupName = g.Name,
                        MembershipYear = gm.MembershipYear
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

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid("Only board members can create groups.");
            }

            if(groupDto.Name.Contains(';') || groupDto.Name.Contains(':'))
            {
                return BadRequest("Group names cannot contain ';' or ':'.");
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

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
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

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, PredefinedGroups.Board, db))
            {
                return Forbid("Only board members can update groups.");
            }

            if (patchDoc == null)
                return BadRequest();

            Group? group = await db.Groups.FindAsync(new [] { id }, cancellationToken);
            if (group == null)
                return NotFound();

            patchDoc.ApplyTo(group, ModelState);

            TryValidateModel(group);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (group.Name.Contains(';') || group.Name.Contains(':'))
            {
                return BadRequest("Group names cannot contain ';' or ':'.");
            }

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
            
            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid("Only board members can update groups.");
            }

            if(groupDto.Name.Contains(';') || groupDto.Name.Contains(':'))
            {
                return BadRequest("Group names cannot contain ';' or ':'.");
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

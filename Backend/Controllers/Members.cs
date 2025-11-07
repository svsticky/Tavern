using Microsoft.AspNetCore.Mvc;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Members(PostgresDbContext db) : ControllerBase
    {
        // GET: api/activities
        /// <summary>
        /// Lists all activities in the database.
        /// </summary>
        /// <returns>Said list.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Member>>> GetMembers(CancellationToken cancellationToken)
        {
            return await db.Members.ToListAsync(cancellationToken);
        }

        // GET: api/members/5
        /// <summary>
        /// Fetches a single member.
        /// </summary>
        /// <param name="id">The id of the member to fetch.</param>
        /// <returns>The full member.</returns> 
        [HttpGet("{id}")]
        public async Task<ActionResult<Member>> GetMember(uint id, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);

            return member != null ? member : NotFound();
        }

        // POST: api/members
        /// <summary>
        /// Creates a new member with a unique ID assigned by the database.
        /// </summary>
        /// <param name="member">The member to be added to the database.</param>
        /// <returns>Fully created member in body and api route of where to fetch it in the headers.</returns>
        [HttpPost]
        public async Task<ActionResult<Member>> PostMember(PostMemberDTO member, CancellationToken cancellationToken)
        {
            var newEntry = db.Members.Add(new Member
            {
                StudentNumber = member.StudentNumber,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber,
                Address = member.Address,
                DateOfBirth = member.DateOfBirth,
                PreferredLanguage = member.PreferredLanguage,
                RegisteredOn = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetMember), new { id = newEntry.Entity.Id }, newEntry.Entity);
        }

        // DELETE: api/members/5
        /// <summary>
        /// Deletes a member.
        /// </summary>
        /// <param name="id">The id of the member to delete.</param>
        /// <returns>Nothing, really.</returns>
        /// <remarks>
        /// Deleting a member will also delete all enrollments and study enrollments associated with said
        /// member.
        /// </remarks>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMember(uint id, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            db.Members.Remove(member);
            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}

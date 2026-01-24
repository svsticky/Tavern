using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
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
        public async Task<ActionResult<IEnumerable<MemberResponseDTO>>> GetMembers(CancellationToken cancellationToken)
        {
            return await db.Members
                .Select(m => new MemberResponseDTO
                {
                    Id = m.Id,
                    StudentNumber = m.StudentNumber,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    Address = m.Address,
                    DateOfBirth = m.DateOfBirth,
                    Notes = m.Notes,
                    RegisteredOn = m.RegisteredOn,
                    PreferredLanguage = m.PreferredLanguage,
                    StudyEnrollments = m.StudyEnrollments.Select(se => new StudyEnrollmentResponseDTO
                    {
                        Id = se.Id,
                        StudyId = se.StudyId,
                        StudyTitle = se.Study.Title, 
                        MemberId = se.MemberId,
                        MemberName = $"{m.FirstName} {m.LastName}",
                        EnrollmentDate = se.EnrollmentDate,
                        CompletionDate = se.CompletionDate,
                        Status = se.Status
                    }).ToList(),
                    GroupMemberships = db.GroupMemberships
                        .Where(gm => gm.MemberId == m.Id)
                        .Select(gm => new GroupMembershipResponseDTO
                        {
                            Id = gm.Id,
                            GroupId = gm.GroupId,
                            GroupName = gm.Group.Name, 
                            GroupType = gm.Group.Type,
                            MemberId = gm.MemberId,
                            MemberName = $"{m.FirstName} {m.LastName}",
                            MembershipYear = gm.MembershipYear,
                            RoleId = gm.Role != null ? gm.Role.Id : null,
                            RoleName = gm.Role != null ? gm.Role.Name : null
                        }).ToList()
                })
                .ToListAsync(cancellationToken);
        }

        // GET: api/members/5
        /// <summary>
        /// Fetches a single member.
        /// </summary>
        /// <param name="id">The id of the member to fetch.</param>
        /// <returns>The full member.</returns> 
        [HttpGet("{id}")]
        public async Task<ActionResult<MemberResponseDTO>> GetMember(uint id, CancellationToken cancellationToken)
        {
            var result = await db.Members
                .Where(m => m.Id == id)
                .Select(m => new MemberResponseDTO
                {
                    Id = m.Id,
                    StudentNumber = m.StudentNumber,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    Address = m.Address,
                    DateOfBirth = m.DateOfBirth,
                    Notes = m.Notes,
                    RegisteredOn = m.RegisteredOn,
                    PreferredLanguage = m.PreferredLanguage,
                    StudyEnrollments = m.StudyEnrollments.Select(se => new StudyEnrollmentResponseDTO
                    {
                        Id = se.Id,
                        StudyId = se.StudyId,
                        StudyTitle = se.Study.Title,
                        MemberId = se.MemberId,
                        MemberName = $"{m.FirstName} {m.LastName}",
                        EnrollmentDate = se.EnrollmentDate,
                        CompletionDate = se.CompletionDate,
                        Status = se.Status
                    }).ToList(),
                    GroupMemberships = db.GroupMemberships
                        .Where(gm => gm.MemberId == m.Id)
                        .Select(gm => new GroupMembershipResponseDTO
                        {
                            Id = gm.Id,
                            GroupId = gm.GroupId,
                            GroupName = gm.Group.Name,
                            GroupType = gm.Group.Type,
                            MemberId = gm.MemberId,
                            MemberName = $"{m.FirstName} {m.LastName}",
                            MembershipYear = gm.MembershipYear,
                            RoleId = gm.Role != null ? gm.Role.Id : null,
                            RoleName = gm.Role != null ? gm.Role.Name : null
                        }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (result == null) return NotFound();

            return Ok(result);
        }

        // POST: api/members
        /// <summary>
        /// Creates a new member with a unique ID assigned by the database.
        /// </summary>
        /// <param name="memberDto">The member to be added to the database.</param>
        /// <returns>Fully created member in body and api route of where to fetch it in the headers.</returns>
        [HttpPost]
        public async Task<ActionResult<Member>> PostMember(PostMemberDTO memberDto, CancellationToken cancellationToken)
        {
            var newMember = new Member
            {
                StudentNumber = memberDto.StudentNumber,
                FirstName = memberDto.FirstName,
                LastName = memberDto.LastName,
                Email = memberDto.Email,
                PhoneNumber = memberDto.PhoneNumber,
                Address = memberDto.Address,
                DateOfBirth = memberDto.DateOfBirth,
                PreferredLanguage = memberDto.PreferredLanguage,
                RegisteredOn = DateTimeOffset.UtcNow,
                StudyEnrollments = new List<StudyEnrollment>()
            };

            // Add study enrollments if provided
            if (memberDto.StudyEnrollments is not null)
            {
                foreach (var enrollmentDto in memberDto.StudyEnrollments)
                {
                    if(enrollmentDto.MemberId != newMember.Id)
                        return BadRequest("MemberId in StudyEnrollments must match the new member's ID.");
                    newMember.StudyEnrollments.Add(new StudyEnrollment
                    {
                        StudyId = enrollmentDto.StudyId,
                        EnrollmentDate = enrollmentDto.EnrollmentDate,
                        Status = enrollmentDto.Status
                    });
                }
            }

            db.Members.Add(newMember);
            await db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetMember), new { id = newMember.Id }, newMember);
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

        // PATCH: api/members/5
        /// <summary>
        /// Partially updates a member's details.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="patchDoc">The patch document containing the changes.</param>
        /// <returns>No Content.</returns>
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchMember(uint id, [FromBody] JsonPatchDocument<Member> patchDoc, CancellationToken cancellationToken)
        {
            if (patchDoc == null)
                return BadRequest();

            Member? member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null)
                return NotFound();

            // Pas de patch toe op het database object
            patchDoc.ApplyTo(member, ModelState);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PUT: api/members/5
        /// <summary>
        /// Updates a member's details.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="memberDto">The new details of the member.</param>
        /// <returns>The updated member.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMember(uint id, MemberUpdateDTO memberDto, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.StudentNumber = memberDto.StudentNumber;
            member.FirstName = memberDto.FirstName;
            member.LastName = memberDto.LastName;
            member.Email = memberDto.Email;
            member.PhoneNumber = memberDto.PhoneNumber;
            member.Address = memberDto.Address;
            member.DateOfBirth = memberDto.DateOfBirth;
            member.PreferredLanguage = memberDto.PreferredLanguage;
            member.Notes = memberDto.Notes;
            member.Gratie = memberDto.Gratie;
            member.LidVanVerdienste = memberDto.LidVanVerdienste;
            member.EreLid = memberDto.EreLid;
            member.Begunstiger = memberDto.Begunstiger;
            member.Suspended = memberDto.Suspended;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}

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
            var members = await db.Members
                .Include(m => m.StudyEnrollments)
                .ThenInclude(se => se.Study)
                .Include(m => m.Enrollments)
                .ToListAsync(cancellationToken);

            var result = members.Select(m => new MemberResponseDTO
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
                    MemberName = $"{se.Member.FirstName} {se.Member.LastName}",
                    EnrollmentDate = se.EnrollmentDate,
                    CompletionDate = se.CompletionDate,
                    Status = se.Status
                }).ToList(),
                CommissionMemberships = db.CommissionMemberships
                    .Where(cm => cm.MemberId == m.Id)
                    .Include(cm => cm.Commission)
                    .Select(cm => new CommissionMembershipResponseDTO
                    {
                        Id = cm.Id,
                        CommissionId = cm.CommissionId,
                        CommissionName = cm.Commission.Name,
                        MemberId = cm.MemberId,
                        MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                        MembershipYear = cm.MembershipYear,
                        RoleId = cm.Role != null ? cm.Role.Id : (uint?)null,
                        RoleName = cm.Role != null ? cm.Role.Name : null
                    }).ToList()
            });

            return Ok(result);
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
            var member = await db.Members
                .Include(m => m.StudyEnrollments)
                .ThenInclude(se => se.Study) // als je ook de study info wilt
                .Include(m => m.Enrollments)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (member is null) return NotFound();

            var result = new MemberResponseDTO
            {
                Id = member.Id,
                StudentNumber = member.StudentNumber,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber,
                Address = member.Address,
                DateOfBirth = member.DateOfBirth,
                Notes = member.Notes,
                RegisteredOn = member.RegisteredOn,
                PreferredLanguage = member.PreferredLanguage,
                StudyEnrollments = member.StudyEnrollments.Select(se => new StudyEnrollmentResponseDTO
                {
                    Id = se.Id,
                    StudyId = se.StudyId,
                    StudyTitle = se.Study.Title,
                    MemberId = se.MemberId,
                    MemberName = $"{se.Member.FirstName} {se.Member.LastName}",
                    EnrollmentDate = se.EnrollmentDate,
                    CompletionDate = se.CompletionDate,
                    Status = se.Status
                }).ToList(),
                CommissionMemberships = db.CommissionMemberships
                    .Where(cm => cm.MemberId == member.Id)
                    .Include(cm => cm.Commission)
                    .Select(cm => new CommissionMembershipResponseDTO
                    {
                        Id = cm.Id,
                        CommissionId = cm.CommissionId,
                        CommissionName = cm.Commission.Name,
                        MemberId = cm.MemberId,
                        MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                        MembershipYear = cm.MembershipYear,
                        RoleId = cm.Role != null ? cm.Role.Id : (uint?)null,
                        RoleName = cm.Role != null ? cm.Role.Name : null
                    }).ToList()
            };

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

        // PATCH: api/members/5/studentnumber
        /// <summary>
        /// Updates a member's student number.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newStudentNumber">The new student number for the member.</param
        /// <returns>No content.</returns>
        [HttpPatch("{id}/studentnumber")]
        public async Task<IActionResult> PatchMemberStudentNumber(uint id, uint newStudentNumber, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.StudentNumber = newStudentNumber;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/firstName
        /// <summary>
        /// Updates a member's first name.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newFirstName">The new first name for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/firstName")]
        public async Task<IActionResult> PatchMemberFirstName(uint id, string newFirstName, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.FirstName = newFirstName;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/lastName
        /// <summary>
        /// Updates a member's last name.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newLastName">The new last name for the member.</param
        /// <returns>No content.</returns>
        [HttpPatch("{id}/lastName")]
        public async Task<IActionResult> PatchMemberLastName(uint id, string newLastName, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.LastName = newLastName;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/email
        /// <summary>
        /// Updates a member's email.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newEmail">The new email for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/email")]
        public async Task<IActionResult> PatchMemberEmail(uint id, string newEmail, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.Email = newEmail;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/phoneNumber
        /// <summary>
        /// Updates a member's phone number.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newPhoneNumber">The new phone number for the member.</param
        /// <returns>No content.</returns>
        [HttpPatch("{id}/phoneNumber")]
        public async Task<IActionResult> PatchMemberPhoneNumber(uint id, string newPhoneNumber, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.PhoneNumber = newPhoneNumber;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/address
        /// <summary>
        /// Updates a member's address.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newAddress">The new address for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/address")]
        public async Task<IActionResult> PatchMemberAddress(uint id, string newAddress, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.Address = newAddress;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/dateOfBirth
        /// <summary>
        /// Updates a member's date of birth.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newDateOfBirth">The new date of birth for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/dateOfBirth")]
        public async Task<IActionResult> PatchMemberDateOfBirth(uint id, DateTimeOffset newDateOfBirth, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.DateOfBirth = newDateOfBirth;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/preferredLanguage
        /// <summary>
        /// Updates a member's preferred language.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newPreferredLanguage">The new preferred language for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/preferredLanguage")]
        public async Task<IActionResult> PatchMemberPreferredLanguage(uint id, Language newPreferredLanguage, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.PreferredLanguage = newPreferredLanguage;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/notes
        /// <summary>
        /// Updates a member's notes.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newNotes">The new notes for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/notes")]
        public async Task<IActionResult> PatchMemberNotes(uint id, string newNotes, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.Notes = newNotes;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/gratie
        /// <summary>
        /// Updates a member's gratie status.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newGratie">The new gratie status for the member.</
        /// returns>No content.</returns>
        [HttpPatch("{id}/gratie")]
        public async Task<IActionResult> PatchMemberGratie(uint id, bool newGratie, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.Gratie = newGratie;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/lidVanVerdienste
        /// <summary>
        /// Updates a member's lid van verdienste status.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newLidVanVerdienste">The new lid van verdienste status for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/lidVanVerdienste")]
        public async Task<IActionResult> PatchMemberLidVanVerdienste(uint id, bool newLidVanVerdienste, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.LidVanVerdienste = newLidVanVerdienste;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/ereLid
        /// <summary>
        /// Updates a member's ere lid status.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newEreLid">The new ere lid status for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/ereLid")]
        public async Task<IActionResult> PatchMemberEreLid(uint id, bool newEreLid, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.EreLid = newEreLid;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/begunstiger
        /// <summary>
        /// Updates a member's begunstiger status.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newBegunstiger">The new begunstiger status for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/begunstiger")]
        public async Task<IActionResult> PatchMemberBegunstiger(uint id, bool newBegunstiger, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.Begunstiger = newBegunstiger;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/members/5/suspended
        /// <summary>
        /// Updates a member's suspended status.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="newSuspended">The new suspended status for the member.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/suspended")]
        public async Task<IActionResult> PatchMemberSuspended(uint id, bool newSuspended, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            member.Suspended = newSuspended;

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

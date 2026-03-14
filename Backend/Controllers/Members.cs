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
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db))
            {
                return Forbid("Only board members can view members.");
            }

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
                            RoleAliasId = gm.RoleAlias != null ? gm.RoleAlias.Id : null,
                            RoleAliasName = gm.RoleAlias != null ? gm.RoleAlias.Name : null
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
        public async Task<ActionResult<MemberResponseDTO>> GetMember(Guid id, CancellationToken cancellationToken)
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            bool isBoard = PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db);

            if(!isBoard && id != userId)
            {
                return Forbid("Only board members can view details of other members.");
            }

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
                    Notes = isBoard ? m.Notes : null,
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
                            RoleAliasId = gm.RoleAlias != null ? gm.RoleAlias.Id : null,
                            RoleAliasName = gm.RoleAlias != null ? gm.RoleAlias.Name : null
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
        [AllowAnonymous]
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

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                db.Members.Add(newMember);
                db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
                {
                    KeycoakId = newMember.Id,
                    TaskType = KeycloakTaskType.Create
                });

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return CreatedAtAction(nameof(GetMember), new { id = newMember.Id }, newMember);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StatusCode(500, "An error occurred while creating the member.");
            }
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
        public async Task<IActionResult> DeleteMember(Guid id, CancellationToken cancellationToken)
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db) && id != userId)
            {
                return Forbid("Only board members can delete other members.");
            }

            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                db.Members.Remove(member);

                db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
                {
                    KeycoakId = member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                    TaskType = KeycloakTaskType.Delete
                });

                await db.SaveChangesAsync(cancellationToken);

                return NoContent();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StatusCode(500, "An error occurred while deleting the member.");
            }
        }

        // PATCH: api/members/5
        /// <summary>
        /// Partially updates a member's details.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="patchDoc">The patch document containing the changes.</param>
        /// <returns>No Content.</returns>
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchMember(Guid id, [FromBody] JsonPatchDocument<Member> patchDoc, CancellationToken cancellationToken)
        {
            if(patchDoc.Operations.Any(op => 
                op.path.TrimStart('/').ToLower() == "id" || 
                op.path.TrimStart('/').ToLower() == "keycloakid"))
            {
                return BadRequest("Updating 'id' or 'keycloakid' is not allowed.");
            }

            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);    

            bool isUpdatingSensitiveData = patchDoc.Operations.Any(op => 
                Member.RestrictedFields.Any(field => op.path.TrimStart('/').ToLower().Equals(field)));

            bool isBoard = PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db);
            bool isOwnProfile = id == userId;

            if (!isBoard)
            {
                if (!isOwnProfile)
                {
                    return Forbid("You can only update your own profile.");
                }

                if (isUpdatingSensitiveData)
                {
                    return Forbid("Only board members can update sensitive member details.");
                }
            }

            if (patchDoc == null)
                return BadRequest();

            Member? member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null)
                return NotFound();

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                patchDoc.ApplyTo(member, ModelState);

                if (!ModelState.IsValid)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return BadRequest(ModelState);
                }

                db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
                {
                    KeycoakId = member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                    TaskType = KeycloakTaskType.Sync
                });

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return NoContent();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StatusCode(500, "An error occurred while updating the member.");
            }
        }

        // PUT: api/members/5
        /// <summary>
        /// Updates a member's details.
        /// </summary>
        /// <param name="id">The id of the member to update.</param>
        /// <param name="memberDto">The new details of the member.</param>
        /// <returns>The updated member.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMember(Guid id, MemberUpdateDTO memberDto, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) return NotFound();
            
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroup.Board, db) && id != userId)
            {
                return Forbid("Only board members can update members.");
            }

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
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

                db.KeyCloakOutboxTasks.Add(new KeyCloakOutboxTask
                {
                    KeycoakId = member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                    TaskType = KeycloakTaskType.Sync
                });

                await db.SaveChangesAsync(cancellationToken);
                return NoContent();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StatusCode(500, "An error occurred while updating the member.");
            }
        }
    }
}

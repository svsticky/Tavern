using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class MemberService(
        PostgresDbContext db,
        IPermissionService permissionService,
        IPaymentValidationService paymentValidationService,
        IStorageService storageService
    ) : IMemberService
    {
        public async Task<List<MemberResponseDTO>> GetMembers(GetMembersDto dto, Guid userId, CancellationToken cancellationToken)
        {
            if (!permissionService.IsBoardOrCandidateBoardMember(userId))
                throw new UnauthorizedAccessException("Only board members can view members.");

            var query = db.Members.AsQueryable();

            if (!string.IsNullOrEmpty(dto.Search))
            {
                query = query.Where(m => m.FirstName.Contains(dto.Search) || m.LastName.Contains(dto.Search) || m.Email.Contains(dto.Search) || m.StudentNumber.ToString().Contains(dto.Search) || m.PhoneNumber.Contains(dto.Search));
            }

            int pageSize = dto.PageSize > 0 ? dto.PageSize : 50;
            int skip = (dto.Page > 0 ? dto.Page - 1 : 0) * pageSize;

            return await query
                .OrderBy(m => m.LastName)
                .Skip(skip)
                .Take(pageSize)
                .Select(m => new MemberResponseDTO
                {
                    Id = m.Id,
                    StudentNumber = m.StudentNumber,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    Street = m.Street,
                    HouseNumber = m.HouseNumber,
                    PostalCode = m.PostalCode,
                    City = m.City,
                    DateOfBirth = m.DateOfBirth,
                    ParentPhoneNumber = m.ParentPhoneNumber,
                    MailSubscriptions = m.MailSubscriptions,
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

        public async Task<MemberResponseDTO?> GetMember(Guid id, Guid userId, bool isBoard, CancellationToken cancellationToken)
        {
            if (!isBoard && id != userId)
                throw new UnauthorizedAccessException();

            return await db.Members
                .Where(m => m.Id == id)
                .Select(m => new MemberResponseDTO
                {
                    Id = m.Id,
                    StudentNumber = m.StudentNumber,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    Street = m.Street,
                    HouseNumber = m.HouseNumber,
                    PostalCode = m.PostalCode,
                    City = m.City,
                    DateOfBirth = m.DateOfBirth,
                    ParentPhoneNumber = m.ParentPhoneNumber,
                    MailSubscriptions = m.MailSubscriptions,
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
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Member> CreateMember(PostMemberDTO dto, CancellationToken cancellationToken)
        {
            if (dto.DateOfBirth > DateTimeOffset.UtcNow.AddYears(-18) &&
                string.IsNullOrEmpty(dto.ParentPhoneNumber))
            {
                throw new ArgumentException("Parent phone number required for minors.");
            }

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                var member = new Member
                {
                    StudentNumber = dto.StudentNumber,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Street = dto.Street,
                    HouseNumber = dto.HouseNumber,
                    PostalCode = dto.PostalCode,
                    City = dto.City,
                    DateOfBirth = dto.DateOfBirth,
                    ParentPhoneNumber = dto.ParentPhoneNumber,
                    MailSubscriptions = dto.MailSubscriptions,
                    PreferredLanguage = dto.PreferredLanguage,
                    RegisteredOn = DateTimeOffset.UtcNow,
                    StudyEnrollments = new List<StudyEnrollment>()
                };

                StateValidator.Validate(member);

                db.Members.Add(member);
                await db.SaveChangesAsync(cancellationToken);
                
                if (dto.StudyEnrollments != null)
                {
                    foreach (var se in dto.StudyEnrollments)
                    {
                        var enrollment = new StudyEnrollment
                        {
                            MemberId = member.Id,
                            StudyId = se.StudyId,
                            EnrollmentDate = se.EnrollmentDate,
                            Status = se.Status
                        };

                        StateValidator.Validate(enrollment);

                        db.StudyEnrollments.Add(enrollment);
                    }

                }

                db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
                {
                    KeycloakId = member.Id,
                    TaskType = KeycloakTaskType.Create
                });

                await db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return member;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> DeleteMember(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null) return false;

            if (!paymentValidationService.MemberHasPaidAllActivities(member))
                throw new InvalidOperationException("Member has unpaid activities.");

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                db.Members.Remove(member);
                await storageService.DeleteFileAsync("profile-pictures", member.ProfilePicturePath ?? "");

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return true;
        }

        public async Task<bool> PatchMember(Guid id, JsonPatchDocument<Member> patchDoc, Guid userId, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null) return false;

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                patchDoc.ApplyTo(member);
                StateValidator.Validate(member);

                db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
                {
                    KeycloakId = member.KeycloakId ?? throw new InvalidOperationException("Member does not have a Keycloak ID."),
                    TaskType = KeycloakTaskType.Sync
                });

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return true;
        }

        public async Task<bool> UpdateMember(Guid id, MemberUpdateDTO dto, Guid userId, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null) return false;

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                member.StudentNumber = dto.StudentNumber;
                member.FirstName = dto.FirstName;
                member.LastName = dto.LastName;
                member.Email = dto.Email;
                member.PhoneNumber = dto.PhoneNumber;
                member.Street = dto.Street;
                member.HouseNumber = dto.HouseNumber;
                member.PostalCode = dto.PostalCode;
                member.City = dto.City;
                member.DateOfBirth = dto.DateOfBirth;
                member.ParentPhoneNumber = dto.ParentPhoneNumber;
                member.PreferredLanguage = dto.PreferredLanguage;

                StateValidator.Validate(member);

                db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
                {
                    KeycloakId = member.KeycloakId ?? throw new InvalidOperationException("Member does not have a Keycloak ID."),
                    TaskType = KeycloakTaskType.Sync
                });

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<FileResultDto?> GetProfilePictureFile(string path)
        {
            var file = await storageService.GetFileAsync("profile-pictures", path);

            if (file == null) return null;

            return new FileResultDto
            {
                Stream = file.Stream,
                ContentType = file.ContentType
            };
        }

        public async Task<bool> DeleteProfilePicture(Guid id)
        {
            var member = await db.Members.FindAsync(id);
            if (member == null) return false;

            if (string.IsNullOrEmpty(member.ProfilePicturePath))
                return true;

            string oldPath = member.ProfilePicturePath;

            member.ProfilePicturePath = null;
            member.ProfilePictureFileName = null;

            await db.SaveChangesAsync();

            await storageService.DeleteFileAsync("profile-pictures", oldPath);

            return true;
        }

        public async Task<Member?> GetMemberEntity(Guid id)
        {
            return await db.Members.FindAsync(id);
        }
    }
}
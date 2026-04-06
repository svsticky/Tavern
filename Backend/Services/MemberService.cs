using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
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
        public async Task<List<MemberResponseDTO>> GetMembers(Guid userId, CancellationToken cancellationToken)
        {
            if (!permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board))
                throw new UnauthorizedAccessException("Only board members can view members.");

            return await db.Members
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

            StateValidateUtils.Validate(member);

            db.Members.Add(member);
            await db.SaveChangesAsync(cancellationToken);

            return member;
        }

        public async Task<bool> DeleteMember(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null) return false;

            if (!paymentValidationService.MemberHasPaidAllActivities(member))
                throw new InvalidOperationException("Member has unpaid activities.");

            db.Members.Remove(member);
            await db.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> PatchMember(Guid id, JsonPatchDocument<Member> patchDoc, Guid userId, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null) return false;

            patchDoc.ApplyTo(member);
            StateValidateUtils.Validate(member);

            await db.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> UpdateMember(Guid id, MemberUpdateDTO dto, Guid userId, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null) return false;

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

            StateValidateUtils.Validate(member);

            await db.SaveChangesAsync(cancellationToken);
            return true;
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

        public bool IsBoard(Guid userId)
        {
            return permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board);
        }
    }
}
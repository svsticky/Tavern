using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Projections;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Mollie.Api.Client.Abstract;

namespace Backend.Services
{
    public class MemberService(
        PostgresDbContext db,
        IPermissionService permissionService,
        IPaymentValidationService paymentValidationService,
        IStorageService storageService,
        IPaymentClient mollieClient,
        KeycloakOutboxWorker keycloakOutboxWorker
    ) : IMemberService
    {
        public async Task<List<MemberResponseDTO>> GetMembers(GetMembersDto dto, Guid userId, CancellationToken cancellationToken)
        {
            EnsureBoardMember(userId, "Only board members can view members.");

            return await db.Members
                .AsQueryable()
                .Filter(dto)
                .OrderBy(m => m.LastName)
                .ApplyPaging(dto)
                .Select(MemberProjections.ToListDto())
                .ToListAsync(cancellationToken);
        }

        public async Task<MemberResponseDTO?> GetMember(Guid id, Guid userId, bool isBoard, CancellationToken cancellationToken)
        {
            if (!isBoard && id != userId)
                throw new UnauthorizedAccessException();

            return await db.Members
                .Where(m => m.Id == id)
                .Select(MemberProjections.ToDetailDto(isBoard))
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
                await RemoveExistingMemberWithSameEmail(dto.Email, cancellationToken);

                var member = BuildMember(dto);

                StateValidator.Validate(member);

                db.Members.Add(member);
                await db.SaveChangesAsync(cancellationToken);
                
                if (dto.StudyEnrollments != null)
                {
                    AddStudyEnrollments(member.Id, dto.StudyEnrollments);
                }

                await keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Create, member.Id);

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

            patchDoc.Operations.RemoveAll(op => op.path.Equals("/email", StringComparison.OrdinalIgnoreCase));
            
            try
            {
                patchDoc.ApplyTo(member);
                StateValidator.Validate(member);

                await keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Sync, member.KeycloakId ?? throw new InvalidOperationException("Member does not have a Keycloak ID."));

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
                member.PhoneNumber = dto.PhoneNumber;
                member.Street = dto.Street;
                member.HouseNumber = dto.HouseNumber;
                member.PostalCode = dto.PostalCode;
                member.City = dto.City;
                member.DateOfBirth = dto.DateOfBirth;
                member.ParentPhoneNumber = dto.ParentPhoneNumber;
                member.PreferredLanguage = dto.PreferredLanguage;

                StateValidator.Validate(member);

                await keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Sync, member.KeycloakId ?? throw new InvalidOperationException("Member does not have a Keycloak ID."));

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

        private void EnsureBoardMember(Guid userId, string errorMessage)
        {
            if (!permissionService.IsBoardOrCandidateBoardMember(userId))
                throw new UnauthorizedAccessException(errorMessage);
        }

        private async Task RemoveExistingMemberWithSameEmail(string email, CancellationToken ct)
        {
            var existingMember = await db.Members.FirstOrDefaultAsync(m => m.Email == email, ct);
            if (existingMember == null)
                return;

            var existingPayment = await db.MembershipPayments
                .Where(p => p.MemberId == existingMember.Id)
                .FirstOrDefaultAsync(ct);

            if (existingPayment != null)
            {
                var molliePayment = await mollieClient.GetPaymentAsync(existingPayment.MollieId);

                if (molliePayment.Status == "paid")
                {
                    throw new InvalidOperationException("Existing member with unpaid payments found.");
                }

                if (molliePayment.Status == "pending")
                {
                    await mollieClient.CancelPaymentAsync(existingPayment.MollieId);
                }

                db.MembershipPayments.Remove(existingPayment);
            }

            db.Members.Remove(existingMember);
            await keycloakOutboxWorker.EnqueueTask(
                KeycloakTaskType.Delete,
                existingMember.KeycloakId ?? throw new Exception("Member isn't synced with Keycloak yet, cannot sync payment status.")
            );
        }

        private static Member BuildMember(PostMemberDTO dto)
        {
            return new Member
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
        }

        private void AddStudyEnrollments(Guid memberId, IEnumerable<PostStudyEnrollmentDTO> studyEnrollments)
        {
            foreach (var se in studyEnrollments)
            {
                var enrollment = new StudyEnrollment
                {
                    MemberId = memberId,
                    StudyId = se.StudyId,
                    EnrollmentDate = se.EnrollmentDate,
                    Status = se.Status
                };

                StateValidator.Validate(enrollment);

                db.StudyEnrollments.Add(enrollment);
            }
        }
    }
}

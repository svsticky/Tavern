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
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.Members
                .AsQueryable()
                .Filter(dto)
                .OrderBy(m => m.LastName)
                .ApplyPaging(dto)
                .Select(MemberProjections.ToDto(userId, true))
                .ToListAsync(cancellationToken);
        }

        public async Task<MemberResponseDTO?> GetMember(Guid userIdFromUserToGet, Guid userId, CancellationToken cancellationToken)
        {
            if (userId != userIdFromUserToGet)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.Members
                .Where(m => m.Id == userIdFromUserToGet)
                .Select(MemberProjections.ToDto(userId, permissionService.IsBoardOrCandidateBoardMember(userId)))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Member> CreateMember(PostMemberDTO dto, CancellationToken cancellationToken)
        {
            // Check if member is a minor and if so, require parent phone number
            if (dto.DateOfBirth > DateTimeOffset.UtcNow.AddYears(-18) &&
                string.IsNullOrEmpty(dto.ParentPhoneNumber))
            {
                throw new ArgumentException("Parent phone number required for minors.");
            }

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Remove any existing member with same mail if they didn't pay membership
                await RemoveExistingMemberWithSameEmail(dto.Email, cancellationToken);

                // Creat the member
                var member = BuildMember(dto);
                StateValidator.Validate(member);
                db.Members.Add(member);

                // Add the studyenrollments if provided
                if (dto.StudyEnrollments != null)
                {
                    AddStudyEnrollments(member.Id, dto.StudyEnrollments);
                }

                // Sync with Keycloak 
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

        public async Task DeleteMember(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync(id, cancellationToken);

            if (id != userId)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            if (member == null) 
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            // Member must pay all activities before they can be deleted
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
        }

        public async Task PatchMember(Guid id, JsonPatchDocument<Member> patchDoc, Guid userId, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) 
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // Some settings a member should not be able to edit themselves, and if they try to edit those, we check if they are board members
            if(member.Id != userId || patchDoc.Operations.Any(operation => Member.RestrictedFields.Contains(operation.path.ToLower())))
                permissionService.EnsureBoardOrCandidateBoardMember(userId);
            
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
        }

        public async Task UpdateMember(Guid id, MemberUpdateDTO dto, Guid userId, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null)
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // Some settings a member should not be able to edit themselves, and if they try to edit those, we check if they are board members
            if(member.Id != userId 
                || member.Email != dto.Email
                || member.Id != userId
                || dto.StudentNumber != member.StudentNumber 
                || dto.FirstName != member.FirstName 
                || dto.LastName != member.LastName 
                || dto.DateOfBirth != member.DateOfBirth
                || dto.Notes != member.Notes
                || dto.Gratie != member.Gratie
                || dto.LidVanVerdienste != member.LidVanVerdienste
                || dto.EreLid != member.EreLid
                || dto.Begunstiger != member.Begunstiger
                || dto.Suspended != member.Suspended)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            try
            {
                ApplyMemberUpdate(member, dto);
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
        }

        public async Task DeleteProfilePicture(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            if(id != userId)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            var member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null)
                return;

            if (string.IsNullOrEmpty(member.ProfilePicturePath))
                throw new KeyNotFoundException("Profile picture not found for this member.");

            string oldPath = member.ProfilePicturePath;

            member.ProfilePicturePath = null;
            member.ProfilePictureFileName = null;

            await db.SaveChangesAsync(cancellationToken);

            await storageService.DeleteFileAsync("profile-pictures", oldPath);
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

                // Make sure there is no paid membership with the same email, if there is, we don't want to delete the member
                if (molliePayment.Status == "paid")
                {
                    throw new InvalidOperationException("Existing member with same email address that has paid membership found.");
                }

                // If there is a pending payment, we cancel it to prevent the member from paying for a membership they won't get
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

        private static void ApplyMemberUpdate(Member member, MemberUpdateDTO dto)
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

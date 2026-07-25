using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Projections;
using Backend.Validators;
using Backend.QueryExtensions;
using Backend.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Backend.Services.PaymentServices;

namespace Backend.Repositories
{
    /// <summary>
    /// Implements member management and profile-related operations.
    /// </summary>
    public class MemberRepository(
        PostgresDbContext db,
        IPermissionService permissionService,
        IPaymentValidationService paymentValidationService,
        IStorageService storageService,
        AbstractPaymentService paymentService,
        AuthOutboxWorker authOutboxWorker,
        MailSubscriptionOutboxWorker mailSubscriptionOutboxWorker,
        IAuthService authService,
        IMemoryCache memoryCache,
        ILogger<MemberRepository> logger
    ) : IMemberRepository
    {
        /// <inheritdoc />
        public async Task<List<MemberResponseDTO>> GetMembers(GetMembersDto dto, Guid userId, CancellationToken cancellationToken)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            var members = await db.Members
                .AsQueryable()
                .Filter(dto)
                .OrderBy(m => m.LastName)
                .ApplyPaging(dto)
                .Include(m => m.StudyEnrollments).ThenInclude(se => se.Study)
                .Include(m => m.GroupMemberships).ThenInclude(gm => gm.Group)
                .Include(m => m.GroupMemberships).ThenInclude(gm => gm.RoleAlias)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var mapper = MemberProjections.ToDto(userId, true).Compile();
            
            return members.Select(m => mapper(m)).ToList();
        }

        /// <inheritdoc />
        public async Task<MemberResponseDTO?> GetMember(Guid userIdFromUserToGet, Guid userId, CancellationToken cancellationToken)
        {
            if (userId != userIdFromUserToGet)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.Members
                .Where(m => m.Id == userIdFromUserToGet)
                .Include(m => m.StudyEnrollments)
                    .ThenInclude(se => se.Study)
                .Include(m => m.GroupMemberships)
                    .ThenInclude(gm => gm.Group)
                .Include(m => m.GroupMemberships)
                    .ThenInclude(gm => gm.RoleAlias)
                .Select(MemberProjections.ToDto(userId, permissionService.IsBoardOrCandidateBoardMember(userId)))
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Member> CreateMember(PostMemberDTO dto, Guid? userId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating member with email {Email}.", dto.Email);

            // Check if begunstiger, and if so: make sure it's done by a board member, otherwise check if at least 1 study enrollment
            if (dto.Begunstiger.HasValue && dto.Begunstiger.Value)
            {
                if(userId == null)
                    throw new UnauthorizedAccessException();
                permissionService.EnsureBoardOrCandidateBoardMember(userId.Value);
            }
            else
            {
                if(dto.StudyEnrollments == null || dto.StudyEnrollments.Count == 0)
                    throw new ArgumentException("Member must be enrolled to atleast one study.");
                
                if(dto.StudentNumber.Trim() == "" || !int.TryParse(dto.StudentNumber, out var _))
                    throw new ArgumentException("Student number must be a number.");

                var studyStartDatesSetting = (await db.Settings.FindAsync(new object[] { "StudyStartDates" }, cancellationToken))?.Value ?? "09-01,02-01";
                Validators.StudyEnrollmentValidator.ValidateEnrollmentDates(dto.StudyEnrollments, studyStartDatesSetting);
            }

            // Check if date of birth is in the past
            if(dto.DateOfBirth >= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException("Date of birth must be in the past.");
            }

            // Check if member is a minor and if so, require parent phone number
            if (dto.DateOfBirth > DateTimeOffset.UtcNow.AddYears(-18) &&
                string.IsNullOrEmpty(dto.ParentPhoneNumber))
            {
                throw new ArgumentException("Parent phone number required for minors.");
            }

            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Remove any existing member with same mail if they didn't pay membership
                await RemoveExistingMemberWithSameEmail(dto.Email, cancellationToken);

                // Create the member
                var member = BuildMember(dto);
                StateValidator.Validate(member);
                db.Members.Add(member);

                // Add the studyenrollments if provided
                if (dto.StudyEnrollments != null)
                {
                    AddStudyEnrollments(member.Id, dto.StudyEnrollments);
                }

                // Sync with the auth system 
                await authOutboxWorker.EnqueueTask(AuthTaskType.Create, member.Id);

                // Enqueue mail subscription update
                mailSubscriptionOutboxWorker.EnqueueTask(member.Email, member.MailSubscriptions, db);

                await db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                logger.LogInformation("Created member {MemberId}.", member.Id);
                return member;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed creating member with email {Email}.", dto.Email);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteMember(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting member {MemberId}. Requested by {UserId}.", id, userId);
            var member = await db.Members.FindAsync(id, cancellationToken);

            if (id != userId)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            if (member == null) 
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            // Member must pay all activities before they can be deleted
            if (!paymentValidationService.MemberHasPaidAllActivities(member))
                throw new InvalidOperationException("Member has unpaid activities.");

            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await authOutboxWorker.EnqueueTask(AuthTaskType.Delete, member.AuthSystemUserId ?? throw new InvalidOperationException("User not synced in the authsystem yet."));

                db.Members.Remove(member);
                mailSubscriptionOutboxWorker.EnqueueTask(member.Email, 0, db);
                
                if (!string.IsNullOrEmpty(member.ProfilePicturePath))
                {
                    await storageService.DeleteFileAsync("profile-pictures", member.ProfilePicturePath);
                    memoryCache.Remove($"prof-pic-{member.ProfilePicturePath}");
                }

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed deleting member {MemberId}.", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task PatchMember(Guid id, JsonPatchDocument<Member> patchDoc, Guid userId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Patching member {MemberId}. Requested by {UserId}.", id, userId);
            var member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null) 
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // Some settings a member should not be able to edit themselves, and if they try to edit those, we check if they are board members
            if(member.Id != userId || patchDoc.Operations.Any(operation => Member.RestrictedFields.Contains(operation.path.ToLower())))
                permissionService.EnsureBoardOrCandidateBoardMember(userId);
            
            try
            {
                patchDoc.ApplyTo(member);
                StateValidator.Validate(member);

                await authOutboxWorker.EnqueueTask(AuthTaskType.Sync, member.AuthSystemUserId ?? throw new InvalidOperationException("Member does not have a authentication system ID."));

                mailSubscriptionOutboxWorker.EnqueueTask(member.Email, member.MailSubscriptions, db);

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed patching member {MemberId}.", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateMember(Guid id, MemberUpdateDTO dto, Guid userId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating member {MemberId}. Requested by {UserId}.", id, userId);
            var member = await db.Members.FindAsync(id, cancellationToken);
            if (member == null)
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

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

                await authOutboxWorker.EnqueueTask(AuthTaskType.Sync, member.AuthSystemUserId ?? throw new InvalidOperationException("Member does not have a authentication system ID."));

                mailSubscriptionOutboxWorker.EnqueueTask(member.Email, member.MailSubscriptions, db);

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed updating member {MemberId}.", id);
                throw;
            }
        }

        /// <inheritdoc />
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
            memoryCache.Remove($"prof-pic-{oldPath}");
            logger.LogInformation("Deleted profile picture for member {MemberId}.", id);
        }

        /// <inheritdoc />
        public async Task RefreshEmail(Guid id, CancellationToken cancellationToken)
        {
            var member = await db.Members.FirstOrDefaultAsync((member) => member.AuthSystemUserId == id);
            if (member == null)                
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                mailSubscriptionOutboxWorker.EnqueueTask(member.Email, 0, db);
                await authOutboxWorker.EnqueueTask(AuthTaskType.RefreshEmail, member.AuthSystemUserId ?? throw new InvalidOperationException("Member does not have a authentication system ID."));
                var newMail = await authService.GetEmail(member.AuthSystemUserId ?? throw new InvalidOperationException("Member does not have a authentication system ID."));
                mailSubscriptionOutboxWorker.EnqueueTask(newMail, member.MailSubscriptions, db);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Failed updating member email {MemberId}.", id);
                throw;
            }
        }

        private async Task RemoveExistingMemberWithSameEmail(string email, CancellationToken ct)
        {
            var existingMember = await db.Members
                .Include(m => m.StudyEnrollments)
                .ThenInclude(se => se.Study)
                .FirstOrDefaultAsync(m => m.Email == email, ct);
            if (existingMember == null)
                return;

            if(existingMember.Begunstiger)
                throw new InvalidOperationException("Existing member with same email address found.");

            if((await db.Settings.FindAsync("MastersShouldPayMembership"))?.Value != "1" &&  existingMember.StudyEnrollments.Any(se => se.Study.Type == StudyType.Master))
                throw new InvalidOperationException("Existing member with same email address found.");

            if((await db.Settings.FindAsync("GratieShouldPayMembership"))?.Value != "1" && existingMember.Gratie)
                throw new InvalidOperationException("Existing member with same email address found.");
            
            if((await db.Settings.FindAsync("ErelidShouldPayMembership"))?.Value != "1" && existingMember.EreLid)
                throw new InvalidOperationException("Existing member with same email address found.");
                
            if((await db.Settings.FindAsync("LidVanVerdiensteShouldPayMembership"))?.Value != "1" && existingMember.LidVanVerdienste)
                throw new InvalidOperationException("Existing member with same email address found.");

            var existingPayment = await db.MembershipPayments
                .Where(p => p.MemberId == existingMember.Id)
                .FirstOrDefaultAsync(ct);

            if (existingPayment != null)
            {
                var paymentResponse = await paymentService.GetPaymentAsync(existingPayment.PaymentServiceId);

                // Make sure there is no paid membership with the same email, if there is, we don't want to delete the member
                if (paymentResponse.Status == PaymentStatus.Paid)
                {
                    throw new InvalidOperationException("Existing member with same email address found.");
                }

                // If there is a pending payment, we cancel it to prevent the member from paying for a membership they won't get
                if (paymentResponse.Status == PaymentStatus.Pending)
                {
                    await paymentService.CancelPaymentAsync(existingPayment.PaymentServiceId);
                }

                db.MembershipPayments.Remove(existingPayment);
            }

            db.Members.Remove(existingMember);
            await authOutboxWorker.EnqueueTask(
                AuthTaskType.Delete,
                existingMember.AuthSystemUserId ?? throw new Exception("Member isn't synced with the authentication system yet.")
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
                StudyEnrollments = new List<StudyEnrollment>(),
                Begunstiger = dto.Begunstiger != null ? dto.Begunstiger.Value : false
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

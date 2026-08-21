using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.QueryExtensions;
using Backend.Services.PaymentServices;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.Domain
{
    /// <summary>
    /// Implements member management and profile-related operations.
    /// </summary>
    public class MemberService(
        PostgresDbContext db,
        IPermissionService permissionService,
        IPaymentValidationService paymentValidationService,
        IStorageService storageService,
        AbstractPaymentService paymentService,
        AuthOutboxWorker authOutboxWorker,
        MailSubscriptionOutboxWorker mailSubscriptionOutboxWorker,
        IAuthService authService,
        IMailSubscriptionService mailSubscriptionService,
        IMailinglistCurationService mailinglistCurationService,
        IMemoryCache memoryCache,
        ILogger<MemberService> logger
    ) : IMemberService
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

            var mapper = MemberResponseDTO.ToDto(userId, true).Compile();

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
                .Select(MemberResponseDTO.ToDto(userId, permissionService.IsBoardOrCandidateBoardMember(userId)))
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Member> CreateMember(PostMemberDTO dto, Guid? userId, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating member with email {Email}.", dto.Email);

            // Check if begunstiger, and if so: make sure it's done by a board member, otherwise check if at least 1 study enrollment
            if (dto.Begunstiger.HasValue && dto.Begunstiger.Value)
            {
                if (userId == null)
                    throw new UnauthorizedAccessException();
                permissionService.EnsureBoardOrCandidateBoardMember(userId.Value);
            }
            else
            {
                if (dto.StudyEnrollments == null || dto.StudyEnrollments.Count == 0)
                    throw new ArgumentException("Member must be enrolled to atleast one study.");

                if (dto.StudentNumber.Trim() == "" || !int.TryParse(dto.StudentNumber, out var _))
                    throw new ArgumentException("Student number must be a number.");

                var studyStartDatesSetting = (await db.Settings.FindAsync(new object[] { "StudyStartDates" }, cancellationToken))?.Value ?? "09-01,02-01";
                Validators.StudyEnrollmentValidator.ValidateEnrollmentDates(dto.StudyEnrollments, studyStartDatesSetting);
            }

            // Check if date of birth is in the past
            if (dto.DateOfBirth >= DateTimeOffset.UtcNow)
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
                authOutboxWorker.EnqueueTask(AuthTaskType.Create, member.Id, db);

                // Enqueue mail subscription update
                mailSubscriptionOutboxWorker.EnqueueUpdateSubscriptionsTask(member.Email, dto.SubscribedMailinglistIds ?? [], db);

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

            if (member == null)
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            if (id != userId)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            // Member must pay all activities before they can be deleted
            if (!paymentValidationService.MemberHasPaidAllActivities(member))
                throw new InvalidOperationException("Member has unpaid activities.");

            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                authOutboxWorker.EnqueueTask(AuthTaskType.Delete, member.AuthSystemUserId ?? throw new InvalidOperationException("User not synced in the authsystem yet."), db);

                // Anonymize member PII while retaining foreign keys and financial history
                var oldEmail = member.Email;

                mailSubscriptionOutboxWorker.EnqueueDeleteTask(oldEmail, db);
                member.FirstName = "Deleted";
                member.LastName = "Member";
                member.Email = $"deleted-{member.Id}@deleted.local";
                member.StudentNumber = $"DELETED-{member.Id}";
                member.PhoneNumber = "0000000000";
                member.ParentPhoneNumber = null;
                member.Street = "Deleted";
                member.HouseNumber = "0";
                member.PostalCode = "0000AA";
                member.City = "Deleted";
                member.Notes = null;
                member.Gratie = false;
                member.LidVanVerdienste = false;
                member.EreLid = false;
                member.Begunstiger = false;
                member.Suspended = false;
                member.IsDeleted = true;

                if (!string.IsNullOrEmpty(member.ProfilePicturePath))
                {
                    await storageService.DeleteFileAsync("profile-pictures", member.ProfilePicturePath);
                    memoryCache.Remove($"prof-pic-{member.ProfilePicturePath}");
                    member.ProfilePicturePath = null;
                    member.ProfilePictureFileName = null;
                }

                // Remove study enrollments that are still in progress
                var activeStudyEnrollments = await db.StudyEnrollments
                    .Where(se => se.MemberId == id && se.Status == StudyStatus.Enrolled)
                    .ToListAsync(cancellationToken);
                db.StudyEnrollments.RemoveRange(activeStudyEnrollments);

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
            bool hasUnauthorizedOperations = patchDoc.Operations.Any(op =>
                !Member.AllowedFields.Contains(op.path) ||
                (!string.IsNullOrEmpty(op.from) && !Member.AllowedFields.Contains(op.from))
            );
            if (member.Id != userId || hasUnauthorizedOperations)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            try
            {
                patchDoc.ApplyTo(member);
                StateValidator.Validate(member);

                authOutboxWorker.EnqueueTask(AuthTaskType.Sync, member.AuthSystemUserId ?? throw new InvalidOperationException("Member does not have a authentication system ID."), db);

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
            if (member.Id != userId
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

                authOutboxWorker.EnqueueTask(AuthTaskType.Sync, member.AuthSystemUserId ?? throw new InvalidOperationException("Member does not have a authentication system ID."), db);

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
            if (id != userId)
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
                authOutboxWorker.EnqueueTask(AuthTaskType.RefreshEmail, member.AuthSystemUserId ?? throw new InvalidOperationException("Member does not have a authentication system ID."), db);
                var newMail = await authService.GetEmail(member.AuthSystemUserId ?? throw new InvalidOperationException("Member does not have a authentication system ID."));
                mailSubscriptionOutboxWorker.EnqueueMigrateEmailTask(member.Email, newMail, db);
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

        /// <inheritdoc />
        public async Task<IEnumerable<MemberMailinglistDto>> GetMemberMailinglists(Guid id, bool includeYearlyRenewal, Guid userId, CancellationToken cancellationToken)
        {
            if (id != userId)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            var member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null)
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            var visibleIds = await mailinglistCurationService.GetVisibleProviderListIds(includeYearlyRenewal, cancellationToken);
            var allLists = await mailSubscriptionService.GetMemberMailinglistsAsync(member.Email, cancellationToken);

            return allLists.Where(l => visibleIds.Contains(l.Id));
        }

        /// <inheritdoc />
        public async Task UpdateMemberMailinglists(Guid id, List<string> subscribedListIds, bool includeYearlyRenewal, Guid userId, CancellationToken cancellationToken)
        {
            if (id != userId)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            var member = await db.Members.FindAsync(new object[] { id }, cancellationToken);
            if (member == null)
                throw new KeyNotFoundException($"Member with ID {id} not found.");

            logger.LogInformation("Updating mailing list subscriptions for member {MemberId}. Requested by {UserId}.", id, userId);

            // subscribedListIds only covers the lists visible in this context (e.g. General only,
            // when saving from the everyday account settings page). Preserve whatever the member is
            // already subscribed to outside that context - including YearlyRenewalOnly lists, and
            // any provider list Tavern doesn't curate at all - so an edit in one context can never
            // silently unsubscribe a member from something outside it.
            var visibleIds = await mailinglistCurationService.GetVisibleProviderListIds(includeYearlyRenewal, cancellationToken);
            var currentState = await mailSubscriptionService.GetMemberMailinglistsAsync(member.Email, cancellationToken);
            var preserved = currentState.Where(l => l.Subscribed && !visibleIds.Contains(l.Id)).Select(l => l.Id);
            var finalSet = subscribedListIds.Union(preserved);

            mailSubscriptionOutboxWorker.EnqueueUpdateSubscriptionsTask(member.Email, finalSet, db);
        }

        private async Task RemoveExistingMemberWithSameEmail(string email, CancellationToken ct)
        {
            var existingMember = await db.Members
                .Include(m => m.StudyEnrollments)
                .ThenInclude(se => se.Study)
                .Include(m => m.Enrollments)
                .FirstOrDefaultAsync(m => m.Email == email, ct);
            if (existingMember == null)
                return;

            if (existingMember.Begunstiger)
                throw new InvalidOperationException("Existing member with same email address found.");

            if ((await db.Settings.FindAsync("MastersShouldPayMembership"))?.Value != "1" && existingMember.StudyEnrollments.Any(se => se.Study.Type == StudyType.Master))
                throw new InvalidOperationException("Existing member with same email address found.");

            if ((await db.Settings.FindAsync("GratieShouldPayMembership"))?.Value != "1" && existingMember.Gratie)
                throw new InvalidOperationException("Existing member with same email address found.");

            if ((await db.Settings.FindAsync("ErelidShouldPayMembership"))?.Value != "1" && existingMember.EreLid)
                throw new InvalidOperationException("Existing member with same email address found.");

            if ((await db.Settings.FindAsync("LidVanVerdiensteShouldPayMembership"))?.Value != "1" && existingMember.LidVanVerdienste)
                throw new InvalidOperationException("Existing member with same email address found.");

            // if ever enrolled for an activity, we don't want to delete the member, (can be an old begunstiger that isn't begunstiger anymore, we want to keep the history of their activity enrollments)
            if (existingMember.Enrollments.Any())
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
            authOutboxWorker.EnqueueTask(
                AuthTaskType.Delete,
                existingMember.AuthSystemUserId ?? throw new Exception("Member isn't synced with the authentication system yet."),
                db
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

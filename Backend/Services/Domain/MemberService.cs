using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
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
        ILogger<MemberService> logger,
        IEnumerable<IMailSyncOutboxWorker>? mailSyncWorkers = null
    ) : IMemberService
    {
        private readonly IEnumerable<IMailSyncOutboxWorker> _mailSyncWorkers = mailSyncWorkers ?? [];
        /// <inheritdoc />
        public async Task<List<MemberResponseDTO>> GetMembers(GetMembersDto dto, Guid userId, CancellationToken cancellationToken)
        {
            permissionService.EnsurePermission(userId, Permission.ViewMembers);

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

            var mapper = MemberResponseDTO.ToDto(userId, true, permissionService.IsBoardOrCandidateBoardMember(userId)).Compile();

            return members.Select(m => mapper(m)).ToList();
        }

        /// <inheritdoc />
        public async Task<MemberResponseDTO?> GetMember(Guid userIdFromUserToGet, Guid userId, CancellationToken cancellationToken)
        {
            if (userId != userIdFromUserToGet)
                permissionService.EnsurePermission(userId, Permission.ViewMembers);

            return await db.Members
                .Where(m => m.Id == userIdFromUserToGet)
                .Include(m => m.StudyEnrollments)
                    .ThenInclude(se => se.Study)
                .Include(m => m.GroupMemberships)
                    .ThenInclude(gm => gm.Group)
                .Include(m => m.GroupMemberships)
                    .ThenInclude(gm => gm.RoleAlias)
                .Select(MemberResponseDTO.ToDto(userId, permissionService.HasPermissionOrBoard(userId, Permission.ViewMembers), permissionService.IsBoardOrCandidateBoardMember(userId)))
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
                permissionService.EnsurePermission(userId.Value, Permission.ManageMembers);
            }
            else
            {
                if ((dto.StudyEnrollments == null || dto.StudyEnrollments.Count == 0) && (userId == null || !permissionService.HasPermissionOrBoard(userId.Value, Permission.ManageMembers)))
                    throw new ArgumentException("Member must be enrolled to atleast one study.");

                if (dto.StudentNumber.Trim() == "" || !int.TryParse(dto.StudentNumber, out var _))
                    throw new ArgumentException("Student number must be a number.");

                var studyStartDatesSetting = (await db.Settings.FindAsync(new object[] { "StudyStartDates" }, cancellationToken))?.Value ?? "09-01,02-01";
                Validators.StudyEnrollmentValidator.ValidateEnrollmentDates(dto.StudyEnrollments ?? new List<PostStudyEnrollmentDTO>(), studyStartDatesSetting);
            }

            // Check if date of birth is in the past
            if (dto.DateOfBirth >= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException("Date of birth must be in the past.");
            }

            // Check if member is a minor and if so, require parent phone number
            string timezoneId = Environment.GetEnvironmentVariable("AssociationTimeZone") ?? "Europe/Amsterdam";
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            DateTime todayInTimeZone = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).Date;

            if (dto.DateOfBirth.AddYears(18).Date > todayInTimeZone &&
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
                permissionService.EnsurePermission(userId, Permission.ManageMembers);

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
                foreach (var worker in _mailSyncWorkers)
                {
                    if (!ReferenceEquals(worker, mailSubscriptionOutboxWorker))
                    {
                        worker.EnqueueDeleteMail(oldEmail, db);
                    }
                }
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

                // Remove study enrollments that are still in progress
                var activeStudyEnrollments = await db.StudyEnrollments
                    .Where(se => se.MemberId == id && se.Status == StudyStatus.Enrolled)
                    .ToListAsync(cancellationToken);
                db.StudyEnrollments.RemoveRange(activeStudyEnrollments);

                // Check if not enrolled for any future activities
                var now = DateTimeOffset.UtcNow;
                var futureEnrollments = (await db.Enrollments
                        .Where(e => e.MemberId == id)
                        .Include(e => e.Activity)
                        .ToListAsync(cancellationToken))
                    .Where(e => e.Activity.DateTimeEnd > now)
                    .ToList();
                if (futureEnrollments.Any(fe => !fe.IsOnWaitingList))
                    throw new InvalidOperationException("Member has future enrollments and cannot be deleted.");

                db.Enrollments.RemoveRange(futureEnrollments);

                if (!string.IsNullOrEmpty(member.ProfilePicturePath))
                {
                    await storageService.DeleteFileAsync("profile-pictures", member.ProfilePicturePath);
                    memoryCache.Remove($"prof-pic-{member.ProfilePicturePath}");
                    member.ProfilePicturePath = null;
                    member.ProfilePictureFileName = null;
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

            // Some settings a member should not be able to edit themselves, and if they try to edit those, we check if they have the ManageMembers permission
            bool hasUnauthorizedOperations = patchDoc.Operations.Any(op =>
                !Member.AllowedFields.Contains(op.path) ||
                (!string.IsNullOrEmpty(op.from) && !Member.AllowedFields.Contains(op.from))
            );
            if (member.Id != userId || hasUnauthorizedOperations)
                permissionService.EnsurePermission(userId, Permission.ManageMembers);

            // Notes stays board-only regardless of ManageMembers, since it isn't covered by that permission
            bool touchesNotes = patchDoc.Operations.Any(op =>
                op.path.Equals("/notes", StringComparison.OrdinalIgnoreCase) ||
                (op.from?.Equals("/notes", StringComparison.OrdinalIgnoreCase) ?? false));
            if (touchesNotes)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            try
            {
                patchDoc.ApplyTo(member);
                StateValidator.Validate(member);

                authOutboxWorker.EnqueueTask(AuthTaskType.Sync, member.Id, db);

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

            if (member.Id != userId)
            {
                // Only ManageMembers holders (or board) may edit someone else's profile at all
                permissionService.EnsurePermission(userId, Permission.ManageMembers);
            }
            else if (!permissionService.HasPermissionOrBoard(userId, Permission.ManageMembers))
            {
                // Some settings a member should not be able to edit themselves. Silently keep them as
                // they are instead of rejecting the request when they differ - comparing and rejecting
                // would let someone guess a hidden value (e.g. the admin-only Notes field) and learn
                // whether they guessed right from whether the request succeeds or fails.
                PreserveFieldsOutsideAllowedFields(member, dto);
            }

            // Notes stays board-only regardless of ManageMembers, since it isn't covered by that permission
            if (!permissionService.IsBoardOrCandidateBoardMember(userId))
                dto.Notes = member.Notes;

            try
            {
                ApplyMemberUpdate(member, dto);
                StateValidator.Validate(member);

                authOutboxWorker.EnqueueTask(AuthTaskType.Sync, member.Id, db);

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
                permissionService.EnsurePermission(userId, Permission.ManageMembers);

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
                authOutboxWorker.EnqueueTask(AuthTaskType.RefreshEmail, member.Id, db);
                var newMail = await authService.GetEmail(id);
                mailSubscriptionOutboxWorker.EnqueueMigrateEmailTask(member.Email, newMail, db);
                foreach (var worker in _mailSyncWorkers)
                {
                    if (!ReferenceEquals(worker, mailSubscriptionOutboxWorker))
                    {
                        worker.EnqueueSyncMail(member.Email, newMail, db);
                    }
                }
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
                permissionService.EnsurePermission(userId, Permission.ManageMembers);

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
                permissionService.EnsurePermission(userId, Permission.ManageMembers);

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

        /// <inheritdoc />
        public async Task<ActivationEmailStatus> SendActivationEmail(Guid id, CancellationToken cancellationToken)
        {
            var member = await db.Members.FindAsync([id], cancellationToken)
                ?? throw new KeyNotFoundException($"Member with ID {id} not found.");

            if (member.ActivationEmailSentAt != null)
            {
                return ActivationEmailStatus.AlreadySent;
            }

            if (!paymentValidationService.HasEverPaidMembershipPayment(member.Id))
            {
                var unpaidMembershipPayments = await db.MembershipPayments
                    .Where(p => p.MemberId == member.Id && p.PaidAt == null)
                    .ToListAsync(cancellationToken);

                var liveStatuses = new List<PaymentStatus>();
                foreach (var payment in unpaidMembershipPayments)
                {
                    liveStatuses.Add((await paymentService.GetPaymentAsync(payment.PaymentServiceId)).Status);
                }

                if (liveStatuses.Count > 0 && !liveStatuses.Contains(PaymentStatus.Paid))
                {
                    return liveStatuses.Contains(PaymentStatus.Pending)
                        ? ActivationEmailStatus.Pending
                        : ActivationEmailStatus.PaymentRequired;
                }
            }

            if (member.AuthSystemUserId == null)
            {
                // Not linked to the auth system yet (their AuthOutboxTask.Create task is still pending).
                // The caller (the confirm-mail page) retries shortly.
                return ActivationEmailStatus.Pending;
            }

            var queued = await paymentService.TryQueueActivationEmailAsync(member.Id);
            await db.SaveChangesAsync(cancellationToken);

            if (!queued)
            {
                return ActivationEmailStatus.AlreadySent;
            }

            logger.LogInformation("Queued activation email for member {MemberId}.", id);
            return ActivationEmailStatus.Sent;
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

            if (paymentValidationService.HasEverPaidMembershipPayment(existingMember.Id) || paymentValidationService.HasEverPaidBegunstigerFee(existingMember.Id))
                throw new InvalidOperationException("Existing member with same email address found.");

            var existingMembershipPayments = await db.MembershipPayments
                .Where(p => p.MemberId == existingMember.Id)
                .ToListAsync(ct);

            var existingBegunstigerPayments = await db.BegunstigerPayments
                .Where(p => p.MemberId == existingMember.Id)
                .ToListAsync(ct);

            foreach (var existingPayment in existingMembershipPayments.Cast<Payment>().Concat(existingBegunstigerPayments))
            {
                var paymentResponse = await paymentService.GetPaymentAsync(existingPayment.PaymentServiceId);

                // Make sure there is no paid membership/begunstiger payment with the same email, if there is, we don't want to delete the member
                if (paymentResponse.Status == PaymentStatus.Paid)
                {
                    throw new InvalidOperationException("Existing member with same email address found.");
                }

                // If there is a pending payment, we cancel it to prevent the member from paying for a membership/fee they won't get
                if (paymentResponse.Status == PaymentStatus.Pending)
                {
                    await paymentService.CancelPaymentAsync(existingPayment.PaymentServiceId);
                }
            }

            db.MembershipPayments.RemoveRange(existingMembershipPayments);
            db.BegunstigerPayments.RemoveRange(existingBegunstigerPayments);

            db.Members.Remove(existingMember);
            if (db.AuthOutboxTasks.Any(t => t.AuthSystemUserId == existingMember.Id && t.TaskType == AuthTaskType.Create))
            {
                db.AuthOutboxTasks.RemoveRange(db.AuthOutboxTasks.Where(t => t.AuthSystemUserId == existingMember.AuthSystemUserId));
                db.AuthOutboxTasks.RemoveRange(db.AuthOutboxTasks.Where(t => t.AuthSystemUserId == existingMember.Id && t.TaskType == AuthTaskType.Create));
            }
            else
            {
                authOutboxWorker.EnqueueTask(
                    AuthTaskType.Delete,
                    existingMember.AuthSystemUserId ?? throw new Exception("Member isn't synced with the authentication system yet."),
                    db
                );
            }
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

        /// <summary>
        /// Since PUT replaces the whole member rather than applying discrete patch operations, field-level
        /// restrictions can't be enforced by inspecting operation paths like PatchMember does. Rejecting the
        /// request whenever one of these fields differs from its current value would let a member guess a
        /// hidden value (e.g. the admin-only Notes field) and learn whether the guess was correct from
        /// whether the request succeeds - so instead, these fields are silently reset to their current
        /// value on the DTO before it's applied, regardless of what was submitted for them.
        /// </summary>
        private static void PreserveFieldsOutsideAllowedFields(Member member, MemberUpdateDTO dto)
        {
            dto.Email = member.Email;
            dto.StudentNumber = member.StudentNumber;
            dto.FirstName = member.FirstName;
            dto.LastName = member.LastName;
            dto.DateOfBirth = member.DateOfBirth;
            dto.Notes = member.Notes;
            dto.Gratie = member.Gratie;
            dto.LidVanVerdienste = member.LidVanVerdienste;
            dto.EreLid = member.EreLid;
            dto.Begunstiger = member.Begunstiger;
            dto.Suspended = member.Suspended;
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
            member.Email = dto.Email;
            member.Notes = dto.Notes;
            member.Gratie = dto.Gratie;
            member.LidVanVerdienste = dto.LidVanVerdienste;
            member.EreLid = dto.EreLid;
            member.Begunstiger = dto.Begunstiger;
            member.Suspended = dto.Suspended;
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

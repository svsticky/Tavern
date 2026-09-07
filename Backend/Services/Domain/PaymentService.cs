using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.PaymentServices;
using Backend.Utils;
using Backend.Validators;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services.Domain
{
    /// <summary>
    /// Implements payment creation, retrieval, export, and status operations.
    /// </summary>
    public class PaymentService(
        PostgresDbContext db,
        IPermissionService permissionService,
        IPaymentValidationService paymentValidationService,
        AbstractPaymentService paymentService,
        AuthOutboxWorker authOutboxWorker,
        ILogger<PaymentService> logger
    ) : IPaymentService
    {
        private readonly string _frontendUrl = Environment.GetEnvironmentVariable("HostUrl")!;
        private readonly string _backendUrl = Environment.GetEnvironmentVariable("ApiUrl")!;
        private readonly string? _ngrokUrl = Environment.GetEnvironmentVariable("NGROK_URL");

        /// <inheritdoc />
        public async Task<List<MembershipPayment>> GetMembershipPayments(Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ViewFinances);

            return await db.MembershipPayments
                .Include(p => p.Member)
                .ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<MembershipPayment?> GetMembershipPayment(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ViewFinances);

            return await db.MembershipPayments.FindAsync(id, ct);
        }

        /// <inheritdoc />
        public async Task<List<EnrollmentPayment>> GetEnrollmentPayments(Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ViewFinances);

            return await db.EnrollmentPayments
                .Include(p => p.Member)
                .Include(p => p.Activity)
                .ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<EnrollmentPayment?> GetEnrollmentPayment(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ViewFinances);

            return await db.EnrollmentPayments.FindAsync(id, ct);
        }

        /// <inheritdoc />
        public async Task<PostPaymentResponse> CreateMembershipPayment(PostMembershipPaymentDTO dto, Guid? userId)
        {
            logger.LogInformation("Creating membership payment for member {MemberId}. Manual: {Manual}", dto.MemberId, dto.ManuallyMarkedAsPaid);

            if (dto.ManuallyMarkedAsPaid)
            {
                permissionService.EnsurePermission(userId ?? throw new UnauthorizedAccessException("Authentication required."), Permission.ManageFinances);
            }

            using var transaction = await db.Database.BeginTransactionAsync();

            var member = await GetMemberOrThrow(dto.MemberId);

            try
            {
                EnsureMemberCanPayMembership(member);
                EnsureMemberHasNoPaidMembership(dto.MemberId);

                // return existing payment
                await HandleExistingMembershipPayment(member, dto.MemberId);

                if (dto.ManuallyMarkedAsPaid)
                {
                    // If payment is manually marked as paid, we can skip creating a payment service fee payment and create it directly in the database
                    var manualPayment = await BuildMembershipPayment(dto.MemberId, null, manuallyMarkedAsPaid: true);
                    StateValidator.Validate(manualPayment);

                    db.MembershipPayments.Add(manualPayment);
                    EnqueueAuthSyncForPaidMembership(member);

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.LogInformation("Created manual membership payment for member {MemberId}.", dto.MemberId);

                    return new PostPaymentResponse();
                }

                // create new payment
                var paymentResponse = await BuildMembershipPaymentRequest(member, dto.MemberId);

                var payment = await BuildMembershipPayment(dto.MemberId, paymentResponse);
                StateValidator.Validate(payment);

                db.MembershipPayments.Add(payment);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                logger.LogInformation("Created membership payment {PaymentId} for member {MemberId}.", payment.Id, dto.MemberId);

                return ToCheckoutResponse(paymentResponse.PaymentUrl);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<PostPaymentResponse> CreateBegunstigerPayment(PostBegunstigerPaymentDTO dto, Guid? userId)
        {
            logger.LogInformation("Creating begunstiger payment for member {MemberId}. Manual: {Manual}", dto.MemberId, dto.ManuallyMarkedAsPaid);

            var member = await GetMemberOrThrow(dto.MemberId);

            if (userId != dto.MemberId)
            {
                permissionService.EnsurePermission(userId ?? throw new UnauthorizedAccessException("Authentication required."), Permission.ManageFinances);
            }

            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                EnsureMemberIsBegunstiger(member);
                EnsureMemberHasNoPaidBegunstigerFee(dto.MemberId);

                // return existing payment
                await HandleExistingBegunstigerPayment(member, dto.MemberId);

                if (dto.ManuallyMarkedAsPaid)
                {
                    // If payment is manually marked as paid, we can skip creating a Mollie payment and create it directly in the database
                    var manualPayment = await BuildBegunstigerPayment(dto.MemberId, null, manuallyMarkedAsPaid: true);
                    StateValidator.Validate(manualPayment);

                    db.BegunstigerPayments.Add(manualPayment);
                    EnqueueAuthSyncForPaidMembership(member);

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.LogInformation("Created manual begunstiger payment for member {MemberId}.", dto.MemberId);

                    return new PostPaymentResponse();
                }

                // create new payment
                var paymentResponse = await BuildBegunstigerPaymentRequest(member, dto.MemberId);

                var payment = await BuildBegunstigerPayment(dto.MemberId, paymentResponse);
                StateValidator.Validate(payment);

                db.BegunstigerPayments.Add(payment);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                logger.LogInformation("Created begunstiger payment {PaymentId} for member {MemberId}.", payment.Id, dto.MemberId);

                return ToCheckoutResponse(paymentResponse.PaymentUrl);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<(byte[] Content, string FileName)> ExportPaymentsToCsv(DateTime startDate, DateTime endDate, Guid userId, CancellationToken ct)
        {
            permissionService.EnsurePermission(userId, Permission.ManageFinances);

            string timezoneId = Environment.GetEnvironmentVariable("AssociationTimeZone") ?? "Europe/Amsterdam";

            var startDateInTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(startDate, timezoneId).Date.ToUniversalTime();
            var endDateInTimeZone = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(endDate, timezoneId).Date.ToUniversalTime();

            var enrollmentPayments = await db.EnrollmentPayments
                .Include(p => p.Activity)
                    .ThenInclude(a => a!.Organizer)
                .Where(p => p.PaidAt >= startDate && p.PaidAt <= endDate && !p.ManuallyMarkedAsPaid)
                .ToListAsync(ct);

            var membershipPayments = await db.MembershipPayments
                .Where(p => p.PaidAt >= startDate && p.PaidAt <= endDate && !p.ManuallyMarkedAsPaid)
                .ToListAsync(ct);

            var paymentServiceFeePayments = await db.PaymentServiceFeePayments
                .Where(p => p.PaidAt >= startDate && p.PaidAt <= endDate && !p.ManuallyMarkedAsPaid)
                .ToListAsync(ct);

            var begunstigerPayments = await db.BegunstigerPayments
                .Where(p => p.PaidAt >= startDate && p.PaidAt <= endDate && !p.ManuallyMarkedAsPaid)
                .ToListAsync(ct);

            var csv = BuildExportCsv(startDateInTimeZone, endDateInTimeZone, enrollmentPayments, membershipPayments, paymentServiceFeePayments, begunstigerPayments);
            logger.LogInformation("Exported payments CSV for period {StartDateInTimeZone} - {EndDateInTimeZone}. Enrollment: {EnrollmentCount}, Membership: {MembershipCount}, PaymentServiceFee: {PaymentServiceFeeCount}, Begunstiger: {BegunstigerCount}",
                startDateInTimeZone, endDateInTimeZone, enrollmentPayments.Count, membershipPayments.Count, paymentServiceFeePayments.Count, begunstigerPayments.Count);

            var fileName = $"payments_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
            return (Encoding.UTF8.GetBytes(csv.ToString()), fileName);
        }

        /// <inheritdoc />
        public async Task<PostPaymentResponse> CreateActivityPayment(PostActivityPaymentDTO dto, Guid userId)
        {
            logger.LogInformation("Creating activity payment for member {MemberId} and {ActivityCount} activities. Manual: {Manual}",
                dto.MemberId, dto.ActivityIds.Count, dto.ManuallyMarkedAsPaid);
            if (userId != dto.MemberId)
            {
                permissionService.EnsurePermission(userId, Permission.ManageFinances);
            }

            var member = await GetMemberOrThrow(dto.MemberId);

            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                await HandleExistingActivityPayment(dto.MemberId, dto.ActivityIds);

                var enrollments = await db.Enrollments
                    .Include(e => e.Activity)
                    .Where(e => dto.ActivityIds.Contains(e.ActivityId) && e.MemberId == dto.MemberId)
                    .ToListAsync();

                if (enrollments.Count != dto.ActivityIds.Count)
                    throw new Exception("One or more enrollments not found");

                if (dto.ManuallyMarkedAsPaid)
                {
                    // If payment is manually marked as paid, we can skip creating a payment service fee payment and create it directly in the database
                    permissionService.EnsurePermission(userId, Permission.ManageFinances);
                    CreateEnrollmentPayments(dto.MemberId, enrollments, true);

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.LogInformation("Created manual activity payment records for member {MemberId}.", dto.MemberId);
                    return new PostPaymentResponse();
                }
                else
                {
                    // Recomputed after HandleExistingActivityPayment, which may have self-healed some of the
                    // requested activities to paid - using a value computed beforehand would overcharge the
                    // member for activities that are already settled.
                    var totalPrice = enrollments.Sum(e =>
                        paymentValidationService.GetUnpaidAmountForEnrollment(e)
                    );

                    if (totalPrice <= 0)
                    {
                        await transaction.CommitAsync();
                        logger.LogInformation("All requested activities for member {MemberId} are already paid.", dto.MemberId);
                        return new PostPaymentResponse();
                    }

                    // Create payment service fee payment
                    CreatePaymentResponse paymentResponse = await BuildPaymentServiceFeePaymentRequest(totalPrice, member, dto);

                    // Create the payment for the payment service fee
                    PaymentServiceFeePayment paymentServiceFeePayment = new PaymentServiceFeePayment
                    {
                        MemberId = dto.MemberId,
                        Price = GetPaymentServiceFee(),
                        PaymentServiceId = dto.ManuallyMarkedAsPaid ? "" : paymentResponse.PaymentId,
                        PaymentIntentUrl = dto.ManuallyMarkedAsPaid ? "" : paymentResponse.PaymentUrl,
                        PaidAt = dto.ManuallyMarkedAsPaid ? DateTime.UtcNow : (DateTime?)null
                    };

                    db.PaymentServiceFeePayments.Add(paymentServiceFeePayment);

                    // Create the enrollment payment with paymentResponse information
                    CreateEnrollmentPayments(dto.MemberId, enrollments, false, paymentResponse);
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.LogInformation("Created payment service fee-backed activity payment for member {MemberId}. PaymentId: {PaymentId}",
                        dto.MemberId, paymentResponse.PaymentId);

                    return new PostPaymentResponse { CheckoutUrl = paymentResponse.PaymentUrl };
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Failed creating activity payment for member {MemberId}.", dto.MemberId);
                throw;
            }
        }

        /// <inheritdoc />
        public IEnumerable<EnrollmentBalance> GetUnpaid(Guid userId, bool allUsers = false)
        {
            if (allUsers)
            {
                permissionService.EnsurePermission(userId, Permission.ViewFinances);
            }

            if (allUsers)
            {
                return paymentValidationService.GetAllUnpaidEnrollments();
            }
            else
            {
                return paymentValidationService.GetUnpaidEnrollmentsForMember(userId);
            }
        }

        /// <inheritdoc />
        public IEnumerable<EnrollmentBalance> GetOverpaid(Guid userId)
        {
            permissionService.EnsurePermission(userId, Permission.ViewFinances);

            return paymentValidationService.GetAllOverpaidEnrollments();
        }

        /// <inheritdoc />
        public async Task<PaymentStatusResponse> GetMemberPaymentStatus(Guid fromUserId, Guid userId, CancellationToken ct)
        {
            if (fromUserId != userId)
            {
                permissionService.EnsurePermission(userId, Permission.ViewFinances);
            }

            var member = await db.Members
                .Include(m => m.StudyEnrollments)
                .ThenInclude(se => se.Study)
                .Include(m => m.Enrollments)
                .FirstOrDefaultAsync(m => m.Id == fromUserId, ct);

            if (member == null) throw new KeyNotFoundException("Member not found");

            var unpaid = paymentValidationService.GetUnpaidEnrollmentsForMember(member.Id);
            var hasDoneOrDoingStudy = paymentValidationService.HasDoneOrDoingStudy(member.Id);

            // Begunstigers pay their own separate fee, due again every board term, instead of the
            // regular membership fee - so "is currently paid" and "has ever paid" are answered by
            // the begunstiger-specific checks for them rather than the membership ones.
            var hasPaidMembershipBeforeExpirationTime = member.Begunstiger
                ? paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id)
                : paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);
            var hasEverPaidMembership = member.Begunstiger
                ? paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id)
                : paymentValidationService.HasEverPaidMembershipPayment(member.Id);

            return new PaymentStatusResponse
            {
                MemberId = member.Id,
                HasEverPaidMembership = hasEverPaidMembership,
                HasPaidMembershipBeforeExpirationTime = hasPaidMembershipBeforeExpirationTime,
                HasPaidAllActivities = !unpaid.Any(),
                IsBegunstiger = member.Begunstiger,
                CanPayMembership = member.Begunstiger || hasDoneOrDoingStudy,
                UnpaidEnrollments = unpaid
            };
        }

        private async Task<Member> GetMemberOrThrow(Guid memberId)
        {
            var member = await db.Members.FindAsync(memberId);
            return member ?? throw new KeyNotFoundException("Member not found");
        }

        /// <summary>
        /// Enforces server-side that regular membership payments can only ever be created for members
        /// who have (had) a study enrollment and are not a begunstiger - begunstigers must pay through
        /// CreateBegunstigerPayment instead, so they can't use this (cheaper) endpoint to underpay.
        /// </summary>
        private void EnsureMemberCanPayMembership(Member member)
        {
            if (member.Begunstiger || !paymentValidationService.HasDoneOrDoingStudy(member.Id))
            {
                throw new InvalidOperationException("Member is not eligible to pay membership");
            }
        }

        /// <summary>
        /// Enforces server-side that begunstiger fee payments can only ever be created for members who
        /// are currently flagged as a begunstiger - regardless of whether it's the member paying
        /// themselves or a board member creating/marking it on their behalf.
        /// </summary>
        private void EnsureMemberIsBegunstiger(Member member)
        {
            if (!member.Begunstiger)
            {
                throw new InvalidOperationException("Member is not a begunstiger");
            }
        }

        private void EnsureMemberHasNoPaidMembership(Guid memberId)
        {
            if (paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(memberId))
            {
                throw new InvalidOperationException("Member already paid membership");
            }
        }

        private async Task HandleExistingMembershipPayment(Member member, Guid memberId)
        {
            var existingPayments = await db.MembershipPayments.Where(p => p.MemberId == memberId && p.PaidAt == null).ToListAsync();

            foreach (var existingPayment in existingPayments)
            {
                var paymentResponse = await paymentService.GetPaymentAsync(existingPayment.PaymentServiceId);
                if (paymentResponse.Status == PaymentStatus.Pending)
                {
                    await paymentService.CancelPaymentAsync(existingPayment.PaymentServiceId);
                    db.MembershipPayments.Remove(existingPayment);
                    await db.SaveChangesAsync();
                }
                else if (paymentResponse.Status == PaymentStatus.Paid)
                {
                    existingPayment.PaidAt = paymentResponse.PaidAt ?? DateTimeOffset.UtcNow;

                    EnqueueAuthSyncForPaidMembership(member);

                    await db.SaveChangesAsync();
                    EnsureMemberHasNoPaidMembership(memberId);
                }
            }
        }

        private void EnsureMemberHasNoPaidBegunstigerFee(Guid memberId)
        {
            if (paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(memberId))
            {
                throw new InvalidOperationException("Member already paid their begunstiger fee since the last board change");
            }
        }

        private async Task HandleExistingBegunstigerPayment(Member member, Guid memberId)
        {
            var existingPayments = await db.BegunstigerPayments.Where(p => p.MemberId == memberId && p.PaidAt == null).ToListAsync();

            foreach (var existingPayment in existingPayments)
            {
                var paymentResponse = await paymentService.GetPaymentAsync(existingPayment.PaymentServiceId);
                if (paymentResponse.Status == PaymentStatus.Pending)
                {
                    await paymentService.CancelPaymentAsync(existingPayment.PaymentServiceId);
                    db.BegunstigerPayments.Remove(existingPayment);
                    await db.SaveChangesAsync();
                }
                else if (paymentResponse.Status == PaymentStatus.Paid)
                {
                    existingPayment.PaidAt = paymentResponse.PaidAt ?? DateTimeOffset.UtcNow;

                    EnqueueAuthSyncForPaidMembership(member);

                    await db.SaveChangesAsync();
                    EnsureMemberHasNoPaidBegunstigerFee(memberId);
                }
            }
        }

        private async Task<CreatePaymentResponse> BuildBegunstigerPaymentRequest(Member member, Guid memberId)
        {
            return await paymentService.CreatePaymentAsync(
                decimal.Parse(db.Settings.Find("BegunstigerPrice")?.Value ?? "10.00"),
                $"Begunstiger payment for {member.FirstName} {member.LastName}",
                _frontendUrl,
                string.IsNullOrEmpty(_ngrokUrl) ?
                    (_backendUrl.ToLower().Contains("localhost") ? null : _backendUrl + "/payments/webhook")
                    : $"{_ngrokUrl}/payments/webhook",
                $"begunstiger_{memberId}"
            );
        }

        private async Task<BegunstigerPayment> BuildBegunstigerPayment(Guid memberId, CreatePaymentResponse? paymentResponse, bool manuallyMarkedAsPaid = false)
        {
            if (paymentResponse == null && !manuallyMarkedAsPaid)
            {
                throw new ArgumentException("Payment response must be provided if payment is not manually marked as paid");
            }

            return new BegunstigerPayment
            {
                MemberId = memberId,
                Price = decimal.TryParse((await db.Settings.FindAsync("BegunstigerPrice"))?.Value ?? "10.00", out var price) ? price : 10.00m,
                PaymentServiceId = manuallyMarkedAsPaid ? "" : paymentResponse!.PaymentId,
                PaymentIntentUrl = manuallyMarkedAsPaid ? "" : paymentResponse!.PaymentUrl,
                PaidAt = manuallyMarkedAsPaid ? DateTime.UtcNow : (DateTime?)null,
                ManuallyMarkedAsPaid = manuallyMarkedAsPaid
            };
        }

        /// <summary>
        /// Checks for a still-pending activity payment covering the requested activities and reuses its checkout
        /// URL instead of creating a duplicate Mollie payment. Also self-heals payments that Mollie already
        /// confirmed as paid but whose webhook hasn't arrived yet, so the member isn't charged twice while
        /// waiting for the webhook.
        /// </summary>
        private async Task HandleExistingActivityPayment(Guid memberId, List<uint> activityIds)
        {
            var pendingPayments = await db.EnrollmentPayments
                .Where(p => p.MemberId == memberId && p.PaidAt == null && p.ActivityId != null && activityIds.Contains(p.ActivityId.Value))
                .ToListAsync();

            foreach (var payment in pendingPayments)
            {
                var paymentResponse = await paymentService.GetPaymentAsync(payment.PaymentServiceId);
                var paymentsInSameMollieUrl = pendingPayments.Where(p => p.PaymentServiceId == payment.PaymentServiceId).ToList();
                if (paymentResponse.Status == PaymentStatus.Pending)
                {
                    // try to cancel the payment, so a new one can be created
                    await paymentService.CancelPaymentAsync(payment.PaymentServiceId);
                    db.EnrollmentPayments.Remove(payment);
                }

                if (paymentResponse.Status == PaymentStatus.Paid)
                {
                    payment.PaidAt = paymentResponse.PaidAt ?? DateTimeOffset.UtcNow;
                    var feePayment = await db.PaymentServiceFeePayments
                        .FirstOrDefaultAsync(p => p.MemberId == memberId && p.PaymentServiceId == payment.PaymentServiceId && p.PaidAt == null);

                    if (feePayment != null)
                    {
                        feePayment.PaidAt = paymentResponse.PaidAt ?? DateTimeOffset.UtcNow;
                    }
                    await db.SaveChangesAsync();


                    var coveredActivityIds = paymentsInSameMollieUrl.Select(p => p.ActivityId!.Value);

                    foreach (var activityId in coveredActivityIds)
                    {
                        // check if paid enough that payment for this activity is covered by the existing payment
                        var enrollment = await db.Enrollments
                            .Include(e => e.Activity)
                            .FirstOrDefaultAsync(e => e.MemberId == memberId && e.ActivityId == activityId);

                        if (enrollment == null)
                        {
                            throw new KeyNotFoundException($"Enrollment for member {memberId} and activity {activityId} not found");
                        }

                        var unpaidAmount = paymentValidationService.GetUnpaidAmountForEnrollment(enrollment);
                        if (unpaidAmount <= 0)
                        {
                            activityIds.Remove(activityId);
                        }
                    }
                }
            }
        }

        private async Task<CreatePaymentResponse> BuildMembershipPaymentRequest(Member member, Guid memberId)
        {
            return await paymentService.CreatePaymentAsync(
                decimal.Parse(db.Settings.Find("MembershipPrice")?.Value ?? "7.50"),
                $"Membership payment for {member.FirstName} {member.LastName}",
                BuildMembershipPaymentRedirectUrl(member, memberId),
                string.IsNullOrEmpty(_ngrokUrl) ?
                    (_backendUrl.ToLower().Contains("localhost") ? null : _backendUrl + "/payments/webhook")
                    : $"{_ngrokUrl}/payments/webhook",
                $"membership_{memberId}"
            );
        }

        private string BuildMembershipPaymentRedirectUrl(Member member, Guid memberId)
        {
            // Members who haven't been sent their activation email yet are still mid-registration
            // (no password/verified email), so they need to land on confirm-mail to trigger it.
            // Already-activated members (e.g. paying to renew an expired membership) already have a
            // working account and should be sent straight back into the app instead.
            if (member.ActivationEmailSentAt == null)
            {
                return $"{_frontendUrl}/confirm-mail?memberId={memberId}";
            }

            return _frontendUrl;
        }

        private async Task<MembershipPayment> BuildMembershipPayment(Guid memberId, CreatePaymentResponse? paymentResponse, bool manuallyMarkedAsPaid = false)
        {
            if (paymentResponse == null && !manuallyMarkedAsPaid)
            {
                throw new ArgumentException("Payment response must be provided if payment is not manually marked as paid");
            }

            return new MembershipPayment
            {
                MemberId = memberId,
                Price = decimal.TryParse((await db.Settings.FindAsync("MembershipPrice"))?.Value ?? "7.50", out var price) ? price : 7.50m,
                PaymentServiceId = manuallyMarkedAsPaid ? "" : paymentResponse!.PaymentId,
                PaymentIntentUrl = manuallyMarkedAsPaid ? "" : paymentResponse!.PaymentUrl,
                PaidAt = manuallyMarkedAsPaid ? DateTime.UtcNow : (DateTime?)null,
                ManuallyMarkedAsPaid = manuallyMarkedAsPaid
            };
        }

        /// <summary>
        /// Queues an auth-system sync for a member whose membership payment was just marked paid. The member isn't
        /// linked to the auth system yet in some cases; don't let that block marking the payment as paid,
        /// AuthOutboxWorker queues a catch-up Sync task once they do get linked.
        /// </summary>
        private void EnqueueAuthSyncForPaidMembership(Member member)
        {
            authOutboxWorker.EnqueueTask(AuthTaskType.Sync, member.Id, db);
        }

        private static PostPaymentResponse ToCheckoutResponse(string checkoutUrl)
        {
            return new PostPaymentResponse { CheckoutUrl = checkoutUrl };
        }

        private async Task<CreatePaymentResponse> BuildPaymentServiceFeePaymentRequest(decimal totalPrice, Member member, PostActivityPaymentDTO dto)
        {
            return await paymentService.CreatePaymentAsync(
                totalPrice + GetPaymentServiceFee(),
                $"Activity payment for {member.FirstName} {member.LastName}",
                BuildActivityPaymentRedirectUrl(),
                string.IsNullOrEmpty(_ngrokUrl) ?
                    (_backendUrl.ToLower().Contains("localhost") ? null : _backendUrl + "/payments/webhook")
                    : $"{_ngrokUrl}/payments/webhook",
                $"activity_{dto.MemberId}_{string.Join("_", dto.ActivityIds)}"
            );
        }

        private string BuildActivityPaymentRedirectUrl()
        {
            // Lets the frontend know it's landing back from a Mollie checkout, so it can briefly
            // wait/poll for the payment webhook instead of trusting a single immediate status fetch.
            var separator = _frontendUrl.Contains('?') ? "&" : "?";
            return $"{_frontendUrl}{separator}paymentReturn=activity";
        }

        private decimal GetPaymentServiceFee()
        {
            return db.Settings.Where(s => s.Name == "PaymentServiceFee").Select(s => decimal.Parse(s.Value)).FirstOrDefault();
        }

        private void CreateEnrollmentPayments(Guid memberId, List<Enrollment> enrollments, bool manuallyMarkedAsPaid, CreatePaymentResponse? paymentResponse = null)
        {
            if (paymentResponse == null && !manuallyMarkedAsPaid)
            {
                throw new ArgumentException("Payment response must be provided if payment is not manually marked as paid");
            }

            foreach (var enrollment in enrollments)
            {
                if (!enrollment.Activity.IsOpenForPayment)
                    throw new Exception($"Activity {enrollment.Activity.Name} is not open for payment");

                var price = paymentValidationService.GetUnpaidAmountForEnrollment(enrollment);
                if (price <= 0) continue;

                var payment = new EnrollmentPayment
                {
                    MemberId = memberId,
                    ActivityId = enrollment.ActivityId,
                    Price = price,
                    PaymentServiceId = manuallyMarkedAsPaid ? "" : paymentResponse?.PaymentId ?? "",
                    PaymentIntentUrl = manuallyMarkedAsPaid ? "" : paymentResponse?.PaymentUrl ?? "",
                    PaidAt = manuallyMarkedAsPaid ? DateTime.UtcNow : (DateTime?)null
                };

                StateValidator.Validate(payment);

                db.EnrollmentPayments.Add(payment);
            }
        }


        /// <summary>
        /// Cost centers/units are optional; a whitespace-only value is treated the same as "not set".
        /// </summary>
        private static string BlankIfWhitespace(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value;

        private StringBuilder BuildExportCsv(
    DateTime startDateInNL,
    DateTime endDateInNL,
    List<EnrollmentPayment> enrollmentPayments,
    List<MembershipPayment> membershipPayments,
    List<PaymentServiceFeePayment> paymentServiceFeePayments,
    List<BegunstigerPayment> begunstigerPayments)
        {
            var csv = new StringBuilder();

            var invoiceDate = endDateInNL.AddDays(-1).ToString("dd-MM-yyyy");
            var periodLabel = $"ideal - {startDateInNL:dd-MM-yyyy} / {endDateInNL:dd-MM-yyyy}";
            var paymentsCondition = db.Settings.Where(s => s.Name == "PaymentServicePaymentsCondition").Select(s => s.Value).FirstOrDefault() ?? "2";
            var paymentServiceRelationalCode = db.Settings.Where(s => s.Name == "PaymentServiceRelationCode").Select(s => s.Value).FirstOrDefault() ?? "473";

            // Header line
            csv.AppendLine(CsvUtils.FormatLine("factuurdatum", invoiceDate, periodLabel, paymentsCondition, paymentServiceRelationalCode));

            var activityGLAccountFallback = db.Settings.Where(s => s.Name == "ActivityGLAccount").Select(s => s.Value).FirstOrDefault() ?? "7001";

            foreach (var p in enrollmentPayments)
            {
                var glAccount = p.Activity?.GLAccountId ?? p.Activity?.Organizer?.DefaultGLAccount ?? activityGLAccountFallback;
                var groupName = p.Activity?.Organizer?.Name ?? "Unknown Organizer";
                var activityName = p.Activity?.Name ?? "Unknown Activity";
                var costCenter = BlankIfWhitespace(p.Activity?.CostCenterId ?? p.Activity?.Organizer?.DefaultCostCenter);
                var costUnit = BlankIfWhitespace(p.Activity?.CostUnitId);
                var vatCode = p.Activity?.VatRate?.ToString() ?? "";
                var price = p.Price;

                var description = $"{groupName} | {activityName}";
                csv.AppendLine(CsvUtils.FormatLine("", glAccount, description, vatCode, price, costCenter, costUnit));
            }

            var membershipGLAccount = db.Settings.Where(s => s.Name == "MembershipGLAccount").Select(s => s.Value).FirstOrDefault() ?? "8000";
            var membershipVatCode = db.Settings.Where(s => s.Name == "MembershipVATCode").Select(s => s.Value).FirstOrDefault() ?? "0";
            var membershipCostCenter = BlankIfWhitespace(db.Settings.Where(s => s.Name == "MembershipCostCenter").Select(s => s.Value).FirstOrDefault());
            var membershipCostUnit = BlankIfWhitespace(db.Settings.Where(s => s.Name == "MembershipCostUnit").Select(s => s.Value).FirstOrDefault());

            foreach (var p in membershipPayments)
            {
                var description = "Lidmaatschap";
                var price = p.Price;

                csv.AppendLine(CsvUtils.FormatLine("", membershipGLAccount, description, membershipVatCode, price, membershipCostCenter, membershipCostUnit));
            }

            var begunstigerGLAccount = db.Settings.Where(s => s.Name == "BegunstigerGLAccount").Select(s => s.Value).FirstOrDefault() ?? "";
            var begunstigerVatCode = db.Settings.Where(s => s.Name == "BegunstigerVATCode").Select(s => s.Value).FirstOrDefault() ?? "0";
            var begunstigerCostCenter = BlankIfWhitespace(db.Settings.Where(s => s.Name == "BegunstigerCostCenter").Select(s => s.Value).FirstOrDefault());
            var begunstigerCostUnit = BlankIfWhitespace(db.Settings.Where(s => s.Name == "BegunstigerCostUnit").Select(s => s.Value).FirstOrDefault());

            foreach (var p in begunstigerPayments)
            {
                var description = "Begunstiger";
                var price = p.Price;

                csv.AppendLine(CsvUtils.FormatLine("", begunstigerGLAccount, description, begunstigerVatCode, price, begunstigerCostCenter, begunstigerCostUnit));
            }

            var groupedFees = paymentServiceFeePayments
                .GroupBy(p => p.Price)
                .Select(g => new
                {
                    UnitPrice = g.Key,
                    Count = g.Count(),
                    TotalPrice = g.Sum(p => p.Price)
                });

            var paymentServiceFeeGLAccount = db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeGLAccount")?.Value ?? "5007";
            var paymentServiceFeeCostCenter = BlankIfWhitespace(db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeCostCenter")?.Value);
            var paymentServiceFeeCostUnit = BlankIfWhitespace(db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeCostUnit")?.Value ?? "TRX");
            var feeVatCode = db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeVATCode")?.Value ?? "21";

            foreach (var group in groupedFees)
            {
                var description = $"Transaction costs {group.UnitPrice:N2} x {group.Count}";
                var totalPrice = group.TotalPrice;

                csv.AppendLine(CsvUtils.FormatLine("", paymentServiceFeeGLAccount, description, feeVatCode, totalPrice, paymentServiceFeeCostCenter, paymentServiceFeeCostUnit));
            }

            return csv;
        }
    }
}

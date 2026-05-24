using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Backend.Validators;
using Backend.Services.PaymentServices;

namespace Backend.Repositories
{
    /// <summary>
    /// Implements payment creation, retrieval, export, and status operations.
    /// </summary>
    public class PaymentRepository(
        PostgresDbContext db,
        IPermissionService permissionService,
        IPaymentValidationService paymentValidationService,
        AbstractPaymentService paymentService,
        AuthOutboxWorker authOutboxWorker,
        ILogger<PaymentRepository> logger
    ) : IPaymentRepository
    {
        private readonly string _frontendUrl = Environment.GetEnvironmentVariable("HostUrl")!;
        private readonly string _backendUrl = Environment.GetEnvironmentVariable("ApiUrl")!;
        private readonly string? _ngrokUrl = Environment.GetEnvironmentVariable("NGROK_URL");

        /// <inheritdoc />
        public async Task<List<MembershipPayment>> GetMembershipPayments(Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.MembershipPayments
                .Include(p => p.Member)
                .ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<MembershipPayment?> GetMembershipPayment(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.MembershipPayments.FindAsync(id, ct);
        }

        /// <inheritdoc />
        public async Task<List<EnrollmentPayment>> GetEnrollmentPayments(Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.EnrollmentPayments
                .Include(p => p.Member)
                .Include(p => p.Activity)
                .ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<EnrollmentPayment?> GetEnrollmentPayment(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.EnrollmentPayments.FindAsync(id, ct);
        }

        /// <inheritdoc />
        public async Task<PostPaymentResponse> CreateMembershipPayment(PostMembershipPaymentDTO dto)
        {
            logger.LogInformation("Creating membership payment for member {MemberId}.", dto.MemberId);
            var member = await GetMemberOrThrow(dto.MemberId);

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                EnsureMemberHasNoPaidMembership(dto.MemberId);
                
                // return existing payment
                var existingResponse = await HandleExistingMembershipPayment(member, dto.MemberId);
                if (existingResponse != null)
                {
                    logger.LogInformation("Reusing existing membership payment for member {MemberId}.", dto.MemberId);
                    return existingResponse;
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
        public async Task<(byte[] Content, string FileName)> ExportPaymentsToCsv(DateTime startDate, DateTime endDate, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            var startDateInNL = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(startDate, "W. Europe Standard Time").Date.ToUniversalTime();
            var endDateInNL = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(endDate, "W. Europe Standard Time").Date.ToUniversalTime();

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

            var csv = BuildExportCsv(startDate, endDate, enrollmentPayments, membershipPayments, paymentServiceFeePayments);
            logger.LogInformation("Exported payments CSV for period {StartDate} - {EndDate}. Enrollment: {EnrollmentCount}, Membership: {MembershipCount}, PaymentServiceFee: {PaymentServiceFeeCount}",
                startDate, endDate, enrollmentPayments.Count, membershipPayments.Count, paymentServiceFeePayments.Count);

            var fileName = $"payments_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
            return (Encoding.UTF8.GetBytes(csv.ToString()), fileName);
        }

        /// <inheritdoc />
        public async Task<PostPaymentResponse> CreateActivityPayment(PostActivityPaymentDTO dto, Guid userId)
        {
            logger.LogInformation("Creating activity payment for member {MemberId} and {ActivityCount} activities. Manual: {Manual}",
                dto.MemberId, dto.ActivityIds.Count, dto.ManuallyMarkedAsPaid);
            if(userId != dto.MemberId)
            {
                permissionService.EnsureBoardOrCandidateBoardMember(userId);
            }

            var member = await GetMemberOrThrow(dto.MemberId);
            
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var enrollments = await db.Enrollments
                    .Include(e => e.Activity)
                    .Where(e => dto.ActivityIds.Contains(e.ActivityId) && e.MemberId == dto.MemberId)
                    .ToListAsync();

                if (enrollments.Count != dto.ActivityIds.Count)
                    throw new Exception("One or more enrollments not found");

                var totalPrice = enrollments.Sum(e =>
                    paymentValidationService.GetUnpaidAmountForEnrollment(e)
                );

                if (dto.ManuallyMarkedAsPaid)
                {
                    // If payment is manually marked as paid, we can skip creating a payment service fee payment and create it directly in the database
                    permissionService.EnsureBoardOrCandidateBoardMember(userId);
                    CreateEnrollmentPayments(dto.MemberId, enrollments, true);

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.LogInformation("Created manual activity payment records for member {MemberId}.", dto.MemberId);
                    return new PostPaymentResponse();
                }
                else
                {
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
            if(allUsers)
            {
                permissionService.EnsureBoardOrCandidateBoardMember(userId);
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
            if(userId != Guid.Empty)
            {
                permissionService.EnsureBoardOrCandidateBoardMember(userId);
            }

            return paymentValidationService.GetAllOverpaidEnrollments();
        }

        /// <inheritdoc />
        public async Task<object> GetMemberPaymentStatus(Guid fromUserId, Guid userId, CancellationToken ct)
        {
            if(fromUserId != userId)
            {
                permissionService.EnsureBoardOrCandidateBoardMember(userId);
            }

            var member = await db.Members
                .Include(m => m.StudyEnrollments)
                .ThenInclude(se => se.Study)
                .Include(m => m.Enrollments)
                .FirstOrDefaultAsync(m => m.Id == fromUserId, ct);

            if (member == null) throw new KeyNotFoundException("Member not found");

            var unpaid = paymentValidationService.GetUnpaidEnrollmentsForMember(member.Id);
            var hasPaidMembershipBeforeExpirationTime = paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);
            var hasEverPaidMembership = paymentValidationService.HasEverPaidMembershipPayment(member.Id);

            return new PaymentStatusResponse
            {
                MemberId = member.Id,
                HasEverPaidMembership = hasEverPaidMembership,
                HasPaidMembershipBeforeExpirationTime = hasPaidMembershipBeforeExpirationTime,
                HasPaidAllActivities = !unpaid.Any(),
                UnpaidEnrollments = unpaid
            };
        }

        private async Task<Member> GetMemberOrThrow(Guid memberId)
        {
            var member = await db.Members.FindAsync(memberId);
            return member ?? throw new KeyNotFoundException("Member not found");
        }

        private void EnsureMemberHasNoPaidMembership(Guid memberId)
        {
            if (paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(memberId))
            {
                throw new InvalidOperationException("Member already paid membership");
            }
        }

        private async Task<PostPaymentResponse?> HandleExistingMembershipPayment(Member member, Guid memberId)
        {
            var existingPayment = await db.MembershipPayments.FirstOrDefaultAsync(p => p.MemberId == memberId);
            if (existingPayment == null)
                return null;

            var paymentResponse = await paymentService.GetPaymentAsync(existingPayment.PaymentServiceId);
            if(paymentResponse.Status == PaymentStatus.Paid)
            {
                throw new InvalidOperationException("Member already paid membership");
            }

            if (paymentResponse.Status == PaymentStatus.Pending)
            {
                return ToCheckoutResponse(existingPayment.PaymentIntentUrl);
            }

            db.MembershipPayments.Remove(existingPayment);
            
            db.Members.Remove(member);
            await authOutboxWorker.EnqueueTask(AuthTaskType.Delete, member.AuthSystemUserId ?? throw new Exception("Member isn't synced with the authentication system yet, cannot sync payment status."));
            
            await db.SaveChangesAsync();
            await db.Database.CommitTransactionAsync();
            
            throw new InvalidOperationException("Payment is expired or canceled.");
        }

        private async Task<CreatePaymentResponse> BuildMembershipPaymentRequest(Member member, Guid memberId)
        {
            return await paymentService.CreatePaymentAsync(
                decimal.Parse(db.Settings.Find("MembershipPrice")?.Value ?? "7.50"), 
                $"Membership payment for {member.FirstName} {member.LastName}",
                $"{_frontendUrl}/confirm-mail",
                string.IsNullOrEmpty(_ngrokUrl) ? 
                    (_backendUrl.ToLower().Contains("localhost") ? null : _backendUrl + "/payments/webhook")
                    : $"{_ngrokUrl}/payments/webhook",
                $"membership_{memberId}"
            );
        }

        private async Task<MembershipPayment> BuildMembershipPayment(Guid memberId, CreatePaymentResponse paymentResponse)
        {
            return new MembershipPayment
            {
                MemberId = memberId,
                Price = decimal.TryParse((await db.Settings.FindAsync("MembershipPrice"))?.Value ?? "7.50", out var price) ? price : 7.50m,
                PaymentServiceId = paymentResponse.PaymentId,
                PaymentIntentUrl = paymentResponse.PaymentUrl
            };
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
                _frontendUrl,
                string.IsNullOrEmpty(_ngrokUrl) ? 
                    (_backendUrl.ToLower().Contains("localhost") ? null : _backendUrl + "/payments/webhook")
                    : $"{_ngrokUrl}/payments/webhook",
                $"activity_{dto.MemberId}_{string.Join("_", dto.ActivityIds)}"
            );
        }

        private decimal GetPaymentServiceFee()
        {
            return db.Settings.Where(s => s.Name == "PaymentServiceFee").Select(s => decimal.Parse(s.Value)).FirstOrDefault();
        }

        private void CreateEnrollmentPayments(Guid memberId, List<Enrollment> enrollments, bool manuallyMarkedAsPaid, CreatePaymentResponse? paymentResponse = null)
        {
            if(paymentResponse == null && !manuallyMarkedAsPaid)
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


        private StringBuilder BuildExportCsv(DateTime startDate, DateTime endDate, List<EnrollmentPayment> enrollmentPayments, List<MembershipPayment> membershipPayments, List<PaymentServiceFeePayment> paymentServiceFeePayments)
        {
            var csv = new StringBuilder();
            
            var invoiceDate = endDate.AddDays(-1).ToString("dd-MM-yyyy");
            var periodLabel = $"ideal - {startDate:dd-MM-yyyy} / {endDate:dd-MM-yyyy}";
            var paymentsCondition = db.Settings.Where(s => s.Name == "PaymentServicePaymentsCondition").Select(s => s.Value).FirstOrDefault() ?? "2";
            var paymentServiceRelationalCode = db.Settings.Where(s => s.Name == "PaymentServiceRelationCode").Select(s => s.Value).FirstOrDefault() ?? "473";
            csv.AppendLine($"factuurdatum;{invoiceDate};{periodLabel};{paymentsCondition};{paymentServiceRelationalCode}");

            foreach (var p in enrollmentPayments)
            {
                var glAccount = p.Activity?.GLAccountId ?? p.Activity?.Organizer?.DefaultGLAccount ?? db.Settings.Where(s => s.Name == "ActivityGLAccount").Select(s => s.Value).FirstOrDefault() ?? "7001";
                var groupName = p.Activity?.Organizer?.Name ?? "Unknown Organizer";
                var activityName = p.Activity?.Name ?? "Unknown Activity";
                var costCenter = p.Activity?.CostCenterId ?? p.Activity?.Organizer?.DefaultCostCenter ?? "";
                var costUnit = p.Activity?.CostUnitId ?? "";
                var VATCode = p.Activity?.VatRate?.ToString() ?? "";
                var price = p.Price;

                var description = $"{groupName} | {activityName}";
                csv.AppendLine($";{glAccount};{description};{VATCode};{price};{costCenter};{costUnit}");
            }

            foreach (var p in membershipPayments)
            {
                var glAccount = db.Settings.Where(s => s.Name == "MembershipGLAccount").Select(s => s.Value).FirstOrDefault() ?? "8000";
                var description = "Lidmaatschap";
                var VATCode = db.Settings.Where(s => s.Name == "MembershipVATCode").Select(s => s.Value).FirstOrDefault() ?? "0";
                var price = p.Price;
                
                csv.AppendLine($";{glAccount};{description};{VATCode};{price};;");
            }

            var groupedFees = paymentServiceFeePayments
                .GroupBy(p => p.Price)
                .Select(g => new {
                    UnitPrice = g.Key,
                    Count = g.Count(),
                    TotalPrice = g.Sum(p => p.Price)
                });

            var paymentServiceFeeGLAccount = db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeGLAccount")?.Value ?? "5007";
            var paymentServiceFeeCostCenter = db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeCostCenter")?.Value ?? "TRX";
            var vatCode = db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeVATCode")?.Value ?? "21";

            foreach (var group in groupedFees)
            {
                var description = $"Transaction costs {group.UnitPrice:N2} x {group.Count}";
                
                var totalPrice = group.TotalPrice;

                csv.AppendLine($";{paymentServiceFeeGLAccount};{description};{vatCode};{totalPrice};{paymentServiceFeeCostCenter};;");
            }

            return csv;
        }
    }
}

using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Backend.Services
{
    public class PaymentService(
        PostgresDbContext db,
        IPermissionService permissionService,
        IPaymentValidationService paymentValidationService,
        IPaymentClient mollieClient,
        KeycloakOutboxWorker keycloakOutboxWorker,
        ILogger<PaymentService> logger
    ) : IPaymentService
    {
        private readonly string _frontendUrl = Environment.GetEnvironmentVariable("HostUrl")!;
        private readonly string _backendUrl = Environment.GetEnvironmentVariable("ApiUrl")!;
        private readonly string? _ngrokUrl = Environment.GetEnvironmentVariable("NGROK_URL");

        public async Task<List<MembershipPayment>> GetMembershipPayments(Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.MembershipPayments
                .Include(p => p.Member)
                .ToListAsync(ct);
        }

        public async Task<MembershipPayment?> GetMembershipPayment(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.MembershipPayments.FindAsync(id, ct);
        }

        public async Task<List<EnrollmentPayment>> GetEnrollmentPayments(Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.EnrollmentPayments
                .Include(p => p.Member)
                .Include(p => p.Activity)
                .ToListAsync(ct);
        }

        public async Task<EnrollmentPayment?> GetEnrollmentPayment(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.EnrollmentPayments.FindAsync(id, ct);
        }

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
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // create new payment
            var request = BuildMembershipPaymentRequest(member, dto.MemberId);
            var mollieResponse = await mollieClient.CreatePaymentAsync(request);

            // Build the payment record and save to database
            if (mollieResponse.Links.Checkout == null)
                throw new Exception("No checkout URL from Mollie");

            var payment = await BuildMembershipPayment(dto.MemberId, mollieResponse);
            StateValidator.Validate(payment);

            db.MembershipPayments.Add(payment);
            await db.SaveChangesAsync();
            logger.LogInformation("Created membership payment {PaymentId} for member {MemberId}.", payment.Id, dto.MemberId);

            return ToCheckoutResponse(mollieResponse.Links.Checkout.Href);
        }

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

            var mollieFeePayments = await db.MollieFeePayments
                .Where(p => p.PaidAt >= startDate && p.PaidAt <= endDate && !p.ManuallyMarkedAsPaid)
                .ToListAsync(ct);

            var csv = BuildExportCsv(startDate, endDate, enrollmentPayments, membershipPayments, mollieFeePayments);
            logger.LogInformation("Exported payments CSV for period {StartDate} - {EndDate}. Enrollment: {EnrollmentCount}, Membership: {MembershipCount}, MollieFee: {MollieFeeCount}",
                startDate, endDate, enrollmentPayments.Count, membershipPayments.Count, mollieFeePayments.Count);

            var fileName = $"payments_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
            return (Encoding.UTF8.GetBytes(csv.ToString()), fileName);
        }

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
                    // If payment is manually marked as paid, we can skip creating a molliepayment and create it directly in the database
                    permissionService.EnsureBoardOrCandidateBoardMember(userId);
                    CreateEnrollmentPayments(dto.MemberId, enrollments, true);

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.LogInformation("Created manual activity payment records for member {MemberId}.", dto.MemberId);
                    return new PostPaymentResponse();
                }
                else
                {
                    // Create Mollie payment
                    PaymentResponse mollieResponse = await CreateMolliePaymentRequest(totalPrice, member, dto);

                    // Create the payment for the mollie fee
                    MollieFeePayment mollieFeePayment = new MollieFeePayment
                    {
                        MemberId = dto.MemberId,
                        Price = GetMollieFee(),
                        MollieId = dto.ManuallyMarkedAsPaid ? "" : mollieResponse!.Id,
                        PaymentIntentUrl = dto.ManuallyMarkedAsPaid ? "" : mollieResponse!.Links.Checkout!.Href,
                        PaidAt = dto.ManuallyMarkedAsPaid ? DateTime.UtcNow : (DateTime?)null
                    };

                    db.MollieFeePayments.Add(mollieFeePayment);

                    // Create the enrollment payment with mollieResponse information
                    CreateEnrollmentPayments(dto.MemberId, enrollments, false, mollieResponse);
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.LogInformation("Created mollie-backed activity payment for member {MemberId}. MollieId: {MollieId}",
                        dto.MemberId, mollieResponse.Id);

                    return new PostPaymentResponse { CheckoutUrl = mollieResponse.Links.Checkout!.Href };
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Failed creating activity payment for member {MemberId}.", dto.MemberId);
                throw;
            }
        }

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

        public IEnumerable<EnrollmentBalance> GetOverpaid(Guid userId)
        {
            if(userId != Guid.Empty)
            {
                permissionService.EnsureBoardOrCandidateBoardMember(userId);
            }

            return paymentValidationService.GetAllOverpaidEnrollments();
        }

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
            var hasPaidMembership = paymentValidationService.HasPaidMembershipPayment(member.Id);

            return new
            {
                MemberId = member.Id,
                HasPaidMembership = hasPaidMembership,
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
            if (paymentValidationService.HasPaidMembershipPayment(memberId))
            {
                throw new InvalidOperationException("Member already paid membership");
            }
        }

        private async Task<PostPaymentResponse?> HandleExistingMembershipPayment(Member member, Guid memberId)
        {
            var existingPayment = await db.MembershipPayments.FirstOrDefaultAsync(p => p.MemberId == memberId);
            if (existingPayment == null)
                return null;

            var molliePayment = await mollieClient.GetPaymentAsync(existingPayment.MollieId);
            if(molliePayment.Status == "paid")
            {
                throw new InvalidOperationException("Member already paid membership");
            }

            if (molliePayment.Status != "pending")
            {
                return ToCheckoutResponse(existingPayment.PaymentIntentUrl);
            }

            db.MembershipPayments.Remove(existingPayment);
            
            db.Members.Remove(member);
            await keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Delete, member.KeycloakId ?? throw new Exception("Member isn't synced with Keycloak yet, cannot sync payment status."));
            
            await db.SaveChangesAsync();
            await db.Database.CommitTransactionAsync();
            
            throw new InvalidOperationException("Payment is expired or canceled.");
        }

        private PaymentRequest BuildMembershipPaymentRequest(Member member, Guid memberId)
        {
            return new PaymentRequest
            {
                Amount = new Amount(Currency.EUR, 7.50m),
                Description = $"Membership payment for {member.FirstName} {member.LastName}",
                RedirectUrl = $"{_frontendUrl}/confirm-mail",
                WebhookUrl = string.IsNullOrEmpty(_ngrokUrl) ? 
                    (_backendUrl.ToLower().Contains("localhost") ? null : _backendUrl + "/api/payments/webhook")
                    : $"{_ngrokUrl}/api/payments/webhook",
                Metadata = $"membership_{memberId}"
            };
        }

        private async Task<MembershipPayment> BuildMembershipPayment(Guid memberId, PaymentResponse mollieResponse)
        {
            return new MembershipPayment
            {
                MemberId = memberId,
                Price = decimal.TryParse((await db.Settings.FindAsync("MembershipPrice"))?.Value ?? "7.50", out var price) ? price : 7.50m,
                MollieId = mollieResponse.Id,
                PaymentIntentUrl = mollieResponse.Links.Checkout!.Href
            };
        }

        private static PostPaymentResponse ToCheckoutResponse(string checkoutUrl)
        {
            return new PostPaymentResponse { CheckoutUrl = checkoutUrl };
        }

        private async Task<PaymentResponse> CreateMolliePaymentRequest(decimal totalPrice, Member member, PostActivityPaymentDTO dto)
        {
            var request = new PaymentRequest
            {
                Amount = new Amount(Currency.EUR, totalPrice + GetMollieFee()),
                Description = $"Activity payment for {member.FirstName} {member.LastName}",
                RedirectUrl = _frontendUrl,
                WebhookUrl = _backendUrl.ToLower().Contains("localhost") ? null : $"{_backendUrl}/api/payments/webhook",
                Metadata = $"activity_{dto.MemberId}_{string.Join("_", dto.ActivityIds)}"
            };

            var mollieResponse = await mollieClient.CreatePaymentAsync(request);

            if (mollieResponse.Links.Checkout == null)
                throw new Exception("No checkout URL from Mollie");

            return mollieResponse;
        }

        private decimal GetMollieFee()
        {
            return db.Settings.Where(s => s.Name == "MollieFee").Select(s => decimal.Parse(s.Value)).FirstOrDefault();
        }

        private void CreateEnrollmentPayments(Guid memberId, List<Enrollment> enrollments, bool manuallyMarkedAsPaid, PaymentResponse? mollieResponse = null)
        {
            if(mollieResponse == null && !manuallyMarkedAsPaid)
            {
                throw new ArgumentException("Mollie response must be provided if payment is not manually marked as paid");
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
                    MollieId = manuallyMarkedAsPaid ? "" : mollieResponse?.Id ?? "",
                    PaymentIntentUrl = manuallyMarkedAsPaid ? "" : mollieResponse?.Links.Checkout?.Href ?? "",
                    PaidAt = manuallyMarkedAsPaid ? DateTime.UtcNow : (DateTime?)null
                };

                StateValidator.Validate(payment);

                db.EnrollmentPayments.Add(payment);
            }
        }


        private StringBuilder BuildExportCsv(DateTime startDate, DateTime endDate, List<EnrollmentPayment> enrollmentPayments, List<MembershipPayment> membershipPayments, List<MollieFeePayment> mollieFeePayments)
        {
            var csv = new StringBuilder();
            
            var invoiceDate = endDate.AddDays(-1).ToString("dd-MM-yyyy");
            var periodLabel = $"ideal - {startDate:dd-MM-yyyy} / {endDate:dd-MM-yyyy}";
            var paymentsCondition = db.Settings.Where(s => s.Name == "MolliePaymentsCondition").Select(s => s.Value).FirstOrDefault() ?? "2";
            var mollieRelationCode = db.Settings.Where(s => s.Name == "MollieRelationCode").Select(s => s.Value).FirstOrDefault() ?? "473";
            csv.AppendLine($"factuurdatum;{invoiceDate};{periodLabel};{paymentsCondition};{mollieRelationCode}");

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

            var groupedFees = mollieFeePayments
                .GroupBy(p => p.Price)
                .Select(g => new {
                    UnitPrice = g.Key,
                    Count = g.Count(),
                    TotalPrice = g.Sum(p => p.Price)
                });

            var mollieFeeGLAccount = db.Settings.FirstOrDefault(s => s.Name == "MollieFeeGLAccount")?.Value ?? "5007";
            var mollieFeeCostCenter = db.Settings.FirstOrDefault(s => s.Name == "MollieFeeCostCenter")?.Value ?? "TRX";
            var vatCode = db.Settings.FirstOrDefault(s => s.Name == "MollieFeeVATCode")?.Value ?? "21";

            foreach (var group in groupedFees)
            {
                var description = $"Transaction costs {group.UnitPrice:N2} x {group.Count}";
                
                var totalPrice = group.TotalPrice;

                csv.AppendLine($";{mollieFeeGLAccount};{description};{vatCode};{totalPrice};{mollieFeeCostCenter};;");
            }

            return csv;
        }
    }
}

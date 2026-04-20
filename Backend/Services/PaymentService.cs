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

namespace Backend.Services
{
    public class PaymentService(
        PostgresDbContext db,
        IPaymentValidationService paymentValidationService,
        IPaymentClient mollieClient,
        KeycloakOutboxWorker keycloakOutboxWorker
    ) : IPaymentService
    {
        private readonly string _frontendUrl = Environment.GetEnvironmentVariable("HostUrl")!;
        private readonly string _backendUrl = Environment.GetEnvironmentVariable("ApiUrl")!;
        private readonly string? _ngrokUrl = Environment.GetEnvironmentVariable("NGROK_URL");

        public async Task<List<MembershipPayment>> GetMembershipPayments(CancellationToken ct)
        {
            return await db.MembershipPayments
                .Include(p => p.Member)
                .ToListAsync(ct);
        }

        public async Task<MembershipPayment?> GetMembershipPayment(uint id, CancellationToken ct)
        {
            return await db.MembershipPayments.FindAsync(id, ct);
        }

        public async Task<List<EnrollmentPayment>> GetEnrollmentPayments(CancellationToken ct)
        {
            return await db.EnrollmentPayments
                .Include(p => p.Member)
                .Include(p => p.Activity)
                .ToListAsync(ct);
        }

        public async Task<EnrollmentPayment?> GetEnrollmentPayment(uint id, CancellationToken ct)
        {
            return await db.EnrollmentPayments.FindAsync(id, ct);
        }

        public async Task<PostPaymentResponse> CreateMembershipPayment(PostMembershipPaymentDTO dto)
        {
            var member = await db.Members.FindAsync(dto.MemberId);
            if (member == null) throw new Exception("Member not found");

            var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if(paymentValidationService.HasPaidMembershipPayment(dto.MemberId))
                {
                    throw new InvalidOperationException("Member already paid membership");
                }
                
                var existingPayment = await db.MembershipPayments.FirstOrDefaultAsync(p => p.MemberId == dto.MemberId);
                if (existingPayment != null)
                {
                    var molliePayment = await mollieClient.GetPaymentAsync(existingPayment.MollieId);
                    if(molliePayment.Status == "paid")
                    {
                        throw new InvalidOperationException("Member already paid membership");
                    }

                    if (molliePayment.Status != "pending")
                    {
                        return new PostPaymentResponse { CheckoutUrl = existingPayment.PaymentIntentUrl };
                    }

                    db.MembershipPayments.Remove(existingPayment);
                    
                    db.Members.Remove(member);
                    keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Delete, member.KeycloakId ?? throw new Exception("Member isn't synced with Keycloak yet, cannot sync payment status."));
                    
                    await db.SaveChangesAsync();
                    await db.Database.CommitTransactionAsync();
                    
                    throw new InvalidOperationException("Payment is expired or canceled.");
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            var request = new PaymentRequest
            {
                Amount = new Amount(Currency.EUR, 7.50m),
                Description = $"Membership payment for {member.FirstName} {member.LastName}",
                RedirectUrl = $"{_frontendUrl}/confirm-mail",
                WebhookUrl = string.IsNullOrEmpty(_ngrokUrl) ? 
                    (_backendUrl.ToLower().Contains("localhost") ? null : _backendUrl + "/api/payments/webhook")
                    : $"{_ngrokUrl}/api/payments/webhook",
                Metadata = $"membership_{dto.MemberId}"
            };

            var mollieResponse = await mollieClient.CreatePaymentAsync(request);

            if (mollieResponse.Links.Checkout == null)
                throw new Exception("No checkout URL from Mollie");

            var payment = new MembershipPayment
            {
                MemberId = dto.MemberId,
                Price = 7.50m,
                MollieId = mollieResponse.Id,
                PaymentIntentUrl = mollieResponse.Links.Checkout.Href
            };

            StateValidator.Validate(payment);

            db.MembershipPayments.Add(payment);
            await db.SaveChangesAsync();

            return new PostPaymentResponse { CheckoutUrl = mollieResponse.Links.Checkout.Href };
        }

        public async Task<(byte[] Content, string FileName)> ExportPaymentsToCsv(DateTime startDate, DateTime endDate, CancellationToken ct)
        {
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

            var fileName = $"payments_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
            return (Encoding.UTF8.GetBytes(csv.ToString()), fileName);
        }

        public async Task<PostPaymentResponse> CreateActivityPayment(PostActivityPaymentDTO dto)
        {
            var member = await db.Members.FindAsync(dto.MemberId);
            if (member == null) throw new Exception("Member not found");

            var enrollments = await db.Enrollments
                .Include(e => e.Activity)
                .Where(e => dto.ActivityIds.Contains(e.ActivityId) && e.MemberId == dto.MemberId)
                .ToListAsync();

            if (enrollments.Count != dto.ActivityIds.Count)
                throw new Exception("One or more enrollments not found");

            var totalPrice = enrollments.Sum(e =>
                paymentValidationService.GetUnpaidAmountForEnrollment(e)
            );

            PaymentResponse? mollieResponse = null;

            if (dto.ManuallyMarkedAsPaid)
            {
                // TO DO: Check if board
            }
            else
            {
                var request = new PaymentRequest
                {
                    Amount = new Amount(Currency.EUR, totalPrice + db.Settings.Where(s => s.Name == "MollieFee").Select(s => decimal.Parse(s.Value)).FirstOrDefault()),
                    Description = $"Activity payment for {member.FirstName} {member.LastName}",
                    RedirectUrl = _frontendUrl,
                    WebhookUrl = _backendUrl.ToLower().Contains("localhost") ? null : $"{_backendUrl}/api/payments/webhook",
                    Metadata = $"activity_{dto.MemberId}_{string.Join("_", dto.ActivityIds)}"
                };

                mollieResponse = await mollieClient.CreatePaymentAsync(request);

                if (mollieResponse.Links.Checkout == null)
                    throw new Exception("No checkout URL from Mollie");

                MollieFeePayment mollieFeePayment = new MollieFeePayment
                {
                    MemberId = dto.MemberId,
                    Price = db.Settings.Where(s => s.Name == "MollieFee").Select(s => decimal.Parse(s.Value)).FirstOrDefault(),
                    MollieId = dto.ManuallyMarkedAsPaid ? "" : mollieResponse!.Id,
                    PaymentIntentUrl = dto.ManuallyMarkedAsPaid ? "" : mollieResponse!.Links.Checkout!.Href,
                    PaidAt = dto.ManuallyMarkedAsPaid ? DateTime.UtcNow : (DateTime?)null
                };

                db.MollieFeePayments.Add(mollieFeePayment);
            }

            foreach (var enrollment in enrollments)
            {
                if (!enrollment.Activity.IsOpenForPayment)
                    throw new Exception($"Activity {enrollment.Activity.Name} is not open for payment");

                var price = paymentValidationService.GetUnpaidAmountForEnrollment(enrollment);
                if (price <= 0) continue;

                var payment = new EnrollmentPayment
                {
                    MemberId = dto.MemberId,
                    ActivityId = enrollment.ActivityId,
                    Price = price,
                    MollieId = dto.ManuallyMarkedAsPaid ? "" : mollieResponse!.Id,
                    PaymentIntentUrl = dto.ManuallyMarkedAsPaid ? "" : mollieResponse!.Links.Checkout!.Href,
                    PaidAt = dto.ManuallyMarkedAsPaid ? DateTime.UtcNow : (DateTime?)null
                };

                StateValidator.Validate(payment);

                db.EnrollmentPayments.Add(payment);
            }

            await db.SaveChangesAsync();

            return new PostPaymentResponse { CheckoutUrl = mollieResponse?.Links.Checkout?.Href ?? "" };
        }

        public IEnumerable<EnrollmentBalance> GetUnpaid(Guid userId, bool allUsers = false)
        {
            if (allUsers)
            {
                return paymentValidationService.GetAllUnpaidEnrollments();
            }
            else
            {
                return paymentValidationService.GetUnpaidEnrollmentsForMember(userId);
            }
        }

        public IEnumerable<EnrollmentBalance> GetOverpaid()
        {
            return paymentValidationService.GetAllOverpaidEnrollments();
        }

        public async Task<object> GetMemberPaymentStatus(Guid memberId, CancellationToken ct)
        {
            var member = await db.Members
                .Include(m => m.StudyEnrollments)
                .ThenInclude(se => se.Study)
                .Include(m => m.Enrollments)
                .FirstOrDefaultAsync(m => m.Id == memberId, ct);

            if (member == null) throw new Exception("Member not found");

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
    }
}
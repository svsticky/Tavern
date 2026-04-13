using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;

namespace Backend.Services
{
    public class PaymentService(
        PostgresDbContext db,
        IPaymentValidationService paymentValidationService
    ) : IPaymentService
    {
        private readonly string _frontendUrl = Environment.GetEnvironmentVariable("HostUrl")!;
        private readonly string _backendUrl = Environment.GetEnvironmentVariable("ApiUrl")!;

        private readonly string? _ngrokUrl = Environment.GetEnvironmentVariable("NGROK_URL");

        private readonly decimal _mollieFee = Environment.GetEnvironmentVariable("MOLLIE_FEE") != null ? decimal.Parse(Environment.GetEnvironmentVariable("MOLLIE_FEE")!) : 0.00m;

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

        public async Task<PostPaymentResponse> CreateMembershipPayment(PostMembershipPaymentDTO dto, IPaymentClient paymentClient)
        {
            var member = await db.Members.FindAsync(dto.MemberId);
            if (member == null) throw new Exception("Member not found");

            var existingPayments = await db.MembershipPayments
                .Where(p => p.MemberId == dto.MemberId)
                .ToListAsync();

            foreach (var existing in existingPayments)
            {
                if (existing.PaidAt == null)
                {
                    var molliePayment = await paymentClient.GetPaymentAsync(existing.MollieId);

                    if (molliePayment.Status == "expired" || molliePayment.Status == "canceled")
                    {
                        db.MembershipPayments.Remove(existing);
                    }
                    else
                    {
                        return new PostPaymentResponse { CheckoutUrl = existing.PaymentIntentUrl };
                    }
                }
                else
                {
                    throw new Exception("Member already paid membership");
                }
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

            var mollieResponse = await paymentClient.CreatePaymentAsync(request);

            if (mollieResponse.Links.Checkout == null)
                throw new Exception("No checkout URL from Mollie");

            var payment = new MembershipPayment
            {
                MemberId = dto.MemberId,
                Price = 7.50m,
                MollieId = mollieResponse.Id,
                PaymentIntentUrl = mollieResponse.Links.Checkout.Href
            };

            StateValidateUtils.Validate(payment);

            db.MembershipPayments.Add(payment);
            await db.SaveChangesAsync();

            return new PostPaymentResponse { CheckoutUrl = mollieResponse.Links.Checkout.Href };
        }

        public async Task<PostPaymentResponse> CreateActivityPayment(PostActivityPaymentDTO dto, IPaymentClient paymentClient)
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
                    Amount = new Amount(Currency.EUR, totalPrice + _mollieFee),
                    Description = $"Activity payment for {member.FirstName} {member.LastName}",
                    RedirectUrl = _frontendUrl,
                    WebhookUrl = _backendUrl.ToLower().Contains("localhost") ? null : $"{_backendUrl}/api/payments/webhook",
                    Metadata = $"activity_{dto.MemberId}_{string.Join("_", dto.ActivityIds)}"
                };

                mollieResponse = await paymentClient.CreatePaymentAsync(request);

                if (mollieResponse.Links.Checkout == null)
                    throw new Exception("No checkout URL from Mollie");

                MollieFeePayment mollieFeePayment = new MollieFeePayment
                {
                    MemberId = dto.MemberId,
                    Price = _mollieFee,
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

                StateValidateUtils.Validate(payment);

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
using Microsoft.AspNetCore.Mvc;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models.Payment.Response;
using Backend.Utils;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Payments(PostgresDbContext db) : ControllerBase
    {
        private string _frontendUrl = Environment.GetEnvironmentVariable("HostUrl")!;

        // GET: api/payments/membership
        [HttpGet("membership")]
        public async Task<ActionResult<IEnumerable<MembershipPayment>>> GetMembershipPayments(CancellationToken ct)
        {
            return await db.MembershipPayments.Include(p => p.Member).ToListAsync(ct);
        }

        // GET: api/payments/membership/5
        [HttpGet("membership/{id}")]
        public async Task<ActionResult<MembershipPayment>> GetMembershipPayment(uint id, CancellationToken ct)
        {
            MembershipPayment? payment = await db.MembershipPayments.FindAsync(id, ct);

            return payment != null ? payment : NotFound();
        }

        // GET: api/payments/enrollment
        [HttpGet("enrollment")]
        public async Task<ActionResult<IEnumerable<EnrollmentPayment>>> GetEnrollmentPayments(CancellationToken ct)
        {
            return await db.EnrollmentPayments.Include(p => p.Member).Include(p => p.Activity).ToListAsync(ct);
        }

        // GET: api/payments/enrollment/membership/5
        [HttpGet("enrollment/{id}")]
        public async Task<ActionResult<EnrollmentPayment>> GetEnrollmentPayment(uint id, CancellationToken ct)
        {
            EnrollmentPayment? payment = await db.EnrollmentPayments.FindAsync(id, ct);

            return payment != null ? payment : NotFound();
        }        

        // POST: api/payments/membership
        [HttpPost("membership")]
        public async Task<ActionResult> PostMembershipPayment(PostMembershipPaymentDTO dto, [FromServices] IPaymentClient paymentClient)
        {
            Member? member = await db.Members.FindAsync(dto.MemberId);
            if (member == null) return NotFound("Member not found");

            List<MembershipPayment> existingPayments = await db.MembershipPayments.Where(p => p.MemberId == dto.MemberId).ToListAsync();
           
            foreach (MembershipPayment existingPayment in existingPayments)
            {
                if (existingPayment.PaidAt == null) 
                {
                    // Payment isn't paid yet, check if it's expired
                    PaymentResponse molliePayment = await paymentClient.GetPaymentAsync(existingPayment.MollieId);
                    if (molliePayment.Status == "expired" || molliePayment.Status == "canceled")
                    {
                        // Payment is expired, we can remove it and create a new one
                        db.MembershipPayments.Remove(existingPayment);
                    }
                    else
                    {
                        // Payment isn't expired, return the existing payment link
                        return Ok(new { existingPayment.PaymentIntentUrl });
                    }
                }
                else
                {
                    return BadRequest("Member has already payed for a membership, if you think this is an error please contact support.");
                }
            }

            var amount = new Mollie.Api.Models.Amount(Mollie.Api.Models.Currency.EUR, 7.50m);
            var request = new Mollie.Api.Models.Payment.Request.PaymentRequest
            {
                Amount = amount,
                Description = $"Membership payment for {member.FirstName} {member.LastName}",
                RedirectUrl = $"{_frontendUrl}",
                WebhookUrl = _frontendUrl.ToLower().Contains("localhost") ? null : $"{_frontendUrl}/api/payments/webhook",
                Metadata = $"membership_{dto.MemberId}"
            };

            var mollieResponse = await paymentClient.CreatePaymentAsync(request);

            if(mollieResponse.Links.Checkout == null) return BadRequest("Mollie response did not contain a checkout link");

            var payment = new MembershipPayment
            {
                MemberId = dto.MemberId,
                Price = 7.50m,
                MollieId = mollieResponse.Id,
                PaymentIntentUrl = mollieResponse.Links.Checkout.Href
            };

            db.MembershipPayments.Add(payment);
            await db.SaveChangesAsync();

            return Ok(new { checkoutUrl = mollieResponse.Links.Checkout.Href });
        }

        // POST: api/activity/membership
        [HttpPost("activity")]
        public async Task<ActionResult> PostActivityPayment(PostActivityPaymentDTO dto, [FromServices] IPaymentClient paymentClient)
        {
            Member? member = await db.Members.FindAsync(dto.MemberId);
            if (member == null) return NotFound("Member not found");

            List<Enrollment> enrollments = await db.Enrollments
                .Include(e => e.Activity)
                .Where(e => dto.ActivityIds.Contains(e.ActivityId))
                .ToListAsync();
            if (enrollments.Count != dto.ActivityIds.Count) return NotFound("One or more enrollments not found");

            var enrollmentBalances = await db.Enrollments
                .Where(e => dto.ActivityIds.Contains(e.ActivityId) && e.MemberId == dto.MemberId)
                .Select(e => new
                {
                    Enrollment = e,
                    Activity = e.Activity,
                    PaidSum = db.EnrollmentPayments
                        .Where(p => p.PaidAt != null && p.ActivityId == e.ActivityId && p.MemberId == e.MemberId)
                        .Sum(p => (decimal?)p.Price) ?? 0
                })
                .ToListAsync();

            var totalPrice = enrollmentBalances.Sum(e => Math.Max(0, e.Enrollment.Price - e.PaidSum));
            var amount = new Mollie.Api.Models.Amount(Mollie.Api.Models.Currency.EUR, totalPrice);
            var request = new Mollie.Api.Models.Payment.Request.PaymentRequest
            {
                Amount = amount,
                Description = $"Activity payment for {member.FirstName} {member.LastName}",
                RedirectUrl = $"{_frontendUrl}",
                WebhookUrl = _frontendUrl.ToLower().Contains("localhost") ? null : $"{_frontendUrl}/api/payments/webhook",
                Metadata = $"activity_{dto.MemberId}_{string.Join("_", dto.ActivityIds)}"
            };

            var mollieResponse = await paymentClient.CreatePaymentAsync(request);

            foreach (var enrollment in enrollments)
            {
                if(!enrollment.Activity.IsOpenForPayment)
                {
                    return BadRequest($"Activity {enrollment.Activity.Name} is not open for payment");
                }

                decimal price = PaymentUtils.GetUnpaidAmountForEnrollment(enrollment, db);

                if(price <= 0) continue; // No need to create a payment for free activities

                if (mollieResponse.Links.Checkout == null) return BadRequest("Mollie response did not contain a checkout link");

                EnrollmentPayment payment = new EnrollmentPayment
                {
                    MemberId = dto.MemberId,
                    ActivityId = enrollment.ActivityId,
                    Price = price,
                    MollieId = mollieResponse.Id,
                    PaymentIntentUrl = mollieResponse.Links.Checkout.Href
                };

                db.EnrollmentPayments.Add(payment);
            }

            await db.SaveChangesAsync();

            if(mollieResponse.Links.Checkout == null) throw new Exception("Mollie response did not contain a checkout link");

            return Ok(new { checkoutUrl = mollieResponse.Links.Checkout.Href });
        }

        // POST: api/payments/webhook
        [HttpPost("webhook")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> MollieWebhook([FromForm] string id, [FromServices] IPaymentClient paymentClient)
        {
            PaymentResponse result = await paymentClient.GetPaymentAsync(id);

            var payments = await db.MembershipPayments.Where(p => p.MollieId == id).Cast<Payment>().ToListAsync();
            payments.AddRange(await db.EnrollmentPayments.Where(p => p.MollieId == id).Cast<Payment>().ToListAsync());

            if (payments.Count == 0) return NotFound();

            if (result.Status == "paid")
            {
                foreach (var payment in payments)
                {
                    payment.PaidAt = result.PaidAt?.ToString("O");
                    if(payment is MembershipPayment)
                    {
                        KeyCloakOutboxTask task = new KeyCloakOutboxTask
                        {
                            TaskType = KeycloakTaskType.Sync,
                            KeycoakId = payment.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID")
                        };
                        db.KeyCloakOutboxTasks.Add(task);
                    }
                }
                await db.SaveChangesAsync();
            }

            return Ok();
        }

        // GET: api/payments/unpaid
        [HttpGet("unpaid")]
        public ActionResult<IEnumerable<EnrollmentBalance>> GetAllUnpaid(CancellationToken ct)
        {
            var unpaid = PaymentUtils.GetAllUnpaidEnrollments(db);
            return Ok(unpaid);
        }

        // GET: api/payments/overpaid
        [HttpGet("overpaid")]
        public ActionResult<IEnumerable<EnrollmentBalance>> GetAllOverpaid(CancellationToken ct)
        {
            var overpaid = PaymentUtils.GetAllOverpaidEnrollments(db);
            return Ok(overpaid);
        }

        // GET: api/payments/member/{memberId}/status
        [HttpGet("member/{memberId}/status")]
        public async Task<ActionResult> GetMemberPaymentStatus(Guid memberId, CancellationToken ct)
        {
            var member = await db.Members
                .Include(m => m.StudyEnrollments)
                .ThenInclude(se => se.Study)
                .Include(m => m.Enrollments)
                .FirstOrDefaultAsync(m => m.Id == memberId, ct);

            if (member == null) return NotFound("Member not found");

            var unpaid = PaymentUtils.GetUnpaidEnrollmentsForMember(member, db);
            var hasPaidMembership = PaymentUtils.HasPaidMembershipPayment(member, db);
            var hasPaidEverything = !unpaid.Any();

            return Ok(new
            {
                MemberId = member.Id,
                HasPaidMembership = hasPaidMembership,
                HasPaidAllActivities = hasPaidEverything,
                UnpaidEnrollments = unpaid
            });
        }
    }
}
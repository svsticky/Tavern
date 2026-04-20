using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IPermissionService _permissionService;

        public PaymentsController(IPaymentService paymentService, IPermissionService permissionService)
        {
            _paymentService = paymentService;
            _permissionService = permissionService;
        }

        // GET: api/payments/membership
        [HttpGet("membership")]
        public async Task<ActionResult<IEnumerable<MembershipPayment>>> GetMembershipPayments(CancellationToken ct)
        {
            var result = await _paymentService.GetMembershipPayments(ct);
            return Ok(result);
        }

        // GET: api/payments/membership/5
        [HttpGet("membership/{id}")]
        public async Task<ActionResult<MembershipPayment>> GetMembershipPayment(uint id, CancellationToken ct)
        {
            var result = await _paymentService.GetMembershipPayment(id, ct);
            return result != null ? Ok(result) : NotFound();
        }

        // GET: api/payments/enrollment
        [HttpGet("enrollment")]
        public async Task<ActionResult<IEnumerable<EnrollmentPayment>>> GetEnrollmentPayments(CancellationToken ct)
        {
            var result = await _paymentService.GetEnrollmentPayments(ct);
            return Ok(result);
        }

        // GET: api/payments/enrollment/5
        [HttpGet("enrollment/{id}")]
        public async Task<ActionResult<EnrollmentPayment>> GetEnrollmentPayment(uint id, CancellationToken ct)
        {
            var result = await _paymentService.GetEnrollmentPayment(id, ct);
            return result != null ? Ok(result) : NotFound();
        }

        // POST: api/payments/membership
        [HttpPost("membership")]
        public async Task<ActionResult<PostPaymentResponse>> PostMembershipPayment(
            PostMembershipPaymentDTO dto
        )
        {
            try
            {
                var result = await _paymentService.CreateMembershipPayment(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/payments/activity
        [HttpPost("activity")]
        public async Task<ActionResult<PostPaymentResponse>> PostActivityPayment(
            PostActivityPaymentDTO dto
        )
        {
            try
            {
                var result = await _paymentService.CreateActivityPayment(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/payments/webhook
        [HttpPost("webhook")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult> MollieWebhook(
            [FromForm] string id,
            [FromServices] IPaymentWebhookService webhookService
        )
        {
            await webhookService.HandleWebhookAsync(id);
            return Ok();
        }

        // GET: api/payments/unpaid
        [HttpGet("unpaid")]
        public ActionResult<IEnumerable<EnrollmentBalance>> GetUnpaid(bool allUsers = false)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if(userId == null)
            {
                return BadRequest("UserId claim is missing");
            }

            bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(Guid.Parse(userId));

            if(allUsers && !isBoard)
            {
                return Forbid("Only board members can view all unpaid enrollments");
            }

            var result = _paymentService.GetUnpaid(Guid.Parse(userId), allUsers);
            return Ok(result);
        }

        // GET: api/payments/overpaid
        [HttpGet("overpaid")]
        public ActionResult<IEnumerable<EnrollmentBalance>> GetOverpaid()
        {
            var result = _paymentService.GetOverpaid();
            return Ok(result);
        }

        // GET: api/payments/member/{memberId}/status
        [HttpGet("member/{memberId}/status")]
        public async Task<ActionResult<object?>> GetMemberPaymentStatus(Guid memberId, CancellationToken ct)
        {
            try
            {
                var result = await _paymentService.GetMemberPaymentStatus(memberId, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        // GET: api/payments/export?startDate=2024-01-01&endDate=2024-12-31
        [HttpGet("export")]
        public async Task<ActionResult> ExportPaymentsToCsv(DateTime startDate, DateTime endDate, CancellationToken ct)
        {            
            try
            {                
                var (content, fileName) = await _paymentService.ExportPaymentsToCsv(startDate, endDate, ct);
                return File(content, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
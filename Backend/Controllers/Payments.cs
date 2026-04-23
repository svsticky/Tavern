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
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentService.GetMembershipPayments(userId, ct);
                return Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/payments/membership/5
        [HttpGet("membership/{id}")]
        public async Task<ActionResult<MembershipPayment>> GetMembershipPayment(uint id, CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentService.GetMembershipPayment(id, userId, ct);
                return result != null ? Ok(result) : NotFound();
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/payments/enrollment
        [HttpGet("enrollment")]
        public async Task<ActionResult<IEnumerable<EnrollmentPayment>>> GetEnrollmentPayments(CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentService.GetEnrollmentPayments(userId, ct);
                return Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/payments/enrollment/5
        [HttpGet("enrollment/{id}")]
        public async Task<ActionResult<EnrollmentPayment>> GetEnrollmentPayment(uint id, CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentService.GetEnrollmentPayment(id, userId, ct);
                return result != null ? Ok(result) : NotFound();
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentService.CreateActivityPayment(dto, userId);
                return Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
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
            try
            {
                await webhookService.HandleWebhookAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/payments/unpaid
        [HttpGet("unpaid")]
        public ActionResult<IEnumerable<EnrollmentBalance>> GetUnpaid(bool allUsers = false)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = _paymentService.GetUnpaid(userId, allUsers);

                if(result == null)
                    return NotFound();

                return Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/payments/overpaid
        [HttpGet("overpaid")]
        public ActionResult<IEnumerable<EnrollmentBalance>> GetOverpaid()
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = _paymentService.GetOverpaid(userId);

                if(result == null)
                    return NotFound();

                return Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/payments/member/{userId}/status
        [HttpGet("member/{userId}/status")]
        public async Task<ActionResult<object?>> GetMemberPaymentStatus(Guid fromUserId, CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentService.GetMemberPaymentStatus(fromUserId, userId, ct);

                if(result == null)
                    return NotFound();

                return Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
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
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var (content, fileName) = await _paymentService.ExportPaymentsToCsv(startDate, endDate, userId, ct);
                return File(content, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services.PaymentServices;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// Controller for managing financial transactions and payment records within the system. The PaymentsController serves as a centralized hub for processing membership fees, activity enrollments, and maintaining payment balances. It integrates with external payment providers through secure webhooks and provides administrative tools for auditing, such as exporting transaction data to CSV. This controller ensures that all financial operations are strictly authorized, allowing users to view their own payment history while granting administrative access for oversight and reporting, ultimately ensuring a transparent and manageable financial ecosystem for the organization.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepository;

        /// <summary>
        /// Initializes a new instance of the PaymentsController with the required payment and permission services.
        /// </summary>
        /// <param name="paymentRepository">The repository handling payment processing and ledger operations.</param>
        public PaymentsController(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        // GET: payments/membership
        /// <summary>
        /// Retrieves a history of membership-related payments for the authenticated user or organization. The GetMembershipPayments endpoint allows users to track their subscription or membership fees, providing a clear audit trail of past and pending transactions. By interacting with the IPaymentRepository, this endpoint fetches relevant financial records while ensuring that users can only access data they are authorized to view.
        /// </summary>
        /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A collection of membership payment records.</returns>
        [HttpGet("membership")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<MembershipPayment>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<MembershipPayment>>> GetMembershipPayments(CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentRepository.GetMembershipPayments(userId, ct);
                return Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }

        // GET: payments/membership/5
        /// <summary>
        /// Retrieves the details of a specific membership payment by its unique identifier. The GetMembershipPayment endpoint provides a granular view of a single transaction, including status, amount, and timestamp. This is essential for resolving billing inquiries and providing users with detailed receipts or proof of payment for their membership status.
        /// </summary>
        /// <param name="id">The unique identifier of the membership payment record.</param>
        /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
        /// <returns>The specific membership payment record if found; otherwise, a 404 Not Found status.</returns>
        [HttpGet("membership/{id}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(MembershipPayment), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MembershipPayment>> GetMembershipPayment(uint id, CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentRepository.GetMembershipPayment(id, userId, ct);
                return result != null ? Ok(result) : NotFound();
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }

        // GET: payments/enrollment
        /// <summary>
        /// Retrieves a list of payments specifically associated with activity enrollments. The GetEnrollmentPayments endpoint allows users to review the costs and payment statuses of the activities they have registered for. This helps maintain clarity regarding which events have been fully paid for and which require further financial action.
        /// </summary>
        /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A collection of enrollment-related payment records.</returns>
        [HttpGet("enrollment")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentPayment>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<EnrollmentPayment>>> GetEnrollmentPayments(CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentRepository.GetEnrollmentPayments(userId, ct);
                return Ok(result);
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }

        // GET: payments/enrollment/5
        /// <summary>
        /// Retrieves a specific enrollment payment detail by its unique identifier. This endpoint serves to verify the payment status of a particular activity registration, offering a breakdown of the transaction details for both the user and administrative staff.
        /// </summary>
        /// <param name="id">The unique identifier of the enrollment payment record.</param>
        /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
        /// <returns>The specific enrollment payment record if found.</returns>
        [HttpGet("enrollment/{id}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(EnrollmentPayment), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EnrollmentPayment>> GetEnrollmentPayment(uint id, CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentRepository.GetEnrollmentPayment(id, userId, ct);
                return result != null ? Ok(result) : NotFound();
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }

        // POST: payments/membership
        /// <summary>
        /// Initiates a new membership payment process. The PostMembershipPayment endpoint receives the PostMembershipPaymentDTO to trigger the creation of a payment intent. This usually involves communicating with an external payment gateway to generate a checkout URL, allowing the user to securely complete their membership purchase.
        /// </summary>
        /// <param name="dto">The data transfer object containing membership payment details.</param>
        /// <returns>A response containing the payment status and potential checkout URL.</returns>
        [HttpPost("membership")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PostPaymentResponse>> PostMembershipPayment(
            PostMembershipPaymentDTO dto
        )
        {
            try
            {
                var result = await _paymentRepository.CreateMembershipPayment(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }

        // POST: payments/activity
        /// <summary>
        /// Initiates a payment for a specific activity enrollment. The PostActivityPayment endpoint facilitates the financial registration for events by creating a payment request based on the provided DTO. It ensures that the authenticated user is the one making the request and coordinates with the repository layer to reserve the spot and handle the transaction initiation.
        /// </summary>
        /// <param name="dto">The data transfer object containing activity-specific payment details.</param>
        /// <returns>A response containing the payment status and instructions for completion.</returns>
        [HttpPost("activity")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PostPaymentResponse>> PostActivityPayment(
            PostActivityPaymentDTO dto
        )
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentRepository.CreateActivityPayment(dto, userId);
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

        // POST: payments/webhook
        /// <summary>
        /// Processes asynchronous status updates from the paymentservice payment gateway. The webhook endpoint is a secure entry point for external payment signals. It receives notifications regarding successful payments, cancellations, or failures, and triggers the IPaymentWebhookService to update the internal state of the corresponding transactions in real-time without requiring user intervention.
        /// </summary>
        /// <param name="id">The transaction identifier provided by the payment gateway.</param>
        /// <param name="webhookService">The service dedicated to handling external payment status webhooks.</param>
        /// <returns>An OK status once the webhook has been successfully processed.</returns>
        [HttpPost("webhook")]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> PaymentWebhook(
            [FromForm] string id,
            [FromServices] AbstractPaymentService webhookService
        )
        {
            try
            {
                await webhookService.HandleWebhookAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }

        // GET: payments/unpaid
        /// <summary>
        /// Retrieves a list of outstanding balances or unpaid enrollments. The GetUnpaid endpoint identifies members who have pending financial obligations. It can be filtered to show only the current user's debts or, for administrators, a comprehensive list of all unpaid activities across the organization, facilitating debt collection and financial planning.
        /// </summary>
        /// <param name="allUsers">A boolean flag indicating whether to fetch unpaid balances for all users (requires admin permissions).</param>
        /// <returns>A collection of enrollment balances with outstanding amounts.</returns>
        [HttpGet("unpaid")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentBalance>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<EnrollmentBalance>> GetUnpaid(bool allUsers = false)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = _paymentRepository.GetUnpaid(userId, allUsers);

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
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }

        // GET: payments/overpaid
        /// <summary>
        /// Retrieves a list of enrollment balances that have been overpaid. The GetOverpaid endpoint is designed for financial reconciliation, highlighting instances where users have paid more than the required amount. This allows administrators to manage refunds or apply credits to future activities, ensuring accurate accounting and member satisfaction.
        /// </summary>
        /// <returns>A collection of enrollment balances with credit amounts.</returns>
        [HttpGet("overpaid")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<EnrollmentBalance>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<EnrollmentBalance>> GetOverpaid()
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = _paymentRepository.GetOverpaid(userId);

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
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }

        // GET: payments/member/{userId}/status
        /// <summary>
        /// Retrieves the current payment and membership status for a specific member. The GetMemberPaymentStatus endpoint provides a high-level summary of a user's financial standing, including whether they are considered a "paid member" and if they have any critical outstanding debts. This is frequently used by other modules to determine eligibility for activity registration or access to certain system features.
        /// </summary>
        /// <param name="fromUserId">The unique identifier of the member whose status is being queried.</param>
        /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A status object reflecting the member's current financial standing.</returns>
        [HttpGet("member/{fromUserId}/status")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaymentStatusResponse>> GetMemberPaymentStatus(Guid fromUserId, CancellationToken ct)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var result = await _paymentRepository.GetMemberPaymentStatus(fromUserId, userId, ct);

                if(result == null)
                    return NotFound();

                return Ok(result);
            }
            catch(KeyNotFoundException)
            {
                return NotFound();
            }
            catch(UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }

        // GET: payments/export?startDate=2024-01-01&endDate=2024-12-31
        /// <summary>
        /// Exports all payment transactions within a specified date range to a CSV file. The ExportPaymentsToCsv endpoint is a powerful administrative tool for financial reporting and external auditing. It gathers all relevant transaction data between the start and end dates, formats it into a structured CSV file, and provides it as a download, enabling deep-dive analysis in spreadsheet software.
        /// </summary>
        /// <param name="startDate">The beginning of the date range for the export.</param>
        /// <param name="endDate">The end of the date range for the export.</param>
        /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A downloadable CSV file containing the payment transaction records.</returns>
        [HttpGet("export")]
        [Produces("text/csv")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Stream>> ExportPaymentsToCsv(DateTime startDate, DateTime endDate, CancellationToken ct)
        {            
            try
            {
                var userId = Guid.Parse(User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value);
                var (content, fileName) = await _paymentRepository.ExportPaymentsToCsv(startDate, endDate, userId, ct);
                return File(content, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
        }
    }
}
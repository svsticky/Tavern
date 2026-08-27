using Backend.Controllers.DTOs;
using Backend.Models.Domain;

namespace Backend.Interfaces
{
    /// <summary>
    /// Defines the contract for creating payments and querying payment status data.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Retrieves membership payments visible to the requesting user.
        /// </summary>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The membership payments.</returns>
        Task<List<MembershipPayment>> GetMembershipPayments(Guid userId, CancellationToken ct);

        /// <summary>
        /// Retrieves a single membership payment by ID.
        /// </summary>
        /// <param name="id">The payment ID.</param>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The payment when found; otherwise <c>null</c>.</returns>
        Task<MembershipPayment?> GetMembershipPayment(uint id, Guid userId, CancellationToken ct);

        /// <summary>
        /// Retrieves enrollment payments visible to the requesting user.
        /// </summary>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The enrollment payments.</returns>
        Task<List<EnrollmentPayment>> GetEnrollmentPayments(Guid userId, CancellationToken ct);

        /// <summary>
        /// Retrieves a single enrollment payment by ID.
        /// </summary>
        /// <param name="id">The payment ID.</param>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The payment when found; otherwise <c>null</c>.</returns>
        Task<EnrollmentPayment?> GetEnrollmentPayment(uint id, Guid userId, CancellationToken ct);

        /// <summary>
        /// Creates a membership payment. When <see cref="AbstractPaymentDTO.ManuallyMarkedAsPaid"/> is set, <paramref name="userId"/> must belong to a board or candidate board member.
        /// </summary>
        /// <param name="dto">The membership payment payload.</param>
        /// <param name="userId">The ID of the requesting user, if authenticated.</param>
        /// <returns>The created payment response.</returns>
        Task<PostPaymentResponse> CreateMembershipPayment(PostMembershipPaymentDTO dto, Guid? userId);

        /// <summary>
        /// Creates an activity payment.
        /// </summary>
        /// <param name="dto">The activity payment payload.</param>
        /// <param name="userId">The ID of the paying user.</param>
        /// <returns>The created payment response.</returns>
        Task<PostPaymentResponse> CreateActivityPayment(PostActivityPaymentDTO dto, Guid userId);

        /// <summary>
        /// Creates a "Begunstiger" (benefactor) fee payment. Only allowed when <paramref name="userId"/> is the begunstiger themselves, or belongs to a board or candidate board member acting on their behalf.
        /// </summary>
        /// <param name="dto">The begunstiger payment payload.</param>
        /// <param name="userId">The ID of the requesting user, if authenticated.</param>
        /// <returns>The created payment response.</returns>
        Task<PostPaymentResponse> CreateBegunstigerPayment(PostBegunstigerPaymentDTO dto, Guid? userId);

        /// <summary>
        /// Retrieves unpaid enrollment balances.
        /// </summary>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="allUsers">Whether unpaid balances for all users should be returned.</param>
        /// <returns>The unpaid enrollment balances.</returns>
        IEnumerable<EnrollmentBalance> GetUnpaid(Guid userId, bool allUsers = false);

        /// <summary>
        /// Retrieves overpaid enrollment balances for a user.
        /// </summary>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <returns>The overpaid enrollment balances.</returns>
        IEnumerable<EnrollmentBalance> GetOverpaid(Guid userId);

        /// <summary>
        /// Retrieves payment status for a member.
        /// </summary>
        /// <param name="fromUserId">The ID of the requesting user.</param>
        /// <param name="userId">The ID of the member to inspect.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The member payment status payload.</returns>
        Task<PaymentStatusResponse> GetMemberPaymentStatus(Guid fromUserId, Guid userId, CancellationToken ct);

        /// <summary>
        /// Exports payments in a date range to CSV.
        /// </summary>
        /// <param name="startDate">The inclusive start date.</param>
        /// <param name="endDate">The inclusive end date.</param>
        /// <param name="userId">The ID of the requesting user.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The CSV content and file name.</returns>
        Task<(byte[] Content, string FileName)> ExportPaymentsToCsv(DateTime startDate, DateTime endDate, Guid userId, CancellationToken ct);
    }
}

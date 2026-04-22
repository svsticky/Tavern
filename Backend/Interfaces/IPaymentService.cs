using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Mollie.Api.Client.Abstract;

namespace Backend.Interfaces
{
    public interface IPaymentService
    {
        Task<List<MembershipPayment>> GetMembershipPayments(Guid userId, CancellationToken ct);
        Task<MembershipPayment?> GetMembershipPayment(uint id, Guid userId, CancellationToken ct);

        Task<List<EnrollmentPayment>> GetEnrollmentPayments(Guid userId, CancellationToken ct);
        Task<EnrollmentPayment?> GetEnrollmentPayment(uint id, Guid userId, CancellationToken ct);

        Task<PostPaymentResponse> CreateMembershipPayment(PostMembershipPaymentDTO dto);
        Task<PostPaymentResponse> CreateActivityPayment(PostActivityPaymentDTO dto, Guid userId);
        IEnumerable<EnrollmentBalance> GetUnpaid(Guid userId, bool allUsers = false);
        IEnumerable<EnrollmentBalance> GetOverpaid(Guid userId);

        Task<object> GetMemberPaymentStatus(Guid fromUserId, Guid userId, CancellationToken ct);

        Task<(byte[] Content, string FileName)> ExportPaymentsToCsv(DateTime startDate, DateTime endDate, Guid userId, CancellationToken ct);
    }
}
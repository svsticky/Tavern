using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Mollie.Api.Client.Abstract;

namespace Backend.Interfaces
{
    public interface IPaymentService
    {
        Task<List<MembershipPayment>> GetMembershipPayments(CancellationToken ct);
        Task<MembershipPayment?> GetMembershipPayment(uint id, CancellationToken ct);

        Task<List<EnrollmentPayment>> GetEnrollmentPayments(CancellationToken ct);
        Task<EnrollmentPayment?> GetEnrollmentPayment(uint id, CancellationToken ct);

        Task<PostPaymentResponse> CreateMembershipPayment(PostMembershipPaymentDTO dto, IPaymentClient paymentClient);
        Task<PostPaymentResponse> CreateActivityPayment(PostActivityPaymentDTO dto, IPaymentClient paymentClient);
        IEnumerable<EnrollmentBalance> GetUnpaid(Guid userId, bool allUsers = false);
        IEnumerable<EnrollmentBalance> GetOverpaid();

        Task<object> GetMemberPaymentStatus(Guid memberId, CancellationToken ct);

        Task<(byte[] Content, string FileName)> ExportPaymentsToCsv(DateTime startDate, DateTime endDate, CancellationToken ct);
    }
}
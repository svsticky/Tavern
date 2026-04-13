using Backend.Models;

namespace Backend.Interfaces;

public interface IPaymentValidationService
{
    bool HasPaidMembershipPayment(Guid member);
    IEnumerable<EnrollmentBalance> GetUnpaidEnrollmentsForMember(Guid member);
    decimal GetUnpaidAmountForEnrollment(Enrollment enrollment);
    IEnumerable<EnrollmentBalance> GetAllUnpaidEnrollments();
    IEnumerable<EnrollmentBalance> GetAllOverpaidEnrollments();
    bool MemberHasPaidAllActivities(Member member);
}
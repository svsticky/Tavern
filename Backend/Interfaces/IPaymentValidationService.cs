using Backend.Models;

namespace Backend.Interfaces;

public interface IPaymentValidationService
{
    bool HasPaidMembershipPayment(Member member);
    IEnumerable<EnrollmentBalance> GetUnpaidEnrollmentsForMember(Member member);
    decimal GetUnpaidAmountForEnrollment(Enrollment enrollment);
    IEnumerable<EnrollmentBalance> GetAllUnpaidEnrollments();
    IEnumerable<EnrollmentBalance> GetAllOverpaidEnrollments();
    bool MemberHasPaidAllActivities(Member member);
}
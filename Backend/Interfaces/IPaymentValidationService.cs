using Backend.Models.Domain;

namespace Backend.Interfaces;

/// <summary>
/// Defines operations for validating membership and enrollment payment state.
/// </summary>
public interface IPaymentValidationService
{
    /// <summary>
    /// Checks whether a member has completed the required membership payment.
    /// </summary>
    /// <param name="member">The member user ID.</param>
    /// <returns><c>true</c> when the membership payment is completed; otherwise <c>false</c>.</returns>
    bool HasPaidMembershipPayment(Guid member);

    /// <summary>
    /// Retrieves unpaid enrollments for a member.
    /// </summary>
    /// <param name="member">The member user ID.</param>
    /// <returns>The unpaid enrollment balances.</returns>
    IEnumerable<EnrollmentBalance> GetUnpaidEnrollmentsForMember(Guid member);

    /// <summary>
    /// Calculates the unpaid amount for an enrollment.
    /// </summary>
    /// <param name="enrollment">The enrollment to evaluate.</param>
    /// <returns>The remaining unpaid amount.</returns>
    decimal GetUnpaidAmountForEnrollment(Enrollment enrollment);

    /// <summary>
    /// Retrieves all unpaid enrollments.
    /// </summary>
    /// <returns>The unpaid enrollment balances across members.</returns>
    IEnumerable<EnrollmentBalance> GetAllUnpaidEnrollments();

    /// <summary>
    /// Retrieves all overpaid enrollments.
    /// </summary>
    /// <returns>The overpaid enrollment balances across members.</returns>
    IEnumerable<EnrollmentBalance> GetAllOverpaidEnrollments();

    /// <summary>
    /// Checks whether a member has paid all required activity fees.
    /// </summary>
    /// <param name="member">The member to evaluate.</param>
    /// <returns><c>true</c> when all activity fees are paid; otherwise <c>false</c>.</returns>
    bool MemberHasPaidAllActivities(Member member);
}

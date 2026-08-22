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
    bool HasPaidMembershipPaymentBeforeExpirationTime(Guid member);

    /// <summary>
    /// Checks whether a member has completed the required membership payment, regardless of expiration time.
    /// This method is used for historical checks where we want to know if a member has ever paid, while the other method is used for current access checks where we want to ensure the payment is still valid.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <returns><c>true</c> when the membership payment is completed; otherwise <c>false</c>.</returns>
    bool HasEverPaidMembershipPayment(Guid memberId);

    /// <summary>
    /// Retrieves unpaid enrollments for a member.
    /// </summary>
    /// <param name="member">The member user ID.</param>
    /// <param name="includeNotOpenForPayment">Whether to include enrollments that are not open for payment.</param>
    /// <returns>The unpaid enrollment balances.</returns>
    IEnumerable<EnrollmentBalance> GetUnpaidEnrollmentsForMember(Guid member, bool includeNotOpenForPayment = false);

    /// <summary>
    /// Calculates the unpaid amount for an enrollment.
    /// </summary>
    /// <param name="enrollment">The enrollment to evaluate.</param>
    /// <param name="includeNotOpenForPayment">Whether to include enrollments that are not open for payment.</param>
    /// <returns>The remaining unpaid amount.</returns>
    decimal GetUnpaidAmountForEnrollment(Enrollment enrollment, bool includeNotOpenForPayment = false);

    /// <summary>
    /// Retrieves all unpaid enrollments.
    /// </summary>
    /// <param name="includeNotOpenForPayment">Whether to include enrollments that are not open for payment.</param>
    /// <returns>The unpaid enrollment balances across members.</returns>
    IEnumerable<EnrollmentBalance> GetAllUnpaidEnrollments(bool includeNotOpenForPayment = false);

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

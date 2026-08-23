using Backend.Models.Domain;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the abstract base class for payment DTOs, containing common properties for payment-related data transfer objects, including the member ID and a flag indicating whether the payment was manually marked as paid. The AbstractPaymentDTO serves as a foundation for specific payment DTOs, ensuring consistency and reusability of common payment-related properties across different payment operations within the application.
/// </summary>
public abstract class AbstractPaymentDTO
{
    /// <inheritdoc cref="Models.Domain.Payment.MemberId"/>
    public Guid MemberId { get; set; }

    /// <inheritdoc cref="Models.Domain.Payment.ManuallyMarkedAsPaid"/>
    public bool ManuallyMarkedAsPaid { get; set; } = false;
}

/// <summary>
/// Defines the DTO for posting a membership payment, containing the necessary information for creating a new membership payment, including the member ID and an optional flag indicating whether the payment was manually marked as paid. The PostMembershipPaymentDTO is used to transfer data from the client to the server when creating a new membership payment, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostMembershipPaymentDTO : AbstractPaymentDTO
{
}

/// <summary>
/// Defines the DTO for posting an activity payment, containing the necessary information for creating a new activity payment, including the member ID, an optional flag indicating whether the payment was manually marked as paid, and a list of unique identifiers of the activities for which the activity payment is being created. The PostActivityPaymentDTO is used to transfer data from the client to the server when creating a new activity payment, ensuring that all required information is provided and validated appropriately for the creation process, allowing for effective tracking and management of activity payments based on the provided activity IDs in the request payload.
/// </summary>
public class PostActivityPaymentDTO : AbstractPaymentDTO
{
    /// <summary>
    /// The list of unique identifiers of the activities for which the activity payment is being created. This field is required to associate the activity payment with the correct activities within the system, allowing for effective tracking and management of activity payments based on the provided activity IDs in the request payload. The PostActivityPaymentDTO ensures that the activity payment is created with the necessary information to effectively manage and track activity payments for the specified activities, providing a structured and validated approach to activity payment creation in the application.
    /// </summary>
    public List<uint> ActivityIds { get; set; } = new();
}

/// <summary>
/// Defines the response DTO for a payment, containing the necessary information about the payment response, including an optional checkout URL. The PostPaymentResponse is used to transfer data from the server to the client after creating a new payment, allowing for the inclusion of relevant information such as a checkout URL if applicable, providing a structured and informative response for payment-related operations in the application.
/// </summary>
public class PostPaymentResponse
{
    /// <inheritdoc cref="Models.Domain.Payment.PaymentIntentUrl"/>
    public string? CheckoutUrl { get; set; }
}

/// <summary>
/// Defines the response DTO for payment status, containing the necessary information about a member's payment status, including whether they have ever paid for membership, whether they currently have an active membership payment, whether they have paid for all activities, and a list of any unpaid enrollments. The PaymentStatusResponse is used to transfer data from the server to the client when retrieving a member's payment status, providing a comprehensive overview of their payment history and current payment status for both membership and activities within the application.
/// </summary>
public class PaymentStatusResponse
{
    /// <summary>
    /// The unique identifier of the member for whom the payment status is being retrieved. This field is essential for associating the payment status information with the correct member within the system, allowing for accurate tracking and management of payment statuses based on the provided member ID in the request. The PaymentStatusResponse ensures that the payment status information is correctly linked to the specified member, providing a structured and informative response for payment status retrieval in the application.
    /// </summary>
    public required Guid MemberId { get; set; }

    /// <summary>
    /// A boolean value indicating whether the member has ever paid for membership. This field provides insight into the member's payment history, allowing for the identification of members who have previously made membership payments, regardless of their current payment status. The PaymentStatusResponse includes this information to offer a comprehensive overview of the member's payment history and status within the application, enabling effective management and tracking of membership payments over time.
    /// </summary>
    public required bool HasEverPaidMembership { get; set; }

    /// <summary>
    /// A boolean value indicating whether the member has paid for membership before its expiration time. This field provides insight into the member's payment history, allowing for the identification of members who have made timely membership payments. The PaymentStatusResponse includes this information to offer a comprehensive overview of the member's payment status for membership within the application, enabling effective management and tracking of membership payments over time.
    /// </summary>
    public required bool HasPaidMembershipBeforeExpirationTime { get; set; }

    /// <summary>
    /// A boolean value indicating whether the member has paid for all activities. This field provides insight into the member's payment status for activities, allowing for the identification of members who have fulfilled their payment obligations for all enrolled activities. The PaymentStatusResponse includes this information to offer a comprehensive overview of the member's payment status for activities within the application, enabling effective management and tracking of activity payments and ensuring that members are aware of any outstanding payments for their enrolled activities.
    /// </summary>
    public required bool HasPaidAllActivities { get; set; }

    /// <summary>
    /// A list of unpaid enrollments for the member. This field provides detailed information about any enrollments for which the member has not yet made a payment, allowing for the identification of specific activities or memberships that require attention. The PaymentStatusResponse includes this information to offer a comprehensive overview of the member's outstanding payments, enabling effective management and tracking of unpaid enrollments and ensuring that members are aware of any pending payments for their enrolled activities or memberships within the application.
    /// </summary>
    public required IEnumerable<EnrollmentBalance> UnpaidEnrollments { get; set; }
}

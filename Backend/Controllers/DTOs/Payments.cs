namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a membership payment, containing the necessary information for creating a new membership payment, including the member ID. The PostMembershipPaymentDTO is used to transfer data from the client to the server when creating a new membership payment, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostMembershipPaymentDTO
{
    /// <inheritdoc cref="Models.Domain.Payment.MemberId"/>
    public Guid MemberId { get; set; }
}

/// <summary>
/// Defines the DTO for posting an activity payment, containing the necessary information for creating a new activity payment, including the member ID, a list of activity IDs, and an optional flag indicating whether the payment was manually marked as paid. The PostActivityPaymentDTO is used to transfer data from the client to the server when creating a new activity payment, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostActivityPaymentDTO
{
    /// <inheritdoc cref="Models.Domain.Payment.MemberId"/>
    public Guid MemberId { get; set; }

    /// <summary>
    /// The list of unique identifiers of the activities for which the activity payment is being created. This field is required to associate the activity payment with the correct activities within the system, allowing for effective tracking and management of activity payments based on the provided activity IDs in the request payload. The PostActivityPaymentDTO ensures that the activity payment is created with the necessary information to effectively manage and track activity payments for the specified activities, providing a structured and validated approach to activity payment creation in the application.
    /// </summary>
    public List<uint> ActivityIds { get; set; } = new();

    /// <inheritdoc cref="Models.Domain.Payment.ManuallyMarkedAsPaid"/>
    public bool ManuallyMarkedAsPaid { get; set; } = false;
}

/// <summary>
/// Defines the response DTO for a payment, containing the necessary information about the payment response, including an optional checkout URL. The PostPaymentResponse is used to transfer data from the server to the client after creating a new payment, allowing for the inclusion of relevant information such as a checkout URL if applicable, providing a structured and informative response for payment-related operations in the application.
/// </summary>
public class PostPaymentResponse
{
    /// <inheritdoc cref="Models.Domain.Payment.PaymentIntentUrl"/>
    public string? CheckoutUrl { get; set; }
}
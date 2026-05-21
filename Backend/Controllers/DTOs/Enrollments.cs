namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines Data Transfer Object (DTO) for posting enrollments, containing the necessary information for creating a new enrollment, including the activity ID, member ID, and any specification answers provided by the member. The PostEnrollmentDTO is used to transfer data from the client to the server when creating a new enrollment, ensuring that all required information is provided and validated appropriately for the enrollment creation process.
/// </summary>
public class PostEnrollmentDTO
{
    /// <inheritdoc cref="Models.Domain.Enrollment.ActivityId"/>
    public uint ActivityId { get; set; }
    
    /// <inheritdoc cref="Models.Domain.Enrollment.MemberId"/>
    public Guid MemberId { get; set; }

    /// <inheritdoc cref="Models.Domain.Enrollment.SpecificationAnswers"/>
    public List<PostSpecificationAnswerDTO>? SpecificationAnswers { get; set; }
}

/// <summary>
/// Represents the response DTO for an enrollment, containing all relevant information about the enrollment, including its properties and information about the associated member and activity. The EnrollmentResponseDTO is used to transfer comprehensive enrollment data from the server to the client when retrieving enrollment information, allowing for a complete representation of the enrollment's details, member information, activity information, and any specification answers provided by the member in the response payload.
/// </summary>
public class EnrollmentResponseDTO
{
    /// <inheritdoc cref="Models.Domain.Enrollment.IsOnWaitingList"/>
    public required bool IsOnWaitingList { get; set; }
    
    /// <inheritdoc cref="Models.Domain.Enrollment.Member"/>
    public required MemberResponseDTO Member { get; set; }

    /// <inheritdoc cref="Models.Domain.Enrollment.SpecificationAnswers"/>
    public List<SpecificationAnswerResponseDTO>? SpecificationAnswers { get; set; }

    /// <inheritdoc cref="Models.Domain.Enrollment.Price"/>
    public decimal? Price { get; set; }

    /// <inheritdoc cref="Models.Domain.Enrollment.Activity"/>
    public required ActivityResponseDTO Activity { get; set; }
}

/// <summary>
/// Defines the DTO for retrieving enrollments, containing the necessary information for filtering and retrieving enrollment data based on specific criteria. The GetEnrollmentsDTO is used to transfer data from the client to the server when retrieving enrollment information, allowing for the application of filters such as member ID to retrieve enrollments associated with a specific member, ensuring that the retrieved enrollment data is relevant and tailored to the client's needs.
/// </summary>
public class GetEnrollmentsDTO
{
    /// <summary>
    /// The unique identifier of the member for which to retrieve enrollments. This field is optional, and if provided, it allows for filtering enrollments based on the associated member, enabling the retrieval of enrollments specific to a particular member within the system. If not provided, enrollments for all members may be retrieved based on other criteria or without any member-specific filtering.
    /// </summary>
    public Guid? FromMemberId { get; set; }
}

/// <summary>
/// Represents the response DTO for a post enrollment operation, containing the unique identifiers of the activity and member associated with the newly created enrollment. The PostEnrollmentResponseDTO is used to transfer essential enrollment data from the server to the client after successfully creating a new enrollment, allowing for confirmation of the enrollment creation and providing key information about the associated activity and member in the response payload.
/// </summary>
public class PostEnrollmentResponseDTO
{
    /// <inheritdoc cref="Models.Domain.Enrollment.ActivityId"/>
    public uint ActivityId { get; set; }

    /// <inheritdoc cref="Models.Domain.Enrollment.MemberId"/>
    public Guid MemberId { get; set; }
}
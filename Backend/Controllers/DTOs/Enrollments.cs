using Backend.Models.Domain;
using System.Linq.Expressions;
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

    /// <inheritdoc cref="Models.Domain.Enrollment.RegisteredOn"/>
    public required DateTime RegisteredOn { get; set; }

    /// <inheritdoc cref="Models.Domain.Enrollment.Member"/>
    public required MemberResponseDTO? Member { get; set; }

    /// <inheritdoc cref="Models.Domain.Enrollment.SpecificationAnswers"/>
    public List<SpecificationAnswerResponseDTO>? SpecificationAnswers { get; set; }

    /// <inheritdoc cref="Models.Domain.Enrollment.Price"/>
    public decimal? Price { get; set; }

    /// <inheritdoc cref="Models.Domain.Enrollment.Activity"/>
    public required ActivityResponseDTO Activity { get; set; }

    /// <summary>
    /// Projects an Enrollment entity into an EnrollmentResponseDTO, including related member information, specification answers, and optionally the associated activity. The method takes a user ID, a boolean indicating whether the requester is a board member, and an optional boolean to include activity information, allowing it to conditionally include certain information based on the user's role and the context of the request. This projection is used to transform the data from the Enrollment model into a format that is suitable for API responses, ensuring that the relevant information is included while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to project the enrollment.</param>
    /// <param name="hasViewMembers">
    /// A boolean indicating whether the requester has the ViewMembers permission (or is a (candidate) board
    /// member, who always has it).
    /// </param>
    /// <param name="hasViewFinances">
    /// A boolean indicating whether the requester has the ViewFinances permission (or is a (candidate) board
    /// member, who always has it).
    /// </param>
    /// <param name="isBoardOrCandidateBoard">
    /// A boolean indicating whether the requester is a (candidate) board member. Forwarded into the nested
    /// member projection, where it gates the Notes field.
    /// </param>
    /// <param name="includeActivity">A boolean indicating whether to include activity information.</param>
    /// <returns>An expression that projects an Enrollment entity into an EnrollmentResponseDTO.</returns>
    public static Expression<Func<Enrollment, EnrollmentResponseDTO>> ToDto(Guid userId, bool hasViewMembers, bool hasViewFinances, bool isBoardOrCandidateBoard, bool includeActivity = true)
    {
        return e => new EnrollmentResponseDTO
        {
            IsOnWaitingList = e.IsOnWaitingList,
            RegisteredOn = e.RegisteredOn,
            Member = e.Member != null && (hasViewMembers || e.Member.Id == userId || (e.Activity != null && e.Activity.AreParticipantsVisible && e.Activity.DateTimeEnd >= DateTime.UtcNow))
                        ? MemberResponseDTO.ToDto(userId, hasViewMembers, isBoardOrCandidateBoard).Compile()(e.Member)
                        : null,
            SpecificationAnswers = e.SpecificationAnswers == null ? new List<SpecificationAnswerResponseDTO>() : e.SpecificationAnswers
                .Where(sa => hasViewMembers || sa.MemberId == userId || sa.Question.IsPublic && sa.Question.Activity.AreParticipantsVisible && sa.Question.Activity.DateTimeEnd >= DateTime.UtcNow)
                .Select(sa => SpecificationAnswerResponseDTO.ToDto().Compile()(sa)).ToList(),
            Price = hasViewFinances ? e.Price : null,
            Activity = e.Activity == null ? null! : includeActivity ? new ActivityResponseDTO
            {
                Id = e.Activity.Id,
                Name = e.Activity.Name,
                Price = e.Activity.Price,
                PosterPath = e.Activity.PosterPath,
                PosterFileName = e.Activity.PosterFileName,
                DutchDescription = e.Activity.DutchDescription,
                EnglishDescription = e.Activity.EnglishDescription,
                DateTimeStart = e.Activity.DateTimeStart,
                DateTimeEnd = e.Activity.DateTimeEnd,
                UnenrollmentDeadline = e.Activity.UnenrollmentDeadline,
                EnrollmentDeadline = e.Activity.EnrollmentDeadline,
                EnrollOpenDate = e.Activity.EnrollOpenDate,
                Location = e.Activity.Location,
                ParticipantLimit = e.Activity.ParticipantLimit,
                OrganizerId = e.Activity.OrganizerId,
                ShowInKoala = e.Activity.ShowInKoala,
                ShowOnWebsite = e.Activity.ShowOnWebsite,
                IsEnrollable = e.Activity.IsEnrollable,
                AreParticipantsVisible = e.Activity.AreParticipantsVisible,
                IsAdultOnly = e.Activity.IsAdultOnly,
                IsWeeklyDrinks = e.Activity.IsWeeklyDrinks,
                IsArchived = e.Activity.IsArchived,
                AllowedAudience = e.Activity.AllowedAudience,
                VatRate = e.Activity.VatRate,
                GLAccountId = e.Activity.GLAccountId,
                CostCenterId = e.Activity.CostCenterId,
                CostUnitId = e.Activity.CostUnitId,
                Enrollments = new List<EnrollmentResponseDTO>(),
                SpecificationQuestions = new List<GetSpecificationQuestionResponseDTO>(),
                PaymentDeadline = hasViewFinances ? e.Activity.PaymentDeadline : default,
                IsOpenForPayment = e.Activity.IsOpenForPayment
            } : null!
        };
    }
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

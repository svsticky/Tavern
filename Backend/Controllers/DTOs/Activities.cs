using System.Linq.Expressions;
using Backend.Models;
using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines Data Transfer Objects (DTOs) for activities, including the base DTO for creating and updating activities (BaseActivityDTO), as well as specific DTOs for posting new activities (PostActivityDTO), putting updates to existing activities (PutActivityDTO), and retrieving activity information (ActivityResponseDTO). These DTOs are used to facilitate the transfer of data between the client and server when managing activities within the system, ensuring that the necessary information is captured and validated appropriately for each operation related to activities.
/// </summary>
/// <typeparam name="TQuestion">The type of the specification questions for the activity.</typeparam>
public abstract class BaseActivityDTO<TQuestion>
{
    /// <inheritdoc cref="Activity.Name"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Activity.Price"/>
    [Column(TypeName = "numeric(18,2)")]
    public decimal Price { get; set; }

    /// <summary>
    /// The poster file for the activity, which can be uploaded when creating or updating an activity. The Poster property is of type IFormFile, allowing for the handling of file uploads in ASP.NET Core. This property is used to manage the visual representation of the activity, as the poster can be displayed on the website or in the Koala app to attract participants and provide information about the activity. When a poster is uploaded, it can be stored on the server and associated with the activity, enabling better organization and presentation of activities within the system.
    /// </summary>
    public IFormFile? Poster { get; set; }

    /// <inheritdoc cref="Activity.DutchDescription"/>
    [StringLength(2000)]
    public required string DutchDescription { get; set; }

    /// <inheritdoc cref="Activity.EnglishDescription"/>
    [StringLength(2000)]
    public required string EnglishDescription { get; set; }

    /// <inheritdoc cref="Activity.DateTimeStart"/>
    public required DateTimeOffset DateTimeStart { get; set; }

    /// <inheritdoc cref="Activity.DateTimeEnd"/>
    public required DateTimeOffset DateTimeEnd { get; set; }

    /// <inheritdoc cref="Activity.UnenrollmentDeadline"/>
    public DateTimeOffset? UnenrollmentDeadline { get; set; }

    /// <inheritdoc cref="Activity.EnrollmentDeadline"/>
    public DateTimeOffset? EnrollmentDeadline { get; set; }

    /// <inheritdoc cref="Activity.EnrollOpenDate"/>
    public DateTimeOffset? EnrollOpenDate { get; set; }

    /// <inheritdoc cref="Activity.Location"/>
    [StringLength(200)]
    public required string Location { get; set; }

    /// <inheritdoc cref="Activity.ParticipantLimit"/>
    public uint? ParticipantLimit { get; set; }

    /// <inheritdoc cref="Activity.OrganizerId"/>
    public uint? OrganizerId { get; set; }

    /// <summary>
    /// A JSON string representing the specification questions associated with the activity. This property is used to capture the details of the specification questions in a structured format, allowing for easy serialization and deserialization when creating or updating an activity. The JSON string can contain an array of specification question objects, each with its own properties such as question text, question type, and possible answers. This approach allows for flexibility in managing the specification questions while ensuring that they are properly associated with the activity in the system.
    /// </summary>
    public string? SpecificationQuestionsJson { get; set; }

    /// <inheritdoc cref="Activity.ShowInKoala"/>
    public required bool ShowInKoala { get; set; }

    /// <inheritdoc cref="Activity.ShowOnWebsite"/>
    public required bool ShowOnWebsite { get; set; }

    /// <inheritdoc cref="Activity.IsEnrollable"/>
    public required bool IsEnrollable { get; set; }

    /// <inheritdoc cref="Activity.AreParticipantsVisible"/>
    public required bool AreParticipantsVisible { get; set; }

    /// <inheritdoc cref="Activity.IsAdultOnly"/>
    public required bool IsAdultOnly { get; set; }

    /// <inheritdoc cref="Activity.IsWeeklyDrinks"/>
    public required bool IsWeeklyDrinks { get; set; } = false;

    /// <inheritdoc cref="Activity.AllowedAudience"/>
    public TargetAudience AllowedAudience { get; set; } = TargetAudience.All;

    /// <inheritdoc cref="Activity.VatRate"/>
    public uint? VatRate { get; set; }

    /// <inheritdoc cref="Activity.GLAccountId"/>
    public string? GLAccountId { get; set; }

    /// <inheritdoc cref="Activity.CostCenterId"/>
    public string? CostCenterId { get; set; }

    /// <inheritdoc cref="Activity.CostUnitId"/>
    public string? CostUnitId { get; set; }

    /// <inheritdoc cref="Activity.PaymentDeadline"/>
    public DateTimeOffset? PaymentDeadline { get; set; }
}

/// <summary>
/// Represents the response DTO for an activity, containing all relevant information about the activity, including its properties and related entities such as enrollments and specification questions. The ActivityResponseDTO is used to transfer comprehensive activity data from the server to the client when retrieving activity information, allowing for a complete representation of the activity's details, enrollment status, and associated specification questions in the response payload.
/// </summary>
public class ActivityResponseDTO
{
    /// <inheritdoc cref="Activity.Id"/>
    public required uint Id { get; set; }

    /// <inheritdoc cref="Activity.Name"/>
    public required string Name { get; set; }

    /// <inheritdoc cref="Activity.Price"/>
    public required decimal Price { get; set; }

    /// <inheritdoc cref="Activity.PosterPath"/>
    public string? PosterPath { get; set; }

    /// <inheritdoc cref="Activity.PosterFileName"/>
    public string? PosterFileName { get; set; }

    /// <inheritdoc cref="Activity.DutchDescription"/>
    public required string DutchDescription { get; set; }   

    /// <inheritdoc cref="Activity.EnglishDescription"/>
    public required string EnglishDescription { get; set; }

    /// <inheritdoc cref="Activity.DateTimeStart"/>
    public required DateTimeOffset DateTimeStart { get; set; }

    /// <inheritdoc cref="Activity.DateTimeEnd"/>
    public required DateTimeOffset DateTimeEnd { get; set; }

    /// <inheritdoc cref="Activity.UnenrollmentDeadline"/>
    public DateTimeOffset? UnenrollmentDeadline { get; set; }

    /// <inheritdoc cref="Activity.EnrollmentDeadline"/>
    public DateTimeOffset? EnrollmentDeadline { get; set; }

    /// <inheritdoc cref="Activity.EnrollOpenDate"/>
    public DateTimeOffset? EnrollOpenDate { get; set; }

    /// <inheritdoc cref="Activity.Location"/>
    public required string Location { get; set; }

    /// <inheritdoc cref="Activity.ParticipantLimit"/>
    public uint? ParticipantLimit { get; set; }

    /// <inheritdoc cref="Activity.OrganizerId"/>
    public uint? OrganizerId { get; set; }

    /// <inheritdoc cref="Activity.ShowInKoala"/>
    public required bool ShowInKoala { get; set; }

    /// <inheritdoc cref="Activity.ShowOnWebsite"/>
    public required bool ShowOnWebsite { get; set; }

    /// <inheritdoc cref="Activity.IsEnrollable"/>
    public required bool IsEnrollable { get; set; }

    /// <inheritdoc cref="Activity.AreParticipantsVisible"/>
    public required bool AreParticipantsVisible { get; set; }

    /// <inheritdoc cref="Activity.IsAdultOnly"/>
    public required bool IsAdultOnly { get; set; }

    /// <inheritdoc cref="Activity.AllowedAudience"/>
    public TargetAudience AllowedAudience { get; set; }

    /// <inheritdoc cref="Activity.IsWeeklyDrinks"/>
    public required bool IsWeeklyDrinks { get; set; }

    /// <inheritdoc cref="Activity.VatRate"/>
    public uint? VatRate { get; set; }

    /// <inheritdoc cref="Activity.GLAccountId"/>
    public string? GLAccountId { get; set; }

    /// <inheritdoc cref="Activity.CostCenterId"/>
    public string? CostCenterId { get; set; }

    /// <inheritdoc cref="Activity.CostUnitId"/>
    public string? CostUnitId { get; set; }
    
    /// <inheritdoc cref="Activity.Enrollments"/>
    public required List<EnrollmentResponseDTO> Enrollments { get; set; }

    /// <inheritdoc cref="Activity.SpecificationQuestions"/>
    public required List<GetSpecificationQuestionResponseDTO> SpecificationQuestions { get; set; }

    /// <inheritdoc cref="Activity.PaymentDeadline"/>
    public DateTimeOffset? PaymentDeadline { get; set; }

    /// <inheritdoc cref="Activity.IsOpenForPayment"/>
    public bool? IsOpenForPayment { get; set; }

    /// <summary>
        /// Projects an Activity entity into an ActivityResponseDTO, including related enrollments and specification questions. The method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information based on the user's role. This projection is used to transform the data from the Activity model into a format that is suitable for API responses, ensuring that the relevant information is included while maintaining appropriate access control based on the user's role within the system.
        /// </summary>
        /// <param name="userId">The ID of the user for whom to project the activity.</param>
        /// <param name="isBoard">A boolean indicating whether the requester is a board member.</param>
        /// <returns>An expression that projects an Activity entity into an ActivityResponseDTO.</returns>
        public static Expression<Func<Activity, ActivityResponseDTO>> ToDto(Guid userId, bool isBoard)
        {
            return a => new ActivityResponseDTO
            {
                Id = a.Id,
                Name = a.Name,
                Price = a.Price,
                PosterPath = a.PosterPath,
                PosterFileName = a.PosterFileName,
                DutchDescription = a.DutchDescription,
                EnglishDescription = a.EnglishDescription,
                DateTimeStart = a.DateTimeStart,
                DateTimeEnd = a.DateTimeEnd,
                UnenrollmentDeadline = a.UnenrollmentDeadline,
                EnrollmentDeadline = a.EnrollmentDeadline,
                EnrollOpenDate = a.EnrollOpenDate,
                Location = a.Location,
                ParticipantLimit = a.ParticipantLimit,
                OrganizerId = a.OrganizerId,
                ShowInKoala = a.ShowInKoala,
                ShowOnWebsite = a.ShowOnWebsite,
                IsEnrollable = a.IsEnrollable,
                AreParticipantsVisible = a.AreParticipantsVisible,
                IsAdultOnly = a.IsAdultOnly,
                IsWeeklyDrinks = a.IsWeeklyDrinks,
                AllowedAudience = a.AllowedAudience,
                VatRate = a.VatRate,
                GLAccountId = a.GLAccountId,
                CostCenterId = a.CostCenterId,
                CostUnitId = a.CostUnitId,

                Enrollments = a.AreParticipantsVisible || isBoard ? a.Enrollments.Select(e => EnrollmentResponseDTO.ToDto(userId, isBoard, false).Compile()(e)).ToList() : new List<EnrollmentResponseDTO>(),

                SpecificationQuestions = a.SpecificationQuestions.Select(q => GetSpecificationQuestionResponseDTO.ToDto().Compile()(q)).ToList(),

                PaymentDeadline = isBoard ? a.PaymentDeadline : default,
                IsOpenForPayment = a.IsOpenForPayment
            };
        }
}

/// <summary>
/// Represents the DTO for creating a new activity, containing all necessary information for defining an activity, including its properties and associated specification questions. The PostActivityDTO is used to transfer data from the client to the server when creating a new activity, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostActivityDTO : BaseActivityDTO<SpecificationQuestionDTO>
{
}

/// <summary>
/// Represents the DTO for updating an existing activity, containing all necessary information for modifying an activity's properties and associated specification questions. The PutActivityDTO is used to transfer data from the client to the server when updating an existing activity, allowing for changes to be made to the activity's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class PutActivityDTO : BaseActivityDTO<UpdateSpecificationQuestionDTO>
{
}

/// <summary>
/// Represents the DTO for retrieving activity information, containing all relevant details about the activity, including its properties and associated specification questions. The ActivityResponseDTO is used to transfer comprehensive activity data from the server to the client when retrieving activity information, allowing for a complete representation of the activity's details, enrollment status, and associated specification questions in the response payload.
/// </summary>
public class GetActivitiesDTO
{
    /// <summary>
    /// Indicates whether to include past activities in the response. If set to true, activities that have already occurred will be included in the response; if set to false, only upcoming activities will be included.
    /// </summary>
    public bool IncludePast { get; set; } = false;

    /// <summary>
    /// Indicates whether to include future activities in the response. If set to true, activities that are scheduled for the future will be included in the response; if set to false, only past activities will be included.
    /// </summary>
    public bool IncludeFuture { get; set; } = true;

    /// <summary>
    /// Indicates the year for which to retrieve activities. If specified, only activities that are associated with the given year will be included in the response.
    /// </summary>
    public uint? Year { get; set; }

    /// <summary>
    /// Indicates whether to include activities that are open for payment in the response. If set to true, activities that are currently open for payment will be included in the response; if set to false, only activities that are not open for payment will be included.
    /// </summary>
    public bool? OpenForPayment { get; set; }
}
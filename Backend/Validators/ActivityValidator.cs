using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Newtonsoft.Json;

namespace Backend.Validators;

/// <summary>
/// The ActivityValidator class provides static methods for validating and normalizing activity-related data transfer objects (DTOs) used in the creation and updating of activities. The ValidateRequest method checks the validity of the activity's time range, participant limit, and poster file if provided, as well as ensuring that only authorized users can create activities with certain features. The NormalizeCreateRequest method adjusts the DTO for creating an activity by setting the enrollment open date to null if the activity is marked as enrollable. The ParseCreateQuestions and ParseUpdateQuestions methods handle the deserialization of specification questions from JSON format, while the MapSpecificationQuestion method maps the properties of an UpdateSpecificationQuestionDTO to a SpecificationQuestion entity. These methods centralize the validation and normalization logic for activity-related operations, ensuring that incoming data is properly checked and formatted before being processed by the application.
/// </summary>
public static class ActivityValidator
{
    /// <summary>
    /// Validates the properties of a BaseActivityDTO object, ensuring that the time range is valid, the participant limit is non-negative, and the poster file (if provided) has an acceptable format. Additionally, this method checks that only board members can create activities with certain features such as being shown in Koala/website or having enrollment/payment options, to prevent abuse of these features. The validation process should throw appropriate exceptions if any of the validation checks fail, providing clear feedback on the nature of the validation errors encountered.
    /// </summary>
    /// <typeparam name="TQuestion">The type activity DTO.</typeparam>
    /// <param name="dto">The activity DTO to validate.</param>
    /// <param name="userId">The ID of the user creating the activity.</param>
    /// <param name="permissionService">The permission service for checking user permissions.</param>
    public static void ValidateRequest<TQuestion>(BaseActivityDTO<TQuestion> dto, Guid userId, IPermissionService permissionService)
    {
        ValidateTimeRange(dto.DateTimeStart, dto.DateTimeEnd);
        ValidateDeadlines(dto.DateTimeEnd, dto.EnrollmentDeadline, dto.UnenrollmentDeadline);
        ValidatePosterIfProvided(dto.Poster);

        bool hasEditForGroup = dto.OrganizerId.HasValue && permissionService.HasPermission(userId, Permission.EditActivityForGroup, dto.OrganizerId.Value);

        // Only board members (or EditAllActivities holders) can create activities that are shown in
        // Koala/website or have enrollment/payment options, to prevent abuse of these features.
        // Otherwise, creating an activity requires EditActivityForGroup for the organizing group
        // (or EditAllActivities/board), mirroring the same permissions PatchActivity/UpdateActivity use.
        if (dto.ShowInKoala
                || dto.ShowOnWebsite
                || dto.PaymentDeadline != null
                || dto.EnrollOpenDate != null
                || !hasEditForGroup
            )
            permissionService.EnsurePermission(userId, Permission.EditAllActivities);
    }

    /// <summary>
    /// Normalizes the properties of a PostActivityDTO object for activity creation. If the activity is marked as enrollable, this method sets the enrollment open date to null, as it will be automatically determined based on the activity's start date. This normalization process ensures that the DTO is properly adjusted for the creation of an activity, allowing for consistent handling of enrollable activities within the system.
    /// </summary>
    /// <param name="dto">The activity DTO to normalize.</param>
    public static void NormalizeCreateRequest(PostActivityDTO dto)
    {
        if (dto.IsEnrollable)
        {
            dto.EnrollOpenDate = null;
        }
    }

    /// <summary>
    /// Parses a JSON string containing a list of specification questions into a list of SpecificationQuestionDTO objects. This method checks if the input JSON string is null or empty, and if so, it returns an empty list. Otherwise, it attempts to deserialize the JSON string into a list of SpecificationQuestionDTO objects using the JsonConvert.DeserializeObject method. If the deserialization process fails or results in a null value, the method throws an ArgumentException indicating that the specification questions format is invalid. This parsing process allows for the conversion of specification question data from JSON format into a structured format that can be used within the application for activity creation and updating operations.
    /// </summary>
    /// <param name="specificationQuestionsJson">The JSON string containing the specification questions.</param>
    /// <returns>The list of specification questions.</returns>
    /// <exception cref="ArgumentException">Thrown when the specification questions format is invalid.</exception>
    public static List<SpecificationQuestionDTO> ParseCreateQuestions(string? specificationQuestionsJson)
    {
        var questions = string.IsNullOrEmpty(specificationQuestionsJson)
            ? new List<SpecificationQuestionDTO>()
            : JsonConvert.DeserializeObject<List<SpecificationQuestionDTO>>(specificationQuestionsJson);

        return questions ?? throw new ArgumentException("Invalid specification questions format.");
    }

    /// <summary>
    /// Parses a JSON string containing a list of specification questions into a list of UpdateSpecificationQuestionDTO objects. This method checks if the input JSON string is null or empty, and if so, it returns an empty list. Otherwise, it attempts to deserialize the JSON string into a list of UpdateSpecificationQuestionDTO objects using the JsonConvert.DeserializeObject method. If the deserialization process fails or results in a null value, the method throws an ArgumentException indicating that the specification questions format is invalid. This parsing process allows for the conversion of specification question data from JSON format into a structured format that can be used within the application for activity updating operations.
    /// </summary>
    /// <param name="specificationQuestionsJson">The JSON string containing the specification questions.</param>
    /// <returns>The list of specification questions.</returns>
    /// <exception cref="ArgumentException">Thrown when the specification questions format is invalid.</exception>
    public static List<UpdateSpecificationQuestionDTO> ParseUpdateQuestions(string? specificationQuestionsJson)
    {
        var questions = string.IsNullOrEmpty(specificationQuestionsJson)
            ? new List<UpdateSpecificationQuestionDTO>()
            : JsonConvert.DeserializeObject<List<UpdateSpecificationQuestionDTO>>(specificationQuestionsJson);

        return questions ?? throw new ArgumentException("Invalid specification questions format.");
    }

    /// <summary>
    /// Maps the properties of an UpdateSpecificationQuestionDTO object to a SpecificationQuestion entity. This method takes an existing SpecificationQuestion entity and an UpdateSpecificationQuestionDTO object as parameters, and it updates the properties of the entity based on the values provided in the DTO. The mapping process includes updating the question text in both Dutch and English, the type of the question, whether it is mandatory or public, and the options for the question if applicable. This method centralizes the logic for mapping specification question data from a DTO to an entity, ensuring that updates to specification questions are handled consistently within the application.
    /// </summary>
    /// <param name="entity">The SpecificationQuestion entity to update.</param>
    /// <param name="dto">The UpdateSpecificationQuestionDTO object containing the updated values.</param>
    public static void MapSpecificationQuestion(SpecificationQuestion entity, UpdateSpecificationQuestionDTO dto)
    {
        entity.QuestionDutch = dto.QuestionDutch;
        entity.QuestionEnglish = dto.QuestionEnglish;
        entity.Type = dto.Type;
        entity.IsMandatory = dto.IsMandatory;
        entity.IsPublic = dto.IsPublic;
        entity.Options = dto.Options != null && dto.Options.Any()
            ? string.Join(';', dto.Options)
            : null;
    }

    /// <summary>
    /// Validates that the end date and time of an activity lies strictly after the start date and time. If the end date and time is before or equal to the start date and time, this method throws an ArgumentException indicating that the activity cannot end before it starts. Requiring a strictly positive duration also keeps zero-length activities out of exported calendar feeds, where an event without a duration cannot be rendered meaningfully. This validation ensures that the time range specified for an activity is logical and prevents the creation of activities with invalid time ranges within the system.
    /// </summary>
    /// <param name="start">The start date and time of the activity.</param>
    /// <param name="end">The end date and time of the activity.</param>
    /// <exception cref="ArgumentException">Thrown when the end date and time is not after the start date and time.</exception>
    public static void ValidateTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new ArgumentException("Activity cannot end before it starts.");
    }

    /// <summary>
    /// Validates that the enrollment and unenrollment deadlines, if provided, are not after the end date and time of the activity. If either deadline is after the activity's end date and time, this method throws an ArgumentException. This validation ensures that participants cannot (un)enroll after the activity has already ended.
    /// </summary>
    /// <param name="end">The end date and time of the activity.</param>
    /// <param name="enrollmentDeadline">The enrollment deadline of the activity, if any.</param>
    /// <param name="unenrollmentDeadline">The unenrollment deadline of the activity, if any.</param>
    /// <exception cref="ArgumentException">Thrown when a deadline is after the activity's end date and time.</exception>
    public static void ValidateDeadlines(DateTimeOffset end, DateTimeOffset? enrollmentDeadline, DateTimeOffset? unenrollmentDeadline)
    {
        if (enrollmentDeadline > end)
            throw new ArgumentException("Enrollment deadline cannot be after the activity ends.");

        if (unenrollmentDeadline > end)
            throw new ArgumentException("Unenrollment deadline cannot be after the activity ends.");
    }



    /// <summary>
    /// Validates the poster file provided for an activity, ensuring that if a poster file is provided, it has an acceptable format. This method checks if the poster file is not null, and if so, it calls the ExtensionValidator.ValidatePosterExtension method to validate the file's extension. This validation helps to ensure that only valid poster files are accepted for activities within the system, maintaining data integrity and security.
    /// </summary>
    /// <param name="poster">The poster file for the activity.</param>
    private static void ValidatePosterIfProvided(IFormFile? poster)
    {
        if (poster != null)
        {
            ExtensionValidator.ValidatePosterExtension(poster);
        }
    }
}

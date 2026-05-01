using Backend.Models.Domain;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a study enrollment, containing the necessary information for creating a new study enrollment, including the study ID, member ID, enrollment date, and status. The PostStudyEnrollmentDTO is used to transfer data from the client to the server when creating a new study enrollment, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostStudyEnrollmentDTO
{
    /// <inheritdoc cref="Models.StudyEnrollment.StudyId"/>
    public required uint StudyId { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.MemberId"/>
    public required Guid MemberId { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.EnrollmentDate"/>
    public required DateTimeOffset EnrollmentDate { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.Status"/>
    public StudyStatus Status { get; set; } = StudyStatus.Enrolled;
}

/// <summary>
/// Represents the response DTO for a study enrollment, containing all relevant information about the study enrollment, including its properties and any associated data. The StudyEnrollmentResponseDTO is used to transfer comprehensive study enrollment data from the server to the client when retrieving study enrollment information, allowing for a complete representation of the study enrollment's details in the response payload. The StudyEnrollmentResponseDTO can include properties such as the enrollment ID, member ID, member name, study ID, study title, study type, enrollment date, completion date, and status, providing a comprehensive view of the study enrollment data for the client application.
/// </summary>
public class StudyEnrollmentResponseDTO
{
    /// <inheritdoc cref="Models.StudyEnrollment.Id"/>
    public required uint Id { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.MemberId"/>
    public Guid? MemberId { get; set; }

    /// <inheritdoc cref="Models.Member"/>
    public string? MemberName { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.StudyId"/>
    public uint? StudyId { get; set; }

    /// <inheritdoc cref="Models.Study"/>
    public string? StudyTitle { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.Study"/>
    public StudyType? StudyType { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.EnrollmentDate"/>
    public required DateTimeOffset EnrollmentDate { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.CompletionDate"/>
    public DateTimeOffset? CompletionDate { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.Status"/>
    public required StudyStatus Status { get; set; }
}

/// <summary>
/// Defines the DTO for retrieving study enrollments, containing all necessary information for filtering and retrieving study enrollment data. The GetStudyEnrollmentsDTO is used to transfer data from the client to the server when retrieving study enrollment information, allowing for filtering based on member ID and ensuring that the provided information is validated appropriately for the retrieval process.
/// </summary>
public class GetStudyEnrollmentsDTO
{
    /// <inheritdoc cref="Models.StudyEnrollment.MemberId"/>
    public Guid? MemberId { get; set; }
}
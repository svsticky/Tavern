using Backend.Models;

namespace Backend.Controllers.DTOs;

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

public class StudyEnrollmentResponseDTO
{
    /// <inheritdoc cref="Models.StudyEnrollment.Id"/>
    public uint Id { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.MemberId"/>
    public Guid? MemberId { get; set; }

    /// <inheritdoc cref="Models.Member"/>
    public string? MemberName { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.StudyId"/>
    public uint? StudyId { get; set; }

    /// <inheritdoc cref="Models.Study"/>
    public string? StudyTitle { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.EnrollmentDate"/>
    public DateTimeOffset EnrollmentDate { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.CompletionDate"/>
    public DateTimeOffset? CompletionDate { get; set; }

    /// <inheritdoc cref="Models.StudyEnrollment.Status"/>
    public StudyStatus Status { get; set; }
}
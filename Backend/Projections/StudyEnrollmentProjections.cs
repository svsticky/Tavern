using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

/// <summary>
/// The StudyEnrollmentProjections class provides a method to project a StudyEnrollment entity into a StudyEnrollmentResponseDTO. This projection is used to transform the data from the StudyEnrollment model into a format that is suitable for API responses, ensuring that the relevant information about the study enrollment is included while maintaining appropriate access control based on the user's role within the system. The ToDto method centralizes the logic for mapping StudyEnrollment entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling study enrollment-related data transformations for API responses.
/// </summary>
public static class StudyEnrollmentProjections
{
    /// <summary>
    /// Projects a StudyEnrollment entity into a StudyEnrollmentResponseDTO, including related member and study information, enrollment and completion dates, study type, and enrollment status. This projection is used to transform the data from the StudyEnrollment model into a format that is suitable for API responses, ensuring that the relevant information about the study enrollment is included while maintaining appropriate access control based on the user's role within the system. The ToDto method centralizes the logic for mapping StudyEnrollment entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling study enrollment-related data transformations for API responses.
    /// </summary>
    /// <returns>An expression that projects a StudyEnrollment entity into a StudyEnrollmentResponseDTO.</returns>
    public static Expression<Func<StudyEnrollment, StudyEnrollmentResponseDTO>> ToDto()
    {
        return se => new StudyEnrollmentResponseDTO
        {
            Id = se.Id,
            MemberId = se.MemberId,
            MemberName = se.Member.FirstName + " " + se.Member.LastName,
            StudyId = se.StudyId,
            StudyTitle = se.Study.Title,
            EnrollmentDate = se.EnrollmentDate,
            CompletionDate = se.CompletionDate,
            StudyType = se.Study.Type,
            Status = se.Status
        };
    }
}

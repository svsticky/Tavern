using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class StudyEnrollmentProjections
{
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
            Status = se.Status
        };
    }
}

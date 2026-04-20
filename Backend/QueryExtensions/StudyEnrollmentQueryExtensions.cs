using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

public static class StudyEnrollmentQueryExtensions
{
    public static IQueryable<StudyEnrollment> IncludeDetails(this IQueryable<StudyEnrollment> query)
    {
        return query
            .Include(se => se.Member)
            .Include(se => se.Study);
    }

    public static IQueryable<StudyEnrollment> Filter(this IQueryable<StudyEnrollment> query, GetStudyEnrollmentsDTO dto)
    {
        if (dto.MemberId != null)
        {
            query = query.Where(se => se.MemberId == dto.MemberId);
        }

        return query;
    }
}

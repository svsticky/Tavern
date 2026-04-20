using Backend.Controllers.DTOs;
using Backend.Models.Domain;

public static class EnrollmentQueryExtensions
{
    public static IQueryable<Enrollment> Filter(
        this IQueryable<Enrollment> query, 
        GetEnrollmentsDTO dto)
    {
        if (dto.FromMemberId != null)
        {
            query = query.Where(e => e.MemberId == dto.FromMemberId);
        }

        return query;
    }
}

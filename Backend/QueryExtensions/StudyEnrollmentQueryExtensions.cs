using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.QueryExtensions;

/// <summary>
/// The StudyEnrollmentQueryExtensions class provides extension methods for filtering and including details in a queryable collection of StudyEnrollment entities based on various criteria specified in a GetStudyEnrollmentsDTO object. The IncludeDetails method allows for including related member and study information in the query results, while the Filter method enables dynamic filtering of study enrollments based on the member ID specified in the DTO. These methods centralize the logic for applying these filters and includes to an <see cref="IQueryable{StudyEnrollment}"/>, ensuring that the relevant study enrollments are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
/// </summary>
public static class StudyEnrollmentQueryExtensions
{
    /// <summary>
    /// Includes the details of the StudyEnrollment entity, such as the associated Member and Study.
    /// </summary>
    /// <param name="query">The queryable collection of StudyEnrollment entities.</param>
    /// <returns>The queryable collection of StudyEnrollment entities with included details.</returns>
    public static IQueryable<StudyEnrollment> IncludeDetails(this IQueryable<StudyEnrollment> query)
    {
        return query
            .Include(se => se.Member)
            .Include(se => se.Study);
    }

    /// <summary>
    /// Filters a queryable collection of StudyEnrollment entities based on criteria specified in a GetStudyEnrollmentsDTO object. This method allows for dynamic filtering of study enrollments based on the member ID specified in the DTO, enabling retrieval of study enrollments associated with a particular member. The Filter method centralizes the logic for applying this filter to an <see cref="IQueryable{StudyEnrollment}"/>, ensuring that the relevant study enrollments are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="query">The queryable collection of StudyEnrollment entities to filter.</param>
    /// <param name="dto">The data transfer object containing the filtering criteria.</param>
    /// <returns>The filtered queryable collection of StudyEnrollment entities.</returns>
    public static IQueryable<StudyEnrollment> Filter(this IQueryable<StudyEnrollment> query, GetStudyEnrollmentsDTO dto)
    {
        if (dto.MemberId != null)
        {
            query = query.Where(se => se.MemberId == dto.MemberId);
        }

        return query;
    }
}

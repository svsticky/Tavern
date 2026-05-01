using Backend.Controllers.DTOs;
using Backend.Models.Domain;

/// <summary>
/// The EnrollmentQueryExtensions class provides an extension method for filtering a queryable collection of Enrollment entities based on criteria specified in a GetEnrollmentsDTO object. This method allows for dynamic filtering of enrollments based on the member ID specified in the DTO, enabling retrieval of enrollments associated with a particular member. The Filter method centralizes the logic for applying this filter to an IQueryable<Enrollment>, ensuring that the relevant enrollments are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
/// </summary>
public static class EnrollmentQueryExtensions
{
    /// <summary>
    /// Filters a queryable collection of Enrollment entities based on criteria specified in a GetEnrollmentsDTO object. This method allows for dynamic filtering of enrollments based on the member ID specified in the DTO, enabling retrieval of enrollments associated with a particular member. The Filter method centralizes the logic for applying this filter to an IQueryable<Enrollment>, ensuring that the relevant enrollments are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="query">The queryable collection of Enrollment entities to filter.</param>
    /// <param name="dto">The data transfer object containing the filtering criteria.</param>
    /// <returns>The filtered queryable collection of Enrollment entities.</returns>
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

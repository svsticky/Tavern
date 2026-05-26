using Backend.Controllers.DTOs;
using Backend.Models.Domain;

namespace Backend.QueryExtensions;

/// <summary>
/// The MemberQueryExtensions class provides extension methods for filtering and applying paging to a queryable collection of Member entities based on various criteria specified in a GetMembersDto object. The Filter method allows for dynamic filtering of members based on search terms, study enrollments, and specific member attributes such as gratie, lid van verdienste, ere lid, begunstiger, suspended, inactive status, and study type. The ApplyPaging method enables pagination of the results based on the page number and page size specified in the DTO. These methods centralize the logic for applying these filters and pagination to an <see cref="IQueryable{Member}"/>, ensuring that the relevant members are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
/// </summary>
public static class MemberQueryExtensions
{
    /// <summary>
    /// Filters a queryable collection of Member entities based on various criteria specified in a GetMembersDto object. This method allows for dynamic filtering of members based on search terms, study enrollments, and specific member attributes such as gratie, lid van verdienste, ere lid, begunstiger, suspended, inactive status, and study type. The Filter method centralizes the logic for applying these filters to an <see cref="IQueryable{Member}"/>, ensuring that the relevant members are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="query">The queryable collection of Member entities to filter.</param>
    /// <param name="dto">The data transfer object containing the filtering criteria.</param>
    /// <returns>The filtered queryable collection of Member entities.</returns>
    public static IQueryable<Member> Filter(
        this IQueryable<Member> query, 
        GetMembersDto dto)
    {
        var now = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(dto.Search))
        {
            var search = dto.Search.Trim().ToLower();

            query = query.Where(m => 
                (m.FirstName + " " + m.LastName).ToLower().Contains(search) ||
                m.Email.ToLower().Contains(search) ||
                m.StudentNumber.ToString().Contains(search) ||
                m.PhoneNumber.Contains(search)
            );
        }

        if (dto.StudyId.HasValue)
        {
            query = query.Where(m => m.StudyEnrollments.Any(se => se.StudyId == dto.StudyId.Value));
        }

        if(dto.Gratie.HasValue)
        {
            query = query.Where(m => m.Gratie == dto.Gratie.Value);
        }

        if(dto.LidVanVerdienste.HasValue)
        {
            query = query.Where(m => m.LidVanVerdienste == dto.LidVanVerdienste.Value);
        }

        if(dto.EreLid.HasValue)
        {
            query = query.Where(m => m.EreLid == dto.EreLid.Value);
        }

        if(dto.Begunstiger.HasValue)
        {
            query = query.Where(m => m.Begunstiger == dto.Begunstiger.Value);
        }

        if(dto.Suspended.HasValue)
        {
            query = query.Where(m => m.Suspended == dto.Suspended.Value);
        }

        if(dto.Inactive.HasValue)
        {
            query = dto.Inactive.Value 
                ? query.Where(m => m.StudyEnrollments.All(se => se.Status == StudyStatus.Completed || se.Status == StudyStatus.DroppedOut)) 
                : query.Where(m => m.StudyEnrollments.Any(se => se.Status == StudyStatus.Enrolled));
        }

        if(dto.StudyType.HasValue)
        {
            query = query.Where(m => m.StudyEnrollments.Any(se => se.Study.Type == dto.StudyType.Value));
        }

        return query;
    }

    /// <summary>
    /// Applies pagination to a queryable collection of Member entities based on the page number and page size specified in a GetMembersDto object. The ApplyPaging method calculates the number of records to skip based on the current page and page size, and then takes the specified number of records for the current page. This method centralizes the logic for applying pagination to an <see cref="IQueryable{Member}"/>, ensuring that the results are returned in manageable chunks based on the client's request while maintaining efficient querying of the underlying data source.
    /// </summary>
    /// <param name="query">The queryable collection of Member entities to paginate.</param>
    /// <param name="dto">The data transfer object containing pagination settings.</param>
    /// <returns>The paginated queryable collection of Member entities.</returns>
    public static IQueryable<Member> ApplyPaging(
        this IQueryable<Member> query,
        GetMembersDto dto)
    {
        int pageSize = dto.PageSize > 0 ? dto.PageSize : 50;
        int skip = (dto.Page > 0 ? dto.Page - 1 : 0) * pageSize;

        return query
            .Skip(skip)
            .Take(pageSize);
    }
}

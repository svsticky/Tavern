using Backend.Controllers.DTOs;
using Backend.Models.Domain;

/// <summary>
/// The ActivityQueryExtensions class provides an extension method for filtering a queryable collection of Activity entities based on various criteria specified in a GetActivitiesDTO object. This method allows for dynamic filtering of activities based on whether the requester is a board member, the user's group memberships, and specific filters such as including past or future activities, filtering by year, and checking if activities are open for payment. The Filter method centralizes the logic for applying these filters to an IQueryable<Activity>, ensuring that the relevant activities are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
/// </summary>
public static class ActivityQueryExtensions
{
    /// <summary>
    /// Filters a queryable collection of Activity entities based on various criteria specified in a GetActivitiesDTO object. This method allows for dynamic filtering of activities based on whether the requester is a board member, the user's group memberships, and specific filters such as including past or future activities, filtering by year, and checking if activities are open for payment. The Filter method centralizes the logic for applying these filters to an IQueryable<Activity>, ensuring that the relevant activities are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="query">The queryable collection of Activity entities to filter.</param>
    /// <param name="dto">The data transfer object containing the filtering criteria.</param>
    /// <param name="isBoard">Indicates whether the requester is a board member.</param>
    /// <param name="userGroupIds">The IDs of the groups to which the user belongs.</param>
    /// <returns>The filtered queryable collection of Activity entities.</returns>
    public static IQueryable<Activity> Filter(
        this IQueryable<Activity> query, 
        GetActivitiesDTO dto, 
        bool isBoard, 
        IEnumerable<uint> userGroupIds)
    {
        var now = DateTimeOffset.UtcNow;

        query = query.Where(a => isBoard 
                                 || a.ShowInKoala 
                                 || (a.OrganizerId != null && userGroupIds.Contains(a.OrganizerId.Value) && !a.ShowOnWebsite && !a.ShowInKoala && !a.EnrollOpenDate.HasValue));

        if (!dto.IncludePast)
            query = query.Where(a => a.DateTimeEnd > now);

        if (!dto.IncludeFuture)
            query = query.Where(a => a.DateTimeStart < now);

        if (dto.Year.HasValue)
            query = query.Where(a => a.DateTimeStart.Year == (int)dto.Year.Value);

        if (dto.OpenForPayment.HasValue)
            query = query.Where(a => a.IsOpenForPayment == dto.OpenForPayment.Value);

        return query;
    }
}
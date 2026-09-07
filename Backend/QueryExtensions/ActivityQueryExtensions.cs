using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using System.Globalization;

namespace Backend.QueryExtensions;

/// <summary>
/// The ActivityQueryExtensions class provides an extension method for filtering a queryable collection of Activity entities based on various criteria specified in a GetActivitiesDTO object. This method allows for dynamic filtering of activities based on whether the requester is a board member, the user's group memberships, and specific filters such as including past or future activities, filtering by year, and checking if activities are open for payment. The Filter method centralizes the logic for applying these filters to an <see cref="IQueryable{Activity}"/>, ensuring that the relevant activities are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
/// </summary>
public static class ActivityQueryExtensions
{
    /// <summary>
    /// Filters a queryable collection of Activity entities based on various criteria specified in a GetActivitiesDTO object. This method allows for dynamic filtering of activities based on whether the requester is a board member, the user's group memberships, and specific filters such as including past or future activities, filtering by year, and checking if activities are open for payment. The Filter method centralizes the logic for applying these filters to an <see cref="IQueryable{Activity}"/>, ensuring that the relevant activities are returned based on the specified criteria while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="query">The queryable collection of Activity entities to filter.</param>
    /// <param name="dto">The data transfer object containing the filtering criteria.</param>
    /// <param name="isBoard">Indicates whether the requester is a board member.</param>
    /// <param name="userGroupIds">The IDs of the groups to which the user belongs.</param>
    /// <param name="isLoggedIn">Indicates whether the user is logged in.</param>
    /// <returns>The filtered queryable collection of Activity entities.</returns>
    public static IQueryable<Activity> Filter(
        this IQueryable<Activity> query,
        GetActivitiesDTO dto,
        bool isBoard,
        IEnumerable<uint> userGroupIds,
        bool isLoggedIn)
    {
        var now = DateTimeOffset.UtcNow;

        if (!isLoggedIn)
        {
            query = query.Where(a => a.ShowOnWebsite && a.DateTimeEnd >= now && !a.IsArchived);

            return query;
        }

        if (dto.IsArchived.HasValue)
            query = query.Where(a => a.IsArchived == dto.IsArchived.Value);
        else
            query = query.Where(a => !a.IsArchived);

        query = query.Where(a => isBoard
                                || a.ShowInKoala
                                || (a.OrganizerId != null && userGroupIds.Contains(a.OrganizerId.Value) && !a.ShowOnWebsite && !a.ShowInKoala && !a.EnrollOpenDate.HasValue));

        if (!dto.IncludePast)
            query = query.Where(a => a.DateTimeEnd >= now);

        if (!dto.IncludeFuture)
            query = query.Where(a => a.DateTimeStart < now);

        if (dto.Year.HasValue)
        {
            var parts = YearUtils.CommitteeCreationDate.Split('-');
            int month = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int day = int.Parse(parts[1], CultureInfo.InvariantCulture);

            uint selectedYear = dto.Year.Value;

            uint creationYear = month > 6
                ? selectedYear - 1
                : selectedYear;

            var creationThreshold = new DateTime((int)creationYear, month, day).Date;
            var nextCreationThreshold = creationThreshold.AddYears(1);

            query = query.Where(a =>
                a.DateTimeStart >= creationThreshold &&
                a.DateTimeStart < nextCreationThreshold
            );
        }

        if (dto.OpenForPayment.HasValue)
            query = query.Where(a => a.IsOpenForPayment == dto.OpenForPayment.Value);

        if (dto.UserId.HasValue)
            query = query.Where(a => a.Enrollments.Any(e => e.MemberId == dto.UserId.Value && !e.IsOnWaitingList));

        query = query.OrderBy(a => a.DateTimeStart);

        return query;
    }

    /// <summary>
    /// Applies pagination to a queryable collection of Activity entities based on the page number and page size specified in a GetActivitiesDTO object.
    /// </summary>
    /// <param name="query">The queryable collection of Activity entities to paginate.</param>
    /// <param name="dto">The data transfer object containing pagination settings.</param>
    /// <returns>The paginated queryable collection of Activity entities.</returns>
    public static IQueryable<Activity> ApplyPaging(
        this IQueryable<Activity> query,
        GetActivitiesDTO dto)
    {
        if (!dto.Page.HasValue && !dto.PageSize.HasValue)
            return query;

        int pageSize = dto.PageSize.HasValue && dto.PageSize.Value > 0 ? dto.PageSize.Value : 50;
        int skip = (dto.Page.HasValue && dto.Page.Value > 0 ? dto.Page.Value - 1 : 0) * pageSize;

        return query
            .OrderByDescending(a => a.DateTimeStart)
            .Skip(skip)
            .Take(pageSize);
    }
}

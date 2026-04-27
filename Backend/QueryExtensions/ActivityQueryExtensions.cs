using Backend.Controllers.DTOs;
using Backend.Models.Domain;

public static class ActivityQueryExtensions
{
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
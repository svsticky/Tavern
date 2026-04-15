using Backend.Controllers.DTOs;

namespace Backend.QueryExtensions;

public static class ActivityQueryExtensions
{
    public static IQueryable<ActivityResponseDTO> Filter(this IQueryable<ActivityResponseDTO> query, GetActivitiesDTO dto)
    {
        var now = DateTime.UtcNow;

        if (!dto.IncludePast)
            query = query.Where(a => a.DateTimeEnd > now && a.ShowInKoala);

        if (!dto.IncludeFuture)
            query = query.Where(a => a.DateTimeStart < now && a.ShowInKoala);

        if (dto.Year.HasValue)
            query = query.Where(a => a.DateTimeStart.Year == dto.Year.Value);

        if (dto.OpenForPayment.HasValue)
            query = query.Where(a => a.IsOpenForPayment == dto.OpenForPayment.Value);

        return query;
    }
}
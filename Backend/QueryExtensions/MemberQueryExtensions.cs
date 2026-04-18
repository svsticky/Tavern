using Backend.Controllers.DTOs;
using Backend.Models.Domain;

public static class MemberQueryExtensions
{
    public static IQueryable<Member> Filter(
        this IQueryable<Member> query, 
        GetMembersDto dto)
    {
        var now = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(dto.Search))
        {
            query = query.Where(m => m.FirstName.Contains(dto.Search) || m.LastName.Contains(dto.Search) || m.Email.Contains(dto.Search) || m.StudentNumber.ToString().Contains(dto.Search) || m.PhoneNumber.Contains(dto.Search));
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

        if(dto.StudyType.HasValue)
        {
            query = query.Where(m => m.StudyEnrollments.Any(se => se.Study.Type == dto.StudyType.Value));
        }

        return query;
    }
}
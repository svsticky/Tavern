using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class MemberProjections
{
    public static Expression<Func<Member, MemberResponseDTO>> ToDto(Guid userId, bool isBoard)
    {
        return m => new MemberResponseDTO
        {
            Id = m.Id,
            StudentNumber = isBoard || userId == m.Id ? m.StudentNumber : null,
            FirstName = isBoard || userId == m.Id ? m.FirstName : null,
            LastName = isBoard || userId == m.Id ? m.LastName : null,
            Email = isBoard || userId == m.Id ? m.Email : null,
            PhoneNumber = isBoard || userId == m.Id ? m.PhoneNumber : null,
            Street = isBoard || userId == m.Id ? m.Street : null,
            HouseNumber = isBoard || userId == m.Id ? m.HouseNumber : null,
            PostalCode = isBoard || userId == m.Id ? m.PostalCode : null,
            City = isBoard || userId == m.Id ? m.City : null,
            DateOfBirth = isBoard || userId == m.Id ? m.DateOfBirth : null,
            ParentPhoneNumber = isBoard || userId == m.Id ? m.ParentPhoneNumber : null,
            MailSubscriptions = m.MailSubscriptions,
            Notes = isBoard || userId == m.Id ? m.Notes : null,
            RegisteredOn = isBoard || userId == m.Id ? m.RegisteredOn : null,
            PreferredLanguage = isBoard || userId == m.Id ? m.PreferredLanguage : null,
            StudyEnrollments = isBoard || userId == m.Id
                ? m.StudyEnrollments
                    .AsQueryable()
                    .Select(StudyEnrollmentProjections.ToDto())
                    .ToList()
                : null,
            GroupMemberships = isBoard || userId == m.Id ? m.GroupMemberships.Select(gm => GroupMembershipProjections.ToDto(userId, isBoard).Compile()(gm)).ToList() : null
        };
    }
}

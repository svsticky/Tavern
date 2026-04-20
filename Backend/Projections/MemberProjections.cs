using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class MemberProjections
{
    public static Expression<Func<Member, MemberResponseDTO>> ToListDto()
    {
        return m => new MemberResponseDTO
        {
            Id = m.Id,
            StudentNumber = m.StudentNumber,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            PhoneNumber = m.PhoneNumber,
            Street = m.Street,
            HouseNumber = m.HouseNumber,
            PostalCode = m.PostalCode,
            City = m.City,
            DateOfBirth = m.DateOfBirth,
            ParentPhoneNumber = m.ParentPhoneNumber,
            MailSubscriptions = m.MailSubscriptions,
            Notes = m.Notes,
            RegisteredOn = m.RegisteredOn,
            PreferredLanguage = m.PreferredLanguage,
            StudyEnrollments = m.StudyEnrollments.Select(se => new StudyEnrollmentResponseDTO
            {
                Id = se.Id,
                StudyId = se.StudyId,
                StudyTitle = se.Study.Title,
                MemberId = se.MemberId,
                MemberName = m.FirstName + " " + m.LastName,
                EnrollmentDate = se.EnrollmentDate,
                CompletionDate = se.CompletionDate,
                Status = se.Status
            }).ToList(),
            GroupMemberships = m.GroupMemberships.Select(gm => new GroupMembershipResponseDTO
            {
                Id = gm.Id,
                GroupId = gm.GroupId,
                GroupName = gm.Group.Name,
                GroupType = gm.Group.Type,
                MemberId = gm.MemberId,
                MemberName = m.FirstName + " " + m.LastName,
                MembershipYear = gm.MembershipYear,
                RoleAliasId = gm.RoleAlias != null ? gm.RoleAlias.Id : null,
                RoleAliasName = gm.RoleAlias != null ? gm.RoleAlias.Name : null
            }).ToList()
        };
    }

    public static Expression<Func<Member, MemberResponseDTO>> ToDetailDto(bool isBoard)
    {
        return m => new MemberResponseDTO
        {
            Id = m.Id,
            StudentNumber = m.StudentNumber,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            PhoneNumber = m.PhoneNumber,
            Street = m.Street,
            HouseNumber = m.HouseNumber,
            PostalCode = m.PostalCode,
            City = m.City,
            DateOfBirth = m.DateOfBirth,
            ParentPhoneNumber = m.ParentPhoneNumber,
            MailSubscriptions = m.MailSubscriptions,
            Notes = isBoard ? m.Notes : null,
            RegisteredOn = m.RegisteredOn,
            PreferredLanguage = m.PreferredLanguage,
            StudyEnrollments = m.StudyEnrollments.Select(se => new StudyEnrollmentResponseDTO
            {
                Id = se.Id,
                StudyId = se.StudyId,
                StudyTitle = se.Study.Title,
                MemberId = se.MemberId,
                MemberName = m.FirstName + " " + m.LastName,
                EnrollmentDate = se.EnrollmentDate,
                CompletionDate = se.CompletionDate,
                Status = se.Status
            }).ToList()
        };
    }
}

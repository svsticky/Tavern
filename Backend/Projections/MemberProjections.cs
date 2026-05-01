using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class MemberProjections
{
    /// <summary>
    /// Projects a Member entity into a MemberResponseDTO, including conditional logic to determine which fields to include based on whether the requester is a board member or the member themselves. The method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information such as personal details, study enrollments, and group memberships based on the user's role. This projection is used to transform the data from the Member model into a format that is suitable for API responses, ensuring that sensitive information is only included when appropriate while still providing relevant details about the member when necessary. The inclusion of related entities like study enrollments and group memberships further enriches the response with contextual information about the member's involvement in various activities and groups within the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to project the member information.</param>
    /// <param name="isBoard">A boolean indicating whether the requester is a board member.</param>
    /// <returns>An expression that projects a Member entity into a MemberResponseDTO.</returns>
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
            Notes = isBoard ? m.Notes : null,
            RegisteredOn = isBoard || userId == m.Id ? m.RegisteredOn : null,
            PreferredLanguage = isBoard || userId == m.Id ? m.PreferredLanguage : null,
            ProfilePicturePath = m.ProfilePicturePath,
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

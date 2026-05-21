using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

/// <summary>
/// The GroupMembershipProjections class provides a method to project a GroupMembership entity into a GroupMembershipResponseDTO. This projection is used to transform the data from the GroupMembership model into a format that is suitable for API responses, including related member and group information, as well as role alias details. The ToDto method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information based on the user's role. This class helps to centralize the logic for mapping GroupMembership entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling group membership-related data transformations for API responses.
/// </summary>
public static class GroupMembershipProjections
{
    /// <summary>
    /// Projects a GroupMembership entity into a GroupMembershipResponseDTO, including related member and group information, as well as role alias details. The method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information based on the user's role. This projection is used to transform the data from the GroupMembership model into a format that is suitable for API responses, ensuring that the relevant information is included while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to project the group membership.</param>
    /// <param name="isBoard">A boolean indicating whether the requester is a board member.</param>
    /// <returns>An expression that projects a GroupMembership entity into a GroupMembershipResponseDTO.</returns>
    public static Expression<Func<GroupMembership, GroupMembershipResponseDTO>> ToDto(Guid userId, bool isBoard)
    {
        return gm => new GroupMembershipResponseDTO
        {
            Id = gm.Id,
            MemberId = isBoard || gm.MemberId == userId ? gm.MemberId : null,
            MemberName = isBoard || gm.MemberId == userId
                ? (gm.Member != null ? gm.Member.FirstName + " " + gm.Member.LastName : null)
                : null,
            GroupId = gm.GroupId,
            GroupName = gm.Group != null ? gm.Group.Name : string.Empty,
            GroupType = gm.Group != null ? gm.Group.Type : default,
            MembershipYear = gm.MembershipYear,
            RoleAliasId = gm.RoleAlias != null ? gm.RoleAlias.Id : null,
            RoleAliasName = gm.RoleAlias != null ? gm.RoleAlias.Name : null
        };
    }
}
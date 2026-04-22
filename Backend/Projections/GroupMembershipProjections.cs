using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

public static class GroupMembershipProjections
{
    public static Expression<Func<GroupMembership, GroupMembershipResponseDTO>> ToDto(Guid userId, bool isBoard)
    {
        return gm => new GroupMembershipResponseDTO
        {
            Id = gm.Id,
            MemberId = isBoard || gm.MemberId == userId ? gm.MemberId : null,
            MemberName = isBoard || gm.MemberId == userId ? gm.Member.FirstName + " " + gm.Member.LastName : null,
            GroupId = gm.GroupId,
            GroupName = gm.Group.Name,
            GroupType = gm.Group.Type,
            MembershipYear = gm.MembershipYear,
            RoleAliasId = gm.RoleAlias != null ? gm.RoleAlias.Id : null,
            RoleAliasName = gm.RoleAlias != null ? gm.RoleAlias.Name : null
        };
    }
}
using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

public static class GroupMembershipProjections
{
    public static Expression<Func<GroupMembership, GroupMembershipResponseDTO>> ToDto()
    {
        return gm => new GroupMembershipResponseDTO
        {
            Id = gm.Id,
            MemberId = gm.MemberId,
            MemberName = gm.Member.FirstName + " " + gm.Member.LastName,
            GroupId = gm.GroupId,
            GroupName = gm.Group.Name,
            GroupType = gm.Group.Type,
            MembershipYear = gm.MembershipYear,
            RoleAliasId = gm.RoleAlias != null ? gm.RoleAlias.Id : null,
            RoleAliasName = gm.RoleAlias != null ? gm.RoleAlias.Name : null
        };
    }
}
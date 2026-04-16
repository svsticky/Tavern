using Backend.Models.Domain;

public class CreateGroupMembershipValidator
{
    public void Validate(Member member, Group group, RoleAlias? roleAlias)
    {
        if (member == null)
            throw new ArgumentException("Member not found");

        if (group == null)
            throw new ArgumentException("Group not found");

        if (roleAlias == null)
            return;
    }
}
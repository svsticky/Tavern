using Backend.Models;

namespace Backend.Interfaces;

public interface IPermissionService
{
    uint GetCurrentFinancialYear();
    
    bool IsInGroupInCurrentYear(Guid memberId, uint groupId);
    bool IsInGroupInCurrentYear(Member member, uint groupId);
    bool IsInGroup(Guid memberId, uint groupId, uint year);
    bool IsInGroup(Member member, uint groupId, uint year);

    bool IsInRoleInCurrentYear(Guid memberId, uint roleId, uint? groupId = null);
    bool IsInRoleInCurrentYear(Member member, uint roleId, uint? groupId = null);
    bool IsInRole(Guid memberId, uint roleId, uint year, uint? groupId = null);
    bool IsInRole(Member member, uint roleId, uint year, uint? groupId = null);
    
    bool IsBoardMember(Guid memberId);
}
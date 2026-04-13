using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Utils;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace Backend.Services;

public class PermissionService(PostgresDbContext db) : IPermissionService
{
    public uint GetCurrentFinancialYear()
    {
        string timezoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "W. Europe Standard Time" 
            : "Europe/Amsterdam";
        
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        DateTime nowInNetherlands = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        // Boekjaar loopt van augustus tot augustus
        return nowInNetherlands.Month >= 8 
            ? (uint)nowInNetherlands.Year + 1 
            : (uint)nowInNetherlands.Year;
    }

    #region Group Checks
    public bool IsInGroupInCurrentYear(Guid memberId, uint groupId) 
        => IsInGroup(memberId, groupId, GetCurrentFinancialYear());

    public bool IsInGroupInCurrentYear(Member member, uint groupId) 
        => IsInGroup(member, groupId, GetCurrentFinancialYear());

    public bool IsInGroup(Guid memberId, uint groupId, uint year)
    {
        return db.GroupMemberships.Any(gm => 
            gm.MemberId == memberId && 
            gm.GroupId == groupId && 
            gm.MembershipYear == year);
    }

    public bool IsInGroup(Member member, uint groupId, uint year)
    {
        return member.GroupMemberships.Any(gm => 
            gm.GroupId == groupId && 
            gm.MembershipYear == year);
    }
    #endregion

    #region Role Checks
    public bool IsInRoleInCurrentYear(Guid memberId, uint roleId, uint? groupId = null)
        => IsInRole(memberId, roleId, GetCurrentFinancialYear(), groupId);

    public bool IsInRoleInCurrentYear(Member member, uint roleId, uint? groupId = null)
        => IsInRole(member, roleId, GetCurrentFinancialYear(), groupId);

    public bool IsInRole(Guid memberId, uint roleId, uint year, uint? groupId = null)
    {
        var query = db.GroupMemberships.Where(gm => 
            gm.MemberId == memberId && 
            gm.MembershipYear == year && 
            gm.RoleAlias != null && 
            gm.RoleAlias.RoleId == roleId);

        if (groupId.HasValue)
            query = query.Where(gm => gm.GroupId == groupId.Value);

        return query.Any();
    }

    public bool IsInRole(Member member, uint roleId, uint year, uint? groupId = null)
    {
        var query = member.GroupMemberships.AsQueryable().Where(gm => 
            gm.MembershipYear == year && 
            gm.RoleAlias != null && 
            gm.RoleAlias.RoleId == roleId);

        if (groupId.HasValue)
            query = query.Where(gm => gm.GroupId == groupId.Value);

        return query.Any();
    }
    #endregion

    public bool IsBoardMember(Guid memberId)
    {
        return IsInGroupInCurrentYear(memberId, PredefinedGroups.Board);
    }
}
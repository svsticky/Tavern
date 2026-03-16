using Backend.Database;
using Backend.Models;
using System.Runtime.InteropServices;

namespace Backend.Utils;

public static class PermissionUtils
{
    private static uint GetCurrentFinancialYear()
    {
        string timezoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "W. Europe Standard Time" 
            : "Europe/Amsterdam";
        
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        DateTime nowInNetherlands = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        return nowInNetherlands.Month >= 8 
            ? (uint)nowInNetherlands.Year + 1 
            : (uint)nowInNetherlands.Year;
    }

    public static bool IsInGroupInCurrentYear(Guid memberId, uint groupId, PostgresDbContext db) 
        => IsInGroup(memberId, groupId, GetCurrentFinancialYear(), db);

    public static bool IsInGroupInCurrentYear(Member member, uint groupId) 
        => IsInGroup(member, groupId, GetCurrentFinancialYear());

    public static bool IsInGroupInCurrentYear(Member member, Group group) 
        => IsInGroup(member, group, GetCurrentFinancialYear());

    public static bool IsInGroupInCurrentYear(Guid memberId, Group group, PostgresDbContext db) 
        => IsInGroup(memberId, group, GetCurrentFinancialYear(), db);

    public static bool IsInRoleInCurrentYear(Guid memberId, uint roleId, uint groupId, PostgresDbContext db) 
        => IsInRole(memberId, roleId, groupId, GetCurrentFinancialYear(), db);

    public static bool IsInRoleInCurrentYear(Member member, uint roleId, uint groupId) 
        => IsInRole(member, roleId, groupId, GetCurrentFinancialYear());

    public static bool IsInRoleInCurrentYear(Member member, uint roleId, Group group) 
        => IsInRole(member, roleId, group, GetCurrentFinancialYear());

    public static bool IsInRoleInCurrentYear(Guid memberId, uint roleId, Group group, PostgresDbContext db) 
        => IsInRole(memberId, roleId, group, GetCurrentFinancialYear(), db);

    public static bool IsInRoleInCurrentYear(Guid memberId, Role role, uint groupId, PostgresDbContext db) 
        => IsInRole(memberId, role, groupId, GetCurrentFinancialYear(), db);

    public static bool IsInRoleInCurrentYear(Member member, Role role, uint groupId) 
        => IsInRole(member, role, groupId, GetCurrentFinancialYear());

    public static bool IsInRoleInCurrentYear(Member member, Role role, Group group) 
        => IsInRole(member, role, group, GetCurrentFinancialYear());

    public static bool IsInRoleInCurrentYear(Guid memberId, Role role, Group group, PostgresDbContext db) 
        => IsInRole(memberId, role, group, GetCurrentFinancialYear(), db);

    public static bool IsInRoleInCurrentYear(Guid memberId, uint roleId, PostgresDbContext db) 
        => IsInRole(memberId, roleId, GetCurrentFinancialYear(), db);

    public static bool IsInRoleInCurrentYear(Member member, uint roleId) 
        => IsInRole(member, roleId, GetCurrentFinancialYear());

    public static bool IsInRoleInCurrentYear(Member member, Role role) 
        => IsInRole(member, role, GetCurrentFinancialYear());

    public static bool IsInRoleInCurrentYear(Guid memberId, Role role, PostgresDbContext db) 
        => IsInRole(memberId, role, GetCurrentFinancialYear(), db);


    public static bool IsInGroup(Guid memberId, uint groupId, uint year, PostgresDbContext db)
    {
        return db.GroupMemberships.Any(gm => gm.MemberId == memberId && gm.GroupId == groupId && gm.MembershipYear == year);
    }

    public static bool IsInGroup(Member member, uint groupId, uint year)
    {
        return member.GroupMemberships.Any(gm => gm.GroupId == groupId && gm.MembershipYear == year);
    }

    public static bool IsInGroup(Member member, Group group, uint year)
    {
        return member.GroupMemberships.Any(gm => gm.GroupId == group.Id && gm.MembershipYear == year);
    }

    public static bool IsInGroup(Guid memberId, Group group, uint year, PostgresDbContext db)
    {
        return db.GroupMemberships.Any(gm => gm.MemberId == memberId && gm.GroupId == group.Id && gm.MembershipYear == year);
    }

    public static bool IsInRole(Guid memberId, uint roleId, uint groupId, uint year, PostgresDbContext db)
    {
        return db.GroupMemberships.Any(gm => gm.MemberId == memberId && gm.GroupId == groupId && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == roleId);
    }

    public static bool IsInRole(Member member, uint roleId, uint groupId, uint year)
    {
        return member.GroupMemberships.Any(gm => gm.GroupId == groupId && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == roleId);
    }

    public static bool IsInRole(Member member, uint roleId, Group group, uint year)
    {
        return member.GroupMemberships.Any(gm => gm.GroupId == group.Id && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == roleId);
    }

    public static bool IsInRole(Guid memberId, uint roleId, Group group, uint year, PostgresDbContext db)
    {
        return db.GroupMemberships.Any(gm => gm.MemberId == memberId && gm.GroupId == group.Id && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == roleId);
    }

    public static bool IsInRole(Guid memberId, Role role, uint groupId, uint year, PostgresDbContext db)
    {
        return db.GroupMemberships.Any(gm => gm.MemberId == memberId && gm.GroupId == groupId && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == role.Id);
    }

    public static bool IsInRole(Member member, Role role, uint groupId, uint year)
    {
        return member.GroupMemberships.Any(gm => gm.GroupId == groupId && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == role.Id);
    }

    public static bool IsInRole(Member member, Role role, Group group, uint year)
    {
        return member.GroupMemberships.Any(gm => gm.GroupId == group.Id && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == role.Id);
    }

    public static bool IsInRole(Guid memberId, Role role, Group group, uint year, PostgresDbContext db)
    {
        return db.GroupMemberships.Any(gm => gm.MemberId == memberId && gm.GroupId == group.Id && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == role.Id);
    }

    public static bool IsInRole(Guid memberId, uint roleId, uint year, PostgresDbContext db)
    {
        return db.GroupMemberships.Any(gm => gm.MemberId == memberId && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == roleId);
    }

    public static bool IsInRole(Member member, uint roleId, uint year)
    {
        return member.GroupMemberships.Any(gm => gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == roleId);
    }
    
    public static bool IsInRole(Member member, Role role, uint year)
    {
        return member.GroupMemberships.Any(gm => gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == role.Id);
    }

    public static bool IsInRole(Guid memberId, Role role, uint year, PostgresDbContext db)
    {
        return db.GroupMemberships.Any(gm => gm.MemberId == memberId && gm.MembershipYear == year && gm.RoleAlias != null && gm.RoleAlias.RoleId == role.Id);
    }
}
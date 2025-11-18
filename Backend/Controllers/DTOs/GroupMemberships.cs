using Backend.Models;

namespace Backend.Controllers.DTOs;

public class PostGroupMembershipDTO
{
    /// <inheritdoc cref="Models.GroupMembership.MemberId"/>
    public required uint MemberId { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.GroupId"/>
    public required uint GroupId { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.MembershipYear"/>
    public required uint MembershipYear { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.RoleId"/>
    public uint? RoleId { get; set; }
}

public class GroupMembershipResponseDTO
{
    /// <inheritdoc cref="Models.GroupMembership.Id"/>
    public uint Id { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.MemberId"/>
    public uint? MemberId { get; set; }

    /// <inheritdoc cref="Models.Member"/>
    public string? MemberName { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.GroupId"/>
    public uint? GroupId { get; set; }

    /// <inheritdoc cref="Models.Group"/>
    public string? GroupName { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.MembershipYear"/>
    public uint MembershipYear { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.RoleId"/>
    public uint? RoleId { get; set; }

    /// <inheritdoc cref="Models.Role.Name"/>
    public string? RoleName { get; set; }
}

public class GroupMembershipUpdateDTO
{
    public uint? RoleId { get; set; }
}
using Backend.Models.Domain;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a group membership, containing the necessary information for creating a new group membership, including member ID, group ID, membership year, and an optional role alias ID. The PostGroupMembershipDTO is used to transfer data from the client to the server when creating a new group membership, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostGroupMembershipDTO
{
    /// <inheritdoc cref="Models.GroupMembership.MemberId"/>
    public required Guid MemberId { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.GroupId"/>
    public required uint GroupId { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.MembershipYear"/>
    public required uint MembershipYear { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.RoleAliasId"/>
    public uint? RoleAliasId { get; set; }
}

/// <summary>
/// Represents the response DTO for a group membership, containing all relevant information about the group membership, including its properties and information about the associated member and group. The GroupMembershipResponseDTO is used to transfer comprehensive group membership data from the server to the client when retrieving group membership information, allowing for a complete representation of the group membership's details, member information, group information, and any associated role alias information in the response payload.
/// </summary>
public class GroupMembershipResponseDTO
{
    /// <inheritdoc cref="Models.GroupMembership.Id"/>
    public required uint Id { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.MemberId"/>
    public Guid? MemberId { get; set; }

    /// <inheritdoc cref="Models.Member"/>
    public string? MemberName { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.GroupId"/>
    public required uint GroupId { get; set; }

    /// <inheritdoc cref="Models.Group"/>
    public required string GroupName { get; set; }

    /// <inheritdoc cref="Models.Group.Type"/>
    public required GroupType GroupType { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.MembershipYear"/>
    public required uint MembershipYear { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.RoleAliasId"/>
    public uint? RoleAliasId { get; set; }

    /// <inheritdoc cref="Models.RoleAlias.Name"/>
    public string? RoleAliasName { get; set; }
}

/// <summary>
/// Defines the DTO for updating an existing group membership, containing all necessary information for modifying a group membership's properties. The GroupMembershipUpdateDTO is used to transfer data from the client to the server when updating an existing group membership, allowing for changes to be made to the group membership's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class GroupMembershipUpdateDTO
{
    /// <inheritdoc cref="Models.GroupMembership.MemberId"/>
    public uint? RoleAliasId { get; set; }
}

/// <summary>
/// Defines the DTO for retrieving group memberships, containing the necessary information for filtering and retrieving group membership data based on specific criteria. The GetGroupMembershipsDTO is used to transfer data from the client to the server when retrieving group membership information, allowing for the application of filters such as group ID, membership year, and member ID to retrieve group memberships that match the specified criteria, ensuring that the retrieved group membership data is relevant and tailored to the client's needs.
/// </summary>
public class GetGroupMembershipsDTO
{
    /// <inheritdoc cref="Models.GroupMembership.GroupId"/>
    public uint? GroupId { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.MembershipYear"/>
    public uint? MembershipYear { get; set; }

    /// <inheritdoc cref="Models.GroupMembership.MemberId"/>
    public Guid? MemberId { get; set; }
}
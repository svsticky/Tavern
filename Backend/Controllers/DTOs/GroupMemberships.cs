using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a group membership, containing the necessary information for creating a new group membership, including member ID, group ID, membership year, and an optional role alias ID. The PostGroupMembershipDTO is used to transfer data from the client to the server when creating a new group membership, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostGroupMembershipDTO
{
    /// <inheritdoc cref="GroupMembership.MemberId"/>
    public required Guid MemberId { get; set; }

    /// <inheritdoc cref="GroupMembership.GroupId"/>
    public required uint GroupId { get; set; }

    /// <inheritdoc cref="GroupMembership.MembershipYear"/>
    public required uint MembershipYear { get; set; }

    /// <inheritdoc cref="GroupMembership.RoleAliasId"/>
    public uint? RoleAliasId { get; set; }
}

/// <summary>
/// Represents the response DTO for a group membership, containing all relevant information about the group membership, including its properties and information about the associated member and group. The GroupMembershipResponseDTO is used to transfer comprehensive group membership data from the server to the client when retrieving group membership information, allowing for a complete representation of the group membership's details, member information, group information, and any associated role alias information in the response payload.
/// </summary>
public class GroupMembershipResponseDTO
{
    /// <inheritdoc cref="GroupMembership.Id"/>
    public required uint Id { get; set; }

    /// <inheritdoc cref="GroupMembership.MemberId"/>
    public Guid? MemberId { get; set; }

    /// <inheritdoc cref="Member.FirstName"/>
    public string? MemberName { get; set; }

    /// <inheritdoc cref="GroupMembership.GroupId"/>
    public required uint GroupId { get; set; }

    /// <inheritdoc cref="Group"/>
    public required string GroupName { get; set; }

    /// <inheritdoc cref="Group.Type"/>
    public required GroupType GroupType { get; set; }

    /// <inheritdoc cref="GroupMembership.MembershipYear"/>
    public required uint MembershipYear { get; set; }

    /// <inheritdoc cref="GroupMembership.RoleAliasId"/>
    public uint? RoleAliasId { get; set; }

    /// <inheritdoc cref="RoleAlias.Name"/>
    public string? RoleAliasName { get; set; }

    /// <summary>
    /// Projects a GroupMembership entity into a GroupMembershipResponseDTO, including related member and group information, as well as role alias details. The method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information based on the user's role. This projection is used to transform the data from the GroupMembership model into a format that is suitable for API responses, ensuring that the relevant information is included while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to project the group membership.</param>
    /// <param name="hasViewMembers">
    /// A boolean indicating whether the requester has the ViewMembers permission (or is a (candidate) board
    /// member, who always has it).
    /// </param>
    /// <returns>An expression that projects a GroupMembership entity into a GroupMembershipResponseDTO.</returns>
    public static Expression<Func<GroupMembership, GroupMembershipResponseDTO>> ToDto(Guid userId, bool hasViewMembers)
    {
        return gm => new GroupMembershipResponseDTO
        {
            Id = gm.Id,
            MemberId = hasViewMembers || gm.MemberId == userId ? gm.MemberId : null,
            MemberName = hasViewMembers || gm.MemberId == userId
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

/// <summary>
/// Defines the DTO for updating an existing group membership, containing all necessary information for modifying a group membership's properties. The GroupMembershipUpdateDTO is used to transfer data from the client to the server when updating an existing group membership, allowing for changes to be made to the group membership's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class GroupMembershipUpdateDTO
{
    /// <inheritdoc cref="GroupMembership.MemberId"/>
    public uint? RoleAliasId { get; set; }
}

/// <summary>
/// Defines the DTO for retrieving group memberships, containing the necessary information for filtering and retrieving group membership data based on specific criteria. The GetGroupMembershipsDTO is used to transfer data from the client to the server when retrieving group membership information, allowing for the application of filters such as group ID, membership year, and member ID to retrieve group memberships that match the specified criteria, ensuring that the retrieved group membership data is relevant and tailored to the client's needs.
/// </summary>
public class GetGroupMembershipsDTO
{
    /// <inheritdoc cref="GroupMembership.GroupId"/>
    public uint? GroupId { get; set; }

    /// <inheritdoc cref="GroupMembership.MembershipYear"/>
    public uint? MembershipYear { get; set; }

    /// <inheritdoc cref="GroupMembership.MemberId"/>
    public Guid? MemberId { get; set; }
}

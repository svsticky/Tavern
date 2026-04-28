using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a group, containing the necessary information for creating a new group, including its name, type, and an optional group picture. The PostGroupDTO is used to transfer data from the client to the server when creating a new group, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostGroupDTO
{
    /// <inheritdoc cref="Models.Group.Name"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Group.Type"/>
    public required GroupType Type { get; set; }

    /// <summary>
    /// The group picture file to be uploaded and associated with the newly created group. This field is optional and can be included in the request to provide a visual representation of the group, allowing for better identification and differentiation of groups within the system. If provided, the group picture will be processed and stored appropriately based on the application's file handling and storage mechanisms.
    /// </summary>
    public required IFormFile GroupPicture { get; set; }
}

/// <summary>
/// Defines the DTO for retrieving groups, containing the necessary information for filtering and retrieving group data based on specific criteria. The GetGroupDTO is used to transfer data from the client to the server when retrieving group information, allowing for the application of filters such as membership year and active status to retrieve groups that match the specified criteria, ensuring that the retrieved group data is relevant and tailored to the client's needs.
/// </summary>
public class GetGroupDTO
{
    /// <inheritdoc cref="Models.GroupMembership.MembershipYear"/>
    public uint? MembershipYear { get; set; }

    /// <summary>
    /// Indicates whether to include inactive groups in the retrieved group data. If set to true, both active and inactive groups will be included in the response; if set to false, only active groups will be included. This field allows for filtering group data based on their active status, enabling the retrieval of groups that are currently active or all groups regardless of their active status based on the client's needs.
    /// </summary>
    public bool IncludeInactive { get; set; } = false;
}

/// <summary>
/// Represents the response DTO for a group, containing all relevant information about the group, including its properties and any associated data. The GroupResponseDTO is used to transfer comprehensive group data from the server to the client when retrieving group information, allowing for a complete representation of the group's details in the response payload. The GroupResponseDTO can include properties such as the group's unique identifier, name, active status, type, and any associated group picture information, providing a comprehensive view of the group data for the client application.
/// </summary>
public class GroupResponseDTO
{
    /// <inheritdoc cref="Models.Group.Id"/>
    public required uint Id { get; set; }

    /// <inheritdoc cref="Models.Group.Name"/>
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Group.Active"/>
    public required bool Active { get; set; } = true;

    /// <inheritdoc cref="Models.Group.Type"/>
    public required GroupType Type { get; set; }

    /// <inheritdoc cref="Models.Group.GroupPicturePath"/>
    public string? GroupPicturePath { get; set; }
}

/// <summary>
/// Defines the DTO for updating an existing group, containing all necessary information for modifying a group's properties. The GroupUpdateDTO is used to transfer data from the client to the server when updating an existing group, allowing for changes to be made to the group's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class GroupUpdateDTO
{
    /// <inheritdoc cref="Models.Group.Name"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Group.Active"/>
    public required bool Active { get; set; }

    /// <inheritdoc cref="Models.Group.Type"/>
    public required GroupType Type { get; set; }
}
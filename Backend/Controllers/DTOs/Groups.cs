using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class PostGroupDTO
{
    /// <inheritdoc cref="Models.Group.Name"/>
    [StringLength(100)]
    public required string Name { get; set; }
}

public class GroupResponseDTO
{
    /// <inheritdoc cref="Models.Group.Id"/>
    public uint Id { get; set; }

    /// <inheritdoc cref="Models.Group.Name"/>
    public string Name { get; set; } = null!;

    /// <inheritdoc cref="Models.Group.Active"/>
    public bool Active { get; set; } = true;

    /// <inheritdoc cref="Models.Group.GroupMemberships"/>
    public List<GroupMembershipResponseDTO> GroupMemberships { get; set; } = new();
}

public class GroupUpdateDTO
{
    /// <inheritdoc cref="Models.Group.Name"/>
    public required string Name { get; set; }
}
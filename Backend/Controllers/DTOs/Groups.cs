using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class PostGroupDTO
{
    /// <inheritdoc cref="Models.Group.Name"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Group.Type"/>
    public required GroupType Type { get; set; }

    public required IFormFile GroupPicture { get; set; }
}

public class GetGroupDTO
{
    public uint? MembershipYear { get; set; }
    public bool IncludeInactive { get; set; } = false;
}

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

    public string? GroupPicturePath;
}

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
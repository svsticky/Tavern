using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class PostGroupDTO
{
    /// <inheritdoc cref="Models.Group.Name"/>
    [StringLength(100)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Group.Type"/>
    public required Models.GroupType Type { get; set; }
}

public class GroupResponseDTO
{
    /// <inheritdoc cref="Models.Group.Id"/>
    public uint Id { get; set; }

    /// <inheritdoc cref="Models.Group.Name"/>
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Group.Active"/>
    public bool Active { get; set; } = true;

    /// <inheritdoc cref="Models.Group.GroupMemberships"/>
    public List<GroupMembershipResponseDTO> GroupMemberships { get; set; } = new();

    /// <inheritdoc cref="Models.Group.Type"/>
    public Models.GroupType Type { get; set; }

    /// <inheritdoc cref="Models.Group.Activities"/>
    public List<ActivityResponseDTO>? Activities { get; set; } = null;

    /// <inheritdoc cref="Models.Group.GeneralLedgerAccount"/>
    public string? GeneralLedgerAccount { get; set; } = null;

    /// <inheritdoc cref="Models.Group.CostCenterId"/>
    public string? CostCenterId { get; set; } = null;

    /// <inheritdoc cref="Models.Group.CostUnit"/>
    public string? CostUnit { get; set; } = null;
}

public class GroupUpdateDTO
{
    /// <inheritdoc cref="Models.Group.Name"/>
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Group.Active"/>
    public required bool Active { get; set; }

    /// <inheritdoc cref="Models.Group.Type"/>
    public required Models.GroupType Type { get; set; }
}
namespace Backend.Controllers.DTOs;

public class CommissionResponseDTO
{
    /// <inheritdoc cref="Models.Commission.Id"/>
    public uint Id { get; set; }

    /// <inheritdoc cref="Models.Commission.Name"/>
    public string Name { get; set; } = null!;

    /// <inheritdoc cref="Models.Commission.Active"/>
    public bool Active { get; set; } = true;

    /// <inheritdoc cref="Models.Commission.CommissionMemberships"/>
    public List<CommissionMembershipResponseDTO> CommissionMemberships { get; set; } = new ();
}

public class CommissionUpdateDTO
{
    /// <inheritdoc cref="Models.Commission.Name"/>
    public required string Name { get; set; }
}
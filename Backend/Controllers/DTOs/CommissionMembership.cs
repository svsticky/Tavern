using Backend.Models;

namespace Backend.Controllers.DTOs;

public class PostCommissionMembershipDTO
{
    /// <inheritdoc cref="Models.CommissionMembership.MemberId"/>
    public required uint MemberId { get; set; }

    /// <inheritdoc cref="Models.CommissionMembership.CommissionId"/>
    public required uint CommissionId { get; set; }

    /// <inheritdoc cref="Models.CommissionMembership.MembershipYear"/>
    public required uint MembershipYear { get; set; }
}

public class CommissionMembershipResponseDTO
{
    /// <inheritdoc cref="Models.CommissionMembership.Id"/>
    public uint Id { get; set; }

    /// <inheritdoc cref="Models.CommissionMembership.MemberId"/>
    public uint? MemberId { get; set; }

    /// <inheritdoc cref="Models.Member"/>
    public string? MemberName { get; set; }

    /// <inheritdoc cref="Models.CommissionMembership.CommissionId"/>
    public uint? CommissionId { get; set; }

    /// <inheritdoc cref="Models.Commission"/>
    public string? CommissionName { get; set; }

    /// <inheritdoc cref="Models.CommissionMembership.MembershipYear"/>
    public uint MembershipYear { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Controllers.DTOs;

// ReSharper disable once InconsistentNaming => Allow DTO as an acronym
public class PostActivityDTO
{
    /// <inheritdoc cref="Models.Activity.Name"/>
    [StringLength(120)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Activity.Price"/>
    [Column(TypeName = "numeric(18,2)")]
    public decimal Price { get; set; }

    /// <inheritdoc cref="Models.Activity.PosterFileName"/>
    public IFormFile? Poster { get; set; }

    /// <inheritdoc cref="Models.Activity.DutchDescription"/>
    [StringLength(240)]
    public required string DutchDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.EnglishDescription"/>
    [StringLength(240)]
    public required string EnglishDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeStart"/>
    public required DateTimeOffset DateTimeStart { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeEnd"/>
    public required DateTimeOffset DateTimeEnd { get; set; }

    /// <inheritdoc cref="Models.Activity.UnenrollmentDeadline"/>
    public required DateTimeOffset UnenrollmentDeadline { get; set; }

    /// <inheritdoc cref="Models.Activity.Location"/>
    [StringLength(200)]
    public required string Location { get; set; }

    /// <inheritdoc cref="Models.Activity.ParticipantLimit"/>
    public uint? ParticipantLimit { get; set; }

    /// <inheritdoc cref="Models.Activity.OrganizerId"/>
    public required uint OrganizerId { get; set; }

    /// <inheritdoc cref="Models.Activity.ShowInKoala"/>
    public bool ShowInKoala { get; set; } = true;

    /// <inheritdoc cref="Models.Activity.ShowOnWebsite"/>
    public bool ShowOnWebsite { get; set; } = true;

    /// <inheritdoc cref="Models.Activity.IsEnrollable"/>
    public bool IsEnrollable { get; set; } = true;

    /// <inheritdoc cref="Models.Activity.AreParticipantsVisible"/>
    public bool AreParticipantsVisible { get; set; }

    /// <inheritdoc cref="Models.Activity.IsAdultOnly"/>
    public bool IsAdultOnly { get; set; }

    /// <inheritdoc cref="Models.Activity.IsOpenToFirstYears"/>
    public bool IsOpenToFirstYears { get; set; } = true;

    /// <inheritdoc cref="Models.Activity.IsOpenToSecondYears"/>
    public bool IsOpenToSecondYears { get; set; } = true;

    /// <inheritdoc cref="Models.Activity.IsOpenToThirdYearsAndAbove"/>
    public bool IsOpenToThirdYearsAndAbove { get; set; } = true;

    /// <inheritdoc cref="Models.Activity.IsOpenToMasters"/>
    public bool IsOpenToMasters { get; set; } = true;

    /// <inheritdoc cref="Models.Activity.IsOpenForPayment"/>
    public bool IsOpenForPayment { get; set; }

    /// <inheritdoc cref="Models.Activity.VatRate"/>
    public uint VatRate { get; set; }

    /// <inheritdoc cref="Models.Activity.GLAccountId"/>
    public string? GLAccountId { get; set; }

    /// <inheritdoc cref="Models.Activity.CostCenterId"/>
    public string? CostCenterId { get; set; }

    /// <inheritdoc cref="Models.Activity.CostUnitId"/>
    public string? CostUnitId { get; set; }
}
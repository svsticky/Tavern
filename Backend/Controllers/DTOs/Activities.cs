using Backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Controllers.DTOs;

// ReSharper disable once InconsistentNaming => Allow DTO as an acronym
public class PostActivityDTO
{
    /// <inheritdoc cref="Models.Activity.Name"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Activity.Price"/>
    [Column(TypeName = "numeric(18,2)")]
    public decimal Price { get; set; }

    /// <inheritdoc cref="Models.Activity.PosterFileName"/>
    public IFormFile? Poster { get; set; }

    /// <inheritdoc cref="Models.Activity.DutchDescription"/>
    [StringLength(2000)]
    public required string DutchDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.EnglishDescription"/>
    [StringLength(2000)]
    public required string EnglishDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeStart"/>
    public required DateTimeOffset DateTimeStart { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeEnd"/>
    public required DateTimeOffset DateTimeEnd { get; set; }

    /// <inheritdoc cref="Models.Activity.UnenrollmentDeadline"/>
    public DateTimeOffset? UnenrollmentDeadline { get; set; }

    /// <inheritdoc cref="Models.Activity.EnrollmentDeadline"/>
    public DateTimeOffset? EnrollmentDeadline { get; set; }

    /// <inheritdoc cref="Models.Activity.Location"/>
    [StringLength(200)]
    public required string Location { get; set; }

    /// <inheritdoc cref="Models.Activity.ParticipantLimit"/>
    public uint? ParticipantLimit { get; set; }

    /// <inheritdoc cref="Models.Activity.OrganizerId"/>
    public uint? OrganizerId { get; set; }

    /// <inheritdoc cref="Models.Activity.SpecificationQuestions"/>
    public List<SpecificationQuestionDTO> SpecificationQuestions { get; set; } = new();

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

    /// <inheritdoc cref="Models.Activity.AllowedAudience"/>
    public TargetAudience AllowedAudience { get; set; } = TargetAudience.All;

    /// <inheritdoc cref="Models.Activity.VatRate"/>
    public uint? VatRate { get; set; }

    /// <inheritdoc cref="Models.Activity.GLAccountId"/>
    public string? GLAccountId { get; set; }

    /// <inheritdoc cref="Models.Activity.CostCenterId"/>
    public string? CostCenterId { get; set; }

    /// <inheritdoc cref="Models.Activity.CostUnitId"/>
    public string? CostUnitId { get; set; }
}

public class ActivityResponseDTO
{
    public uint Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? PosterPath { get; set; }
    public string? PosterFileName { get; set; }
    public string DutchDescription { get; set; } = null!;
    public string EnglishDescription { get; set; } = null!;
    public DateTimeOffset DateTimeStart { get; set; }
    public DateTimeOffset DateTimeEnd { get; set; }
    public DateTimeOffset? UnenrollmentDeadline { get; set; }
    public DateTimeOffset? EnrollmentDeadline { get; set; }
    public string Location { get; set; } = null!;
    public uint? ParticipantLimit { get; set; }
    public uint? OrganizerId { get; set; }
    public bool ShowInKoala { get; set; }
    public bool ShowOnWebsite { get; set; }
    public bool IsEnrollable { get; set; }
    public bool AreParticipantsVisible { get; set; }
    public bool IsAdultOnly { get; set; }
    public TargetAudience AllowedAudience { get; set; }
    public uint? VatRate { get; set; }
    public string? GLAccountId { get; set; }
    public string? CostCenterId { get; set; }
    public string? CostUnitId { get; set; }
    
    public List<EnrollmentSummaryDTO> Enrollments { get; set; } = new();
}
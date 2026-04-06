using Backend.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Controllers.DTOs;

// ReSharper disable once InconsistentNaming => Allow DTO as an acronym
public abstract class BaseActivityDTO<TQuestion>
{
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Price { get; set; }

    public IFormFile? Poster { get; set; }

    [StringLength(2000)]
    public required string DutchDescription { get; set; }

    [StringLength(2000)]
    public required string EnglishDescription { get; set; }

    public required DateTimeOffset DateTimeStart { get; set; }

    public required DateTimeOffset DateTimeEnd { get; set; }

    public DateTimeOffset? UnenrollmentDeadline { get; set; }

    public DateTimeOffset? EnrollmentDeadline { get; set; }

    [StringLength(200)]
    public required string Location { get; set; }

    public uint? ParticipantLimit { get; set; }

    public uint? OrganizerId { get; set; }

    public string? SpecificationQuestionsJson { get; set; }

    public bool ShowInKoala { get; set; } = true;
    public bool ShowOnWebsite { get; set; } = true;
    public bool IsEnrollable { get; set; } = true;
    public bool AreParticipantsVisible { get; set; }
    public bool IsAdultOnly { get; set; }
    public TargetAudience AllowedAudience { get; set; } = TargetAudience.All;
    public uint? VatRate { get; set; }
    public string? GLAccountId { get; set; }
    public string? CostCenterId { get; set; }
    public string? CostUnitId { get; set; }
    public DateTimeOffset? PaymentDeadline { get; set; }
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

    public List<GetSpecificationQuestionResponseDTO> SpecificationQuestions { get; set; } = new();

    public DateTimeOffset PaymentDeadline { get; set; }
}

public class PostActivityDTO : BaseActivityDTO<SpecificationQuestionDTO>
{
}

public class PutActivityDTO : BaseActivityDTO<UpdateSpecificationQuestionDTO>
{
}
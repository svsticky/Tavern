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
    public required uint Id { get; set; }
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public string? PosterPath { get; set; }
    public string? PosterFileName { get; set; }
    public required string DutchDescription { get; set; }
    public required string EnglishDescription { get; set; }
    public required DateTimeOffset DateTimeStart { get; set; }
    public required DateTimeOffset DateTimeEnd { get; set; }
    public DateTimeOffset? UnenrollmentDeadline { get; set; }
    public DateTimeOffset? EnrollmentDeadline { get; set; }
    public required string Location { get; set; }
    public uint? ParticipantLimit { get; set; }
    public uint? OrganizerId { get; set; }
    public required bool ShowInKoala { get; set; }
    public required bool ShowOnWebsite { get; set; }
    public required bool IsEnrollable { get; set; }
    public required bool AreParticipantsVisible { get; set; }
    public required bool IsAdultOnly { get; set; }
    public TargetAudience AllowedAudience { get; set; }
    public uint? VatRate { get; set; }
    public string? GLAccountId { get; set; }
    public string? CostCenterId { get; set; }
    public string? CostUnitId { get; set; }
    
    public required List<EnrollmentSummaryDTO> Enrollments { get; set; } = new();

    public required List<GetSpecificationQuestionResponseDTO> SpecificationQuestions { get; set; } = new();

    public required DateTimeOffset? PaymentDeadline { get; set; }
}

public class PostActivityDTO : BaseActivityDTO<SpecificationQuestionDTO>
{
}

public class PutActivityDTO : BaseActivityDTO<UpdateSpecificationQuestionDTO>
{
}
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class PostSpecificationAnswerDTO
{
    public uint QuestionId { get; set; }

    [StringLength(1000)]
    [Required(AllowEmptyStrings = false)]
    public string Answer { get; set; } = string.Empty;
}

public class SpecificationAnswerResponseDTO
{
    public required uint QuestionId { get; set; }
    public required uint AnswerId { get; set; }
    public required string Answer { get; set; }
}
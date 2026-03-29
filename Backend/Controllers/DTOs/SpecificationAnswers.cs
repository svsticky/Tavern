using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class PostSpecificationAnswerDTO
{
    public uint QuestionId { get; set; }

    [StringLength(1000)]
    [Required(AllowEmptyStrings = false)]
    public string Answer { get; set; } = string.Empty;
}
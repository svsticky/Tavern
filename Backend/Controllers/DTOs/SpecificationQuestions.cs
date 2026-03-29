using Backend.Models;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class SpecificationQuestionDTO
{    
    /// <inheritdoc cref="SpecificationQuestion.QuestionDutch"/>
    [StringLength(200)]
    [Required(AllowEmptyStrings = false)]
    public required string QuestionDutch { get; set; }
    
    /// <inheritdoc cref="SpecificationQuestion.QuestionEnglish"/>
    [StringLength(200)]
    [Required(AllowEmptyStrings = false)]
    public required string QuestionEnglish { get; set; }
    
    /// <inheritdoc cref="SpecificationQuestion.Type"/>
    public QuestionType Type { get; set; }

    /// <inheritdoc cref="SpecificationQuestion.IsMandatory"/>
    public bool IsMandatory { get; set; }

    /// <inheritdoc cref="SpecificationQuestion.Options"/>
    public bool IsPublic { get; set; }

    /// <inheritdoc cref="SpecificationQuestion.Options"/>
    public List<string>? Options { get; set; }
}

public class PostSpecificationQuestionDTO : SpecificationQuestionDTO
{
    /// <inheritdoc cref="SpecificationQuestion.ActivityId"/>
    public required uint ActivityId { get; set; }
}
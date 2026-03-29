using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public enum QuestionType
{
    String,
    Boolean,
    Number,
    Date,
    DateTime,
    MultipleChoice
}

public class SpecificationQuestion
{
    /// <summary>
    /// The unique identifier of a specification question, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The unique identifier of the activity to which this specification question belongs.
    /// </summary>
    public uint ActivityId { get; set; }
    
    /// <summary>
    /// The question in Dutch.
    /// </summary>
    [StringLength(200)]
    [Required(AllowEmptyStrings = false)]
    public required string QuestionDutch { get; set; }
    
    /// <summary> 
    /// The question in English.
    /// </summary>
    [StringLength(200)]
    [Required(AllowEmptyStrings = false)]
    public required string QuestionEnglish { get; set; }
    
    /// <summary>
    /// The type of the specification question, determining the content and format of the answers provided for this question.
    /// </summary>
    public QuestionType Type { get; set; }

    /// <summary>
    /// Whether providing an answer for this specification question is mandatory when enrolling for the associated activity.
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// Whether the answers provided for this specification question are visible to other members who enrolled for the same activity.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// The activity to which this specification question belongs.
    /// </summary>
    [JsonIgnore]
    public required Activity Activity { get; set; }

    /// <summary>
    /// The collection of answers provided for this specification question. The content and format of these answers depend on the type of this specification question.
    /// </summary>
    [JsonIgnore] public virtual ICollection<SpecificationAnswer> Answers { get; set; } = new List<SpecificationAnswer>();

    /// <summary>
    /// The options for this specification question, applicable only if the type of this specification question is MultipleChoice. The content of this field is a list of strings representing the available options seperated by semicolons. For example: "Option 1;Option 2;Option 3".
    /// </summary>
    public string? Options { get; set; }
}
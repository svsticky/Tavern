using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public enum QuestionType
{
    String,
    Boolean,
    Number,
    Date,
    DateTime
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
    public required string QuestionDutch { get; set; }
    
    /// <summary> 
    /// The question in English.
    /// </summary>
    [StringLength(200)]
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
}
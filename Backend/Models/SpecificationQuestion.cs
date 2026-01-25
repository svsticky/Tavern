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
    public uint Id { get; set; }
    public uint ActivityId { get; set; }
    
    [StringLength(200)]
    public required string QuestionDutch { get; set; }
    
    [StringLength(200)]
    public required string QuestionEnglish { get; set; }
    
    public QuestionType Type { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsPublic { get; set; }

    [JsonIgnore]
    public required Activity Activity { get; set; }
}
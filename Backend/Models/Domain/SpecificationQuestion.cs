using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Domain;

/// <summary>
/// The type of a specification question, determining the content and format of the answers provided for this question. The QuestionType enum defines the possible types of specification questions, including String, Boolean, Number, Date, DateTime, and MultipleChoice. Each type corresponds to a specific format for the answers that can be provided for that question, allowing for better organization and management of different types of questions and their associated answers within the system.
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// Indicates that the specification question is of type String, meaning that the answers provided for this question should be in the form of text strings. This type allows for open-ended responses where members can provide detailed information or explanations in their answers, making it suitable for questions that require more descriptive or narrative responses.
    /// </summary>
    String,

    /// <summary>
    /// Indicates that the specification question is of type Boolean, meaning that the answers provided for this question should be in the form of true/false or yes/no responses. This type is suitable for questions that require a simple binary response, allowing for quick and straightforward answers that can be easily analyzed and categorized within the system.
    /// </summary>
    Boolean,

    /// <summary>
    /// Indicates that the specification question is of type Number, meaning that the answers provided for this question should be in the form of numerical values. This type is suitable for questions that require quantitative responses, allowing for the collection of data that can be used for calculations, comparisons, or statistical analysis within the system.
    /// </summary>
    Number,

    /// <summary>
    /// Indicates that the specification question is of type Date, meaning that the answers provided for this question should be in the form of date values. This type is suitable for questions that require responses related to specific dates, such as birthdates, event dates, or other relevant date information that can be used for scheduling, age verification, or other date-related functionalities within the system.
    /// </summary>
    Date,

    /// <summary>
    /// Indicates that the specification question is of type DateTime, meaning that the answers provided for this question should be in the form of date and time values. This type is suitable for questions that require responses related to specific dates and times, such as appointment scheduling, event timing, or other relevant date and time information that can be used for scheduling, reminders, or other date and time-related functionalities within the system.
    /// </summary>
    DateTime,

    /// <summary>
    /// Indicates that the specification question is of type MultipleChoice, meaning that the answers provided for this question should be selected from a predefined set of options. This type is suitable for questions that require responses to be chosen from a specific list of choices, allowing for easier analysis and categorization of responses based on the selected options. The available options for this type of question can be defined in the Options property of the SpecificationQuestion entity, providing flexibility in the range of choices that can be offered to members when answering this type of question.
    /// </summary>
    MultipleChoice
}

/// <summary>
/// Represents a specification question that is associated with an activity. This entity is used to define specific questions that members are required or allowed to answer when enrolling for an activity. Each SpecificationQuestion is linked to a specific Activity and can have various properties such as the question text in both Dutch and English, the type of the question (e.g., string, boolean, multiple choice), whether answering the question is mandatory for enrollment, and whether the answers provided for this question are visible to other members who enrolled for the same activity. Additionally, if the question type is MultipleChoice, the Options property can be used to define the available options for that question. This entity plays a crucial role in facilitating the collection of relevant information from members during the enrollment process for activities within the system.
/// </summary>
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
    public Activity Activity { get; set; } = null!;

    /// <summary>
    /// The collection of answers provided for this specification question. The content and format of these answers depend on the type of this specification question.
    /// </summary>
    [JsonIgnore] public virtual ICollection<SpecificationAnswer> Answers { get; set; } = new List<SpecificationAnswer>();

    /// <summary>
    /// The options for this specification question, applicable only if the type of this specification question is MultipleChoice. The content of this field is a list of strings representing the available options seperated by semicolons. For example: "Option 1;Option 2;Option 3".
    /// </summary>
    public string? Options { get; set; }
}

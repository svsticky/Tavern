using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Domain;

/// <summary>
/// Represents an answer provided for a specification question by a member. This entity is used to capture and manage the responses given by members to specific questions that are part of the activity enrollment process. Each SpecificationAnswer is associated with a specific SpecificationQuestion and a Member, allowing for better organization and retrieval of answers based on the related question and member. The Answer property holds the actual response provided by the member, which can be of various formats depending on the type of the associated specification question (e.g., text, multiple choice, etc.). This entity plays a crucial role in facilitating the collection and management of member responses during the enrollment process for activities within the system.
/// </summary>
public class SpecificationAnswer
{
    /// <summary>
    /// The unique identifier of a specification answer, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The unique identifier of the specification question for which this answer is provided.
    /// </summary>
    public uint SpecificationQuestionId { get; set; }

    /// <summary>
    /// The unique identifier of the member for which this answer is provided.
    /// </summary>
    public required Guid MemberId { get; set; }

    /// <summary>
    /// The member for which this answer is provided.
    /// </summary>
    public Member Member { get; set; } = null!;

    /// <summary>
    /// The answer provided for the specification question. The content and format of this answer depend on the type of the associated specification question.
    /// </summary>
    [StringLength(1000)]
    [Required(AllowEmptyStrings = false)]
    public required string Answer { get; set; }

    /// <summary>
    /// The specification question for which this answer is provided.
    /// </summary>
    public SpecificationQuestion Question { get; set; } = null!;

    /// <summary>
    /// The enrollment for which this answer is provided.
    /// </summary>
    [JsonIgnore] public Enrollment Enrollment { get; set; } = null!;
}

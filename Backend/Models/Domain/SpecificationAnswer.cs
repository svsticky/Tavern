using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Domain;

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
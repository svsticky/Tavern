using Newtonsoft.Json;

namespace Backend.Models;

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
    /// The unique identifier of the enrollment for which this answer is provided.
    /// </summary>
    public uint EnrollmentId { get; set; }

    /// <summary>
    /// The answer provided for the specification question. The content and format of this answer depend on the type of the associated specification question.
    /// </summary>
    public required string Answer { get; set; }

    /// <summary>
    /// The specification question for which this answer is provided.
    /// </summary>
    public required SpecificationQuestion Question { get; set; }

    /// <summary>
    /// The enrollment for which this answer is provided.
    /// </summary>
    [JsonIgnore] public required Enrollment Enrollment { get; set; }
}
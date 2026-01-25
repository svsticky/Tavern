using Newtonsoft.Json;

namespace Backend.Models;

public class SpecificationAnswer
{
    public uint Id { get; set; }
    public uint SpecificationQuestionId { get; set; }
    public uint EnrollmentId { get; set; }

    public required string Answer { get; set; }

    public required SpecificationQuestion Question { get; set; }

    [JsonIgnore] public required Enrollment Enrollment { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a specification answer, containing the necessary information for creating a new specification answer, including the question ID and the answer text. The PostSpecificationAnswerDTO is used to transfer data from the client to the server when creating a new specification answer, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostSpecificationAnswerDTO
{
    /// <inheritdoc cref="Models.SpecificationAnswer.QuestionId"/>
    public uint QuestionId { get; set; }

    /// <inheritdoc cref="Models.SpecificationAnswer.Answer"/>
    [StringLength(1000)]
    [Required(AllowEmptyStrings = false)]
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// Represents the response DTO for a specification answer, containing all relevant information about the specification answer, including its properties and any associated data. The SpecificationAnswerResponseDTO is used to transfer comprehensive specification answer data from the server to the client when retrieving specification answer information, allowing for a complete representation of the specification answer's details in the response payload. The SpecificationAnswerResponseDTO can include properties such as the question ID, answer ID, and the answer text, providing a comprehensive view of the specification answer data for the client application.
/// </summary>
public class SpecificationAnswerResponseDTO
{
    /// <inheritdoc cref="Models.SpecificationAnswer.QuestionId"/>
    public required uint QuestionId { get; set; }

    /// <inheritdoc cref="Models.SpecificationAnswer.AnswerId"/>
    public required uint AnswerId { get; set; }

    /// <inheritdoc cref="Models.SpecificationAnswer.Answer"/>
    public required string Answer { get; set; }
}
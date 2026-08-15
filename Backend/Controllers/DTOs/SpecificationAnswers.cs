using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a specification answer, containing the necessary information for creating a new specification answer, including the question ID and the answer text. The PostSpecificationAnswerDTO is used to transfer data from the client to the server when creating a new specification answer, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostSpecificationAnswerDTO
{
    /// <inheritdoc cref="Models.Domain.SpecificationAnswer.SpecificationQuestionId"/>
    public uint QuestionId { get; set; }

    /// <inheritdoc cref="Models.Domain.SpecificationAnswer.Answer"/>
    [StringLength(1000)]
    [Required(AllowEmptyStrings = false)]
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// Represents the response DTO for a specification answer, containing all relevant information about the specification answer, including its properties and any associated data. The SpecificationAnswerResponseDTO is used to transfer comprehensive specification answer data from the server to the client when retrieving specification answer information, allowing for a complete representation of the specification answer's details in the response payload. The SpecificationAnswerResponseDTO can include properties such as the question ID, answer ID, and the answer text, providing a comprehensive view of the specification answer data for the client application.
/// </summary>
public class SpecificationAnswerResponseDTO
{
    /// <inheritdoc cref="Models.Domain.SpecificationAnswer.SpecificationQuestionId"/>
    public required uint QuestionId { get; set; }

    /// <inheritdoc cref="Models.Domain.SpecificationAnswer.Id"/>
    public required uint AnswerId { get; set; }

    /// <inheritdoc cref="Models.Domain.SpecificationAnswer.Answer"/>
    public required string Answer { get; set; }

    /// <summary>
    /// Projects a SpecificationAnswer entity into a SpecificationAnswerResponseDTO, including the question ID, answer ID, and the answer itself. This projection is used to transform the data from the SpecificationAnswer model into a format that is suitable for API responses, ensuring that the relevant information about the specification answer is included while maintaining appropriate access control based on the user's role within the system. The ToDto method centralizes the logic for mapping SpecificationAnswer entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling specification answer-related data transformations for API responses.
    /// </summary>
    /// <returns>An expression that projects a SpecificationAnswer entity into a SpecificationAnswerResponseDTO.</returns>
    public static Expression<Func<SpecificationAnswer, SpecificationAnswerResponseDTO>> ToDto()
    {
        return sa => new SpecificationAnswerResponseDTO
        {
            QuestionId = sa.SpecificationQuestionId,
            AnswerId = sa.Id,
            Answer = sa.Answer
        };
    }
}

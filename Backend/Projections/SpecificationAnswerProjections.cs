using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

/// <summary>
/// The SpecificationAnswerProjections class provides a method to project a SpecificationAnswer entity into a SpecificationAnswerResponseDTO. This projection is used to transform the data from the SpecificationAnswer model into a format that is suitable for API responses, ensuring that the relevant information about the specification answer is included while maintaining appropriate access control based on the user's role within the system. The ToDto method centralizes the logic for mapping SpecificationAnswer entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling specification answer-related data transformations for API responses.
/// </summary>
public static class SpecificationAnswerProjections
{
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

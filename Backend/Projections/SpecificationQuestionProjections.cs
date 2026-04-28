using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

/// <summary>
/// The SpecificationQuestionProjections class provides a method to project a SpecificationQuestion entity into a GetSpecificationQuestionResponseDTO. This projection is used to transform the data from the SpecificationQuestion model into a format that is suitable for API responses, ensuring that the relevant information about the specification question is included while maintaining appropriate access control based on the user's role within the system. The ToDto method centralizes the logic for mapping SpecificationQuestion entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling specification question-related data transformations for API responses.
/// </summary>
public static class SpecificationQuestionProjections
{
    /// <summary>
    /// Projects a SpecificationQuestion entity into a GetSpecificationQuestionResponseDTO, including the question ID, Dutch and English versions of the question, type, mandatory and public flags, and options if available. This projection is used to transform the data from the SpecificationQuestion model into a format that is suitable for API responses, ensuring that the relevant information about the specification question is included while maintaining appropriate access control based on the user's role within the system. The ToDto method centralizes the logic for mapping SpecificationQuestion entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling specification question-related data transformations for API responses.
    /// </summary>
    /// <returns>An expression that projects a SpecificationQuestion entity into a GetSpecificationQuestionResponseDTO.</returns>
    public static Expression<Func<SpecificationQuestion, GetSpecificationQuestionResponseDTO>> ToDto()
    {
        return sq => new GetSpecificationQuestionResponseDTO
        {
            Id = sq.Id,
            QuestionDutch = sq.QuestionDutch,
            QuestionEnglish = sq.QuestionEnglish,
            Type = sq.Type,
            IsMandatory = sq.IsMandatory,
            IsPublic = sq.IsPublic,
            Options = sq.Options != null
                ? sq.Options.Split(new[] { ';' }, StringSplitOptions.None).ToList()
                : null
        };
    }
}

using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class SpecificationAnswerProjections
{
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

using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class SpecificationQuestionProjections
{
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

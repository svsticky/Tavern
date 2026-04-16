using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Backend.Validators;

public static class EnrollmentValidator
{
    public static void ValidateAnswers(
        IEnumerable<PostSpecificationAnswerDTO> providedAnswers,
        IEnumerable<SpecificationQuestion> questions)
    {
        var validQuestionIds = questions.Select(q => q.Id).ToHashSet();
        var mandatoryQuestionIds = questions.Where(q => q.IsMandatory).Select(q => q.Id).ToList();
        var providedQuestionIds = providedAnswers.Select(a => a.QuestionId).ToList();

        if (providedAnswers.Any(a => !validQuestionIds.Contains(a.QuestionId)))
            throw new ArgumentException("Invalid specification question(s).");

        if (mandatoryQuestionIds.Except(providedQuestionIds).Any())
            throw new ArgumentException("Missing mandatory answers.");

        foreach (var answer in providedAnswers)
        {
            var question = questions.First(q => q.Id == answer.QuestionId);

            AnswerValidator.IsValidAnswer(answer.Answer, question.Type, question.Options);
        }
    }
}
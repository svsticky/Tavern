using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;

namespace Backend.Validators;

/// <summary>
/// Provides validation helpers for enrollment requests and specification answers.
/// </summary>
public static class EnrollmentValidator
{
    /// <summary>
    /// Validates whether a member is allowed to enroll for an activity.
    /// </summary>
    /// <param name="providedAnswers">The provided specification answers.</param>
    /// <param name="member">The member attempting to enroll.</param>
    /// <param name="activity">The target activity.</param>
    /// <param name="isBoardMember">Whether the acting user is a board member.</param>
    /// <param name="_paymentValidationService">The payment validation service.</param>
    /// <exception cref="ArgumentException">Thrown when enrollment requirements are not met.</exception>
    public static void ValidateEnrollment(IEnumerable<PostSpecificationAnswerDTO>? providedAnswers, Member member, Activity activity, bool isBoardMember, IPaymentValidationService _paymentValidationService)
    {
        if (!_paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id))
            throw new ArgumentException("Member does not have a paid membership payment.");

        if (member.Suspended)
            throw new ArgumentException("Member is suspended and cannot enroll in activities.");

        if (activity.Enrollments.Any(e => e.MemberId == member.Id))
            throw new ArgumentException("Member is already enrolled (or on waiting list).");

        if(activity.IsAdultOnly && member.DateOfBirth.Date >= activity.DateTimeStart.Date)
            throw new ArgumentException("Member does not meet the age requirement for this activity.");

        ValidateAnswers(providedAnswers, activity.SpecificationQuestions, isBoardMember);
    }

    /// <summary>
    /// Validates provided specification answers against activity questions.
    /// </summary>
    /// <param name="providedAnswers">The provided specification answers.</param>
    /// <param name="questions">The activity specification questions.</param>
    /// <param name="isBoard">Whether mandatory-answer checks can be bypassed.</param>
    /// <exception cref="ArgumentException">Thrown when answers are missing or invalid.</exception>
    public static void ValidateAnswers(
        IEnumerable<PostSpecificationAnswerDTO>? providedAnswers,
        IEnumerable<SpecificationQuestion> questions, 
        bool isBoard)
    {
        var validQuestionIds = questions.Select(q => q.Id).ToHashSet();
        var mandatoryQuestionIds = questions.Where(q => q.IsMandatory).Select(q => q.Id).ToList();
        var providedQuestionIds = providedAnswers?.Select(a => a.QuestionId).ToList() ?? [];

        if (!isBoard && mandatoryQuestionIds.Except(providedQuestionIds).Any())
            throw new ArgumentException("Missing mandatory answers.");

        if(providedAnswers == null)
            return;

        if (providedAnswers.Any(a => !validQuestionIds.Contains(a.QuestionId)))
            throw new ArgumentException("Invalid specification question(s).");

        foreach (var answer in providedAnswers)
        {
            var question = questions.First(q => q.Id == answer.QuestionId);

            AnswerValidator.IsValidAnswer(answer.Answer, question.Type, question.Options);
        }
    }
}

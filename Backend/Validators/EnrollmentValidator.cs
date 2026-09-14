using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Utils;

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
        if (!_paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id) && !_paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id))
            throw new ArgumentException("Member does not have a paid membership payment.");

        if (member.Suspended)
            throw new ArgumentException("Member is suspended and cannot enroll in activities.");

        if (activity.Enrollments.Any(e => e.MemberId == member.Id))
            throw new ArgumentException("Member is already enrolled (or on waiting list).");

        string timezoneId = Environment.GetEnvironmentVariable("AssociationTimeZone") ?? "Europe/Amsterdam";
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        DateTime activityStartDateInTimeZone = TimeZoneInfo.ConvertTime(activity.DateTimeStart, tz).Date;

        if (activity.IsAdultOnly && member.DateOfBirth.AddYears(18).Date > activityStartDateInTimeZone)
            throw new ArgumentException("Member does not meet the age requirement for this activity.");

        ValidateAnswers(providedAnswers, activity, isBoardMember);
    }

    /// <summary>
    /// Validates provided specification answers against an activity's questions. Questions whose answer
    /// deadline (see <see cref="QuestionDeadlineHelper"/>) has passed can no longer be answered for the
    /// first time and are no longer mandatory for members who have not answered them yet. An answer for
    /// such a question is only accepted if it is identical to the value already on record for that
    /// question, as given by <paramref name="existingAnswers"/> - this lets a caller resubmit an
    /// unchanged, now-locked answer (e.g. as part of a bulk update) without it being rejected.
    /// </summary>
    /// <param name="providedAnswers">The provided specification answers.</param>
    /// <param name="activity">The activity the questions belong to.</param>
    /// <param name="isBoard">Whether mandatory-answer and deadline checks can be bypassed.</param>
    /// <param name="existingAnswers">The member's currently recorded answers, keyed by question ID, if any.</param>
    /// <exception cref="ArgumentException">Thrown when answers are missing, invalid, or no longer answerable.</exception>
    public static void ValidateAnswers(
        IEnumerable<PostSpecificationAnswerDTO>? providedAnswers,
        Activity activity,
        bool isBoard,
        IReadOnlyDictionary<uint, string>? existingAnswers = null)
    {
        var now = DateTimeOffset.UtcNow;
        var questions = activity.SpecificationQuestions.ToList();
        var validQuestionIds = questions.Select(q => q.Id).ToHashSet();

        var answerableQuestionIds = isBoard
            ? validQuestionIds
            : questions.Where(q => QuestionDeadlineHelper.IsAnswerable(q, activity, now)).Select(q => q.Id).ToHashSet();

        var mandatoryQuestionIds = questions.Where(q => q.IsMandatory && answerableQuestionIds.Contains(q.Id)).Select(q => q.Id).ToList();
        var providedQuestionIds = providedAnswers?.Select(a => a.QuestionId).ToList() ?? [];

        if (!isBoard && mandatoryQuestionIds.Except(providedQuestionIds).Any())
            throw new ArgumentException("Missing mandatory answers.");

        if (providedAnswers == null)
            return;

        if (providedAnswers.Any(a => !validQuestionIds.Contains(a.QuestionId)))
            throw new ArgumentException("Invalid specification question(s).");

        if (!isBoard)
        {
            foreach (var answer in providedAnswers.Where(a => !answerableQuestionIds.Contains(a.QuestionId)))
            {
                bool isUnchanged = existingAnswers != null
                    && existingAnswers.TryGetValue(answer.QuestionId, out var existingValue)
                    && existingValue == answer.Answer;

                if (!isUnchanged)
                    throw new ArgumentException("Cannot answer or change this question after its answer deadline has passed.");
            }
        }

        foreach (var answer in providedAnswers)
        {
            var question = questions.First(q => q.Id == answer.QuestionId);

            AnswerValidator.IsValidAnswer(answer.Answer, question.Type, question.Options);
        }
    }

    /// <summary>
    /// Validates a wholesale replacement of an enrollment's specification answer entities (as used by JSON
    /// Patch on the enrollment's <c>/specificationanswers</c> field) against each question's answer
    /// deadline, mirroring <see cref="ValidateAnswers"/> for the entity-based case. An answer for a
    /// question whose deadline has passed is only accepted if it is unchanged from <paramref name="oldAnswers"/>.
    /// </summary>
    /// <param name="oldAnswers">The specification answers on record before the replacement.</param>
    /// <param name="newAnswers">The specification answers after the replacement.</param>
    /// <param name="questionsById">The activity's specification questions, keyed by ID.</param>
    /// <param name="activity">The activity the questions belong to.</param>
    /// <param name="isBoard">Whether deadline checks can be bypassed.</param>
    /// <exception cref="ArgumentException">Thrown when an answer for a no-longer-answerable question has changed.</exception>
    public static void ValidateAnswerDeadlines(
        IEnumerable<SpecificationAnswer> oldAnswers,
        IEnumerable<SpecificationAnswer> newAnswers,
        IReadOnlyDictionary<uint, SpecificationQuestion> questionsById,
        Activity activity,
        bool isBoard)
    {
        if (isBoard)
            return;

        var now = DateTimeOffset.UtcNow;
        var oldByQuestion = oldAnswers.ToDictionary(a => a.SpecificationQuestionId, a => a.Answer);

        foreach (var answer in newAnswers)
        {
            if (!questionsById.TryGetValue(answer.SpecificationQuestionId, out var question))
                continue;

            if (QuestionDeadlineHelper.IsAnswerable(question, activity, now))
                continue;

            bool isUnchanged = oldByQuestion.TryGetValue(answer.SpecificationQuestionId, out var oldValue)
                && oldValue == answer.Answer;

            if (!isUnchanged)
                throw new ArgumentException("Cannot answer or change this question after its answer deadline has passed.");
        }
    }
}

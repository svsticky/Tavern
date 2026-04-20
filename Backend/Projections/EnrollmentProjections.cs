using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class EnrollmentProjections
{
    public static Expression<Func<Enrollment, EnrollmentResponseDTO>> ToDto(Guid userId, bool isBoard)
    {
        return e => new EnrollmentResponseDTO
        {
            IsOnWaitingList = e.IsOnWaitingList,
            Member = new MemberResponseDTO
            {
                Id = isBoard || e.MemberId == userId ? e.MemberId : null,
                FirstName = e.Activity.AreParticipantsVisible || isBoard ? e.Member.FirstName : null,
                LastName = e.Activity.AreParticipantsVisible || isBoard ? e.Member.LastName : null,
                ProfilePicturePath = e.Activity.AreParticipantsVisible || isBoard ? e.Member.ProfilePicturePath : null
             },
            SpecificationAnswers = e.SpecificationAnswers
                .Where(sa => isBoard || sa.MemberId == userId || sa.Question.IsPublic && sa.Question.Activity.AreParticipantsVisible)
                .Select(sa => new SpecificationAnswerResponseDTO
                {
                    QuestionId = sa.SpecificationQuestionId,
                    AnswerId = sa.Id,
                    Answer = sa.Answer
                }).ToList(),
            Price = isBoard ? e.Price : null,
            Activity = ActivityProjections.ToDto(userId, isBoard, false).Compile()(e.Activity)
        };
    }
}
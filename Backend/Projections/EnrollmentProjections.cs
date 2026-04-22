using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class EnrollmentProjections
{
    public static Expression<Func<Enrollment, EnrollmentResponseDTO>> ToDto(Guid userId, bool isBoard, bool includeActivity = false)
    {
        return e => new EnrollmentResponseDTO
        {
            IsOnWaitingList = e.IsOnWaitingList,
            Member = MemberProjections.ToDto(userId, isBoard).Compile()(e.Member),
            SpecificationAnswers = e.SpecificationAnswers
                .Where(sa => isBoard || sa.MemberId == userId || sa.Question.IsPublic && sa.Question.Activity.AreParticipantsVisible)
                .Select(sa => SpecificationAnswerProjections.ToDto().Compile()(sa)).ToList(),
            Price = isBoard ? e.Price : null,
            Activity = includeActivity ? ActivityProjections.ToDto(userId, isBoard).Compile()(e.Activity) : null!
        };
    }
}
using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

/// <summary>
/// The EnrollmentProjections class provides a method to project an Enrollment entity into an EnrollmentResponseDTO. This projection is used to transform the data from the Enrollment model into a format that is suitable for API responses, including related member information, specification answers, and optionally the associated activity. The ToDto method takes a user ID, a boolean indicating whether the requester is a board member, and an optional boolean to include activity information, allowing it to conditionally include certain information based on the user's role and the context of the request. This class helps to centralize the logic for mapping Enrollment entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling enrollment-related data transformations for API responses.
/// </summary>
public static class EnrollmentProjections
{
    /// <summary>
    /// Projects an Enrollment entity into an EnrollmentResponseDTO, including related member information, specification answers, and optionally the associated activity. The method takes a user ID, a boolean indicating whether the requester is a board member, and an optional boolean to include activity information, allowing it to conditionally include certain information based on the user's role and the context of the request. This projection is used to transform the data from the Enrollment model into a format that is suitable for API responses, ensuring that the relevant information is included while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to project the enrollment.</param>
    /// <param name="isBoard">A boolean indicating whether the requester is a board member.</param>
    /// <param name="includeActivity">A boolean indicating whether to include activity information.</param>
    /// <returns>An expression that projects an Enrollment entity into an EnrollmentResponseDTO.</returns>
    public static Expression<Func<Enrollment, EnrollmentResponseDTO>> ToDto(Guid userId, bool isBoard, bool includeActivity = true)
    {
        return e => new EnrollmentResponseDTO
        {
            IsOnWaitingList = e.IsOnWaitingList,
            Member = e.Member == null ? null! :  MemberProjections.ToDto(userId, isBoard).Compile()(e.Member),
            SpecificationAnswers = e.SpecificationAnswers == null ? new List<SpecificationAnswerResponseDTO>() : e.SpecificationAnswers
                .Where(sa => isBoard || sa.MemberId == userId || sa.Question.IsPublic && sa.Question.Activity.AreParticipantsVisible)
                .Select(sa => SpecificationAnswerProjections.ToDto().Compile()(sa)).ToList(),
            Price = isBoard ? e.Price : null,
            Activity = e.Activity == null ? null! : includeActivity ? ActivityProjections.ToDto(userId, isBoard).Compile()(e.Activity) : null!
        };
    }
}
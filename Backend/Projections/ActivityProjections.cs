using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

/// <summary>
/// The ActivityProjections class provides a method to project an Activity entity into an ActivityResponseDTO. This projection is used to transform the data from the Activity model into a format that is suitable for API responses, including related enrollments and specification questions. The ToDto method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information based on the user's role. This class helps to centralize the logic for mapping Activity entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling activity-related data transformations for API responses.
/// </summary>
public static class ActivityProjections
{
    /// <summary>
    /// Projects an Activity entity into an ActivityResponseDTO, including related enrollments and specification questions. The method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information based on the user's role. This projection is used to transform the data from the Activity model into a format that is suitable for API responses, ensuring that the relevant information is included while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to project the activity.</param>
    /// <param name="isBoard">A boolean indicating whether the requester is a board member.</param>
    /// <returns>An expression that projects an Activity entity into an ActivityResponseDTO.</returns>
    public static Expression<Func<Activity, ActivityResponseDTO>> ToDto(Guid userId, bool isBoard)
    {
        return a => new ActivityResponseDTO
        {
            Id = a.Id,
            Name = a.Name,
            Price = a.Price,
            PosterPath = a.PosterPath,
            PosterFileName = a.PosterFileName,
            DutchDescription = a.DutchDescription,
            EnglishDescription = a.EnglishDescription,
            DateTimeStart = a.DateTimeStart,
            DateTimeEnd = a.DateTimeEnd,
            UnenrollmentDeadline = a.UnenrollmentDeadline,
            EnrollmentDeadline = a.EnrollmentDeadline,
            EnrollOpenDate = a.EnrollOpenDate,
            Location = a.Location,
            ParticipantLimit = a.ParticipantLimit,
            OrganizerId = a.OrganizerId,
            ShowInKoala = a.ShowInKoala,
            ShowOnWebsite = a.ShowOnWebsite,
            IsEnrollable = a.IsEnrollable,
            AreParticipantsVisible = a.AreParticipantsVisible,
            IsAdultOnly = a.IsAdultOnly,
            IsWeeklyDrinks = a.IsWeeklyDrinks,
            AllowedAudience = a.AllowedAudience,
            VatRate = a.VatRate,
            GLAccountId = a.GLAccountId,
            CostCenterId = a.CostCenterId,
            CostUnitId = a.CostUnitId,

            Enrollments = a.AreParticipantsVisible || isBoard ? a.Enrollments.Select(e => EnrollmentProjections.ToDto(userId, isBoard, false).Compile()(e)).ToList() : new List<EnrollmentResponseDTO>(),

            SpecificationQuestions = a.SpecificationQuestions.Select(q => SpecificationQuestionProjections.ToDto().Compile()(q)).ToList(),

            PaymentDeadline = isBoard ? a.PaymentDeadline : default,
            IsOpenForPayment = a.IsOpenForPayment
        };
    }
}
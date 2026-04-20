using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class ActivityProjections
{
    public static Expression<Func<Activity, ActivityResponseDTO>> ToDto(Guid userId, bool isBoard, bool includeEnrollments = true)
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

            Enrollments = a.Enrollments.Select(e => EnrollmentProjections.ToDto(userId, isBoard).Compile()(e)).ToList(),

            SpecificationQuestions = a.SpecificationQuestions.Select(q => new GetSpecificationQuestionResponseDTO
            {
                Id = q.Id,
                QuestionDutch = q.QuestionDutch,
                QuestionEnglish = q.QuestionEnglish,
                Type = q.Type,
                IsMandatory = q.IsMandatory,
                IsPublic = q.IsPublic,
                Options = q.Options != null
                    ? q.Options.Split(new[] { ';' }, StringSplitOptions.None).ToList()
                    : null
            }).ToList(),

            PaymentDeadline = isBoard ? a.PaymentDeadline : default,
            IsOpenForPayment = a.IsOpenForPayment
        };
    }
}
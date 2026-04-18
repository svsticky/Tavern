using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class ActivityProjections
{
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
            AllowedAudience = a.AllowedAudience,
            VatRate = a.VatRate,
            GLAccountId = a.GLAccountId,
            CostCenterId = a.CostCenterId,
            CostUnitId = a.CostUnitId,

            Enrollments = a.Enrollments.Select(e => new EnrollmentSummaryDTO
            {
                IsOnWaitingList = e.IsOnWaitingList,
                Member = new MemberSummaryDTO
                {
                    Id = e.MemberId == userId ? e.MemberId : null,
                    FirstName = a.AreParticipantsVisible || isBoard ? e.Member.FirstName : null,
                    LastName = a.AreParticipantsVisible || isBoard ? e.Member.LastName : null,
                    ProfilePicturePath = a.AreParticipantsVisible || isBoard ? e.Member.ProfilePicturePath : null
                },
                SpecificationAnswers = e.SpecificationAnswers
                    .Where(sa => isBoard || sa.MemberId == userId || sa.Question.IsPublic)
                    .Select(sa => new SpecificationAnswerResponseDTO
                    {
                        QuestionId = sa.SpecificationQuestionId,
                        AnswerId = sa.Id,
                        Answer = sa.Answer
                    }).ToList(),
                Price = isBoard ? e.Price : null,
                ActivityId = a.Id,
                MemberId = isBoard || e.MemberId == userId ? e.MemberId : null
            }).ToList(),

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
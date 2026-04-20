using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Newtonsoft.Json;

namespace Backend.Validators;

public static class ActivityValidator
{
    public static void ValidateCreateRequest(PostActivityDTO dto, Guid userId, IPermissionService permissionService)
    {
        ValidateTimeRange(dto.DateTimeStart, dto.DateTimeEnd);
        ValidateParticipantLimit(dto.ParticipantLimit);
        ValidatePosterIfProvided(dto.Poster);

        if (dto.ShowInKoala || dto.ShowOnWebsite || dto.PaymentDeadline != null)
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
    }

    public static void NormalizeCreateRequest(PostActivityDTO dto)
    {
        if (dto.IsEnrollable)
        {
            dto.EnrollOpenDate = null;
        }
    }

    public static void ValidateUpdateRequest(Activity activity, PutActivityDTO dto, Guid userId, IPermissionService permissionService)
    {
        ValidateTimeRange(dto.DateTimeStart, dto.DateTimeEnd);
        ValidateParticipantLimit(dto.ParticipantLimit);
        ValidatePosterIfProvided(dto.Poster);

        if (activity.ShowInKoala || activity.ShowOnWebsite || dto.ShowInKoala || dto.ShowOnWebsite || dto.PaymentDeadline != null || dto.EnrollOpenDate != null)
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
    }

    public static List<SpecificationQuestionDTO> ParseCreateQuestions(string? specificationQuestionsJson)
    {
        var questions = string.IsNullOrEmpty(specificationQuestionsJson)
            ? new List<SpecificationQuestionDTO>()
            : JsonConvert.DeserializeObject<List<SpecificationQuestionDTO>>(specificationQuestionsJson);

        return questions ?? throw new ArgumentException("Invalid specification questions format.");
    }

    public static List<UpdateSpecificationQuestionDTO> ParseUpdateQuestions(string? specificationQuestionsJson)
    {
        var questions = string.IsNullOrEmpty(specificationQuestionsJson)
            ? new List<UpdateSpecificationQuestionDTO>()
            : JsonConvert.DeserializeObject<List<UpdateSpecificationQuestionDTO>>(specificationQuestionsJson);

        return questions ?? throw new ArgumentException("Invalid specification questions format.");
    }

    public static void MapSpecificationQuestion(SpecificationQuestion entity, UpdateSpecificationQuestionDTO dto)
    {
        entity.QuestionDutch = dto.QuestionDutch;
        entity.QuestionEnglish = dto.QuestionEnglish;
        entity.Type = dto.Type;
        entity.IsMandatory = dto.IsMandatory;
        entity.IsPublic = dto.IsPublic;
        entity.Options = dto.Options != null && dto.Options.Any()
            ? string.Join(';', dto.Options)
            : null;
    }

    private static void ValidateTimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
            throw new ArgumentException("Activity cannot end before it starts.");
    }

    private static void ValidateParticipantLimit(uint? participantLimit)
    {
        if (participantLimit < 0)
            throw new ArgumentException("Participant limit cannot be negative.");
    }

    private static void ValidatePosterIfProvided(IFormFile? poster)
    {
        if (poster != null)
        {
            ExtensionValidator.ValidatePosterExtension(poster);
        }
    }
}

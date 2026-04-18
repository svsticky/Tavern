namespace Backend.Controllers.DTOs;

public class PostEnrollmentDTO
{
    public uint ActivityId { get; set; }
    public Guid MemberId { get; set; }
    public List<PostSpecificationAnswerDTO>? SpecificationAnswers { get; set; }
}

public class EnrollmentSummaryDTO
{
    public required bool IsOnWaitingList { get; set; }
    public required MemberSummaryDTO Member { get; set; }
    public List<SpecificationAnswerResponseDTO>? SpecificationAnswers { get; set; }
    public decimal? Price { get; set; }
    public required uint ActivityId { get; set; }
    public Guid? MemberId { get; set; }
}
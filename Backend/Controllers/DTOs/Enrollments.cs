namespace Backend.Controllers.DTOs;

public class PostEnrollmentDTO
{
    public uint ActivityId { get; set; }
    public Guid MemberId { get; set; }
    public List<PostSpecificationAnswerDTO>? SpecificationAnswers { get; set; }
}

public class EnrollmentResponseDTO
{
    public required bool IsOnWaitingList { get; set; }
    public required MemberResponseDTO Member { get; set; }
    public List<SpecificationAnswerResponseDTO>? SpecificationAnswers { get; set; }
    public decimal? Price { get; set; }
    public required ActivityResponseDTO Activity { get; set; }
}

public class GetEnrollmentsDTO
{
    public Guid? FromMemberId { get; set; }
}

public class EnrollmentKeyDTO
{
    public uint ActivityId { get; set; }
    public Guid MemberId { get; set; }
}

public class PostEnrollmentResponseDTO
{
    public uint ActivityId { get; set; }
    public Guid MemberId { get; set; }
}
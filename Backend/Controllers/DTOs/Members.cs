using Backend.Models;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class PostMemberDTO
{
    /// <inheritdoc cref="Models.Member.StudentNumber"/>
    public required uint StudentNumber { get; set; }

    /// <inheritdoc cref="Models.Member.FirstName"/>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string FirstName { get; set; }

    /// <inheritdoc cref="Models.Member.LastName"/>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string LastName { get; set; }

    /// <inheritdoc cref="Models.Member.Email"/>
    [StringLength(100)]
    [EmailAddress]
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }

    /// <inheritdoc cref="Models.Member.PhoneNumber"/>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public required string PhoneNumber { get; set; }

    /// <inheritdoc cref="Models.Member.Street"/>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string Street { get; set; }

    /// <inheritdoc cref="Models.Member.HouseNumber"/>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^[1-9][0-9]*\s?([a-zA-Z]|[a-zA-Z]{1,3}bis)?$", ErrorMessage = "Invalid house number format.")]
    public required string HouseNumber { get; set; }

    /// <inheritdoc cref="Models.Member.PostalCode"/>
    [StringLength(10)]
    [Required(AllowEmptyStrings = false)]
    public required string PostalCode { get; set; }

    /// <inheritdoc cref="Models.Member.City"/>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string City { get; set; }

    /// <inheritdoc cref="Models.Member.DateOfBirth"/>
    public required DateTimeOffset DateOfBirth { get; set; }
    /// <inheritdoc cref="Models.Member.ParentPhoneNumber"/>
    
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public string? ParentPhoneNumber { get; set; }

    /// <inheritdoc cref="Models.Member.MailSubscriptions"/>
    public MailSubscriptions MailSubscriptions { get; set; }

    /// <inheritdoc cref="Models.Member.PreferredLanguage"/>
    public required Language PreferredLanguage { get; set; }

    /// <summary>
    /// Studies where the member is enrolled.
    /// </summary>
    public List<PostStudyEnrollmentDTO>? StudyEnrollments { get; set; }
}

public class MemberResponseDTO
{
    /// <inheritdoc cref="Models.Member.Id"/>
    public Guid Id { get; set; }

    /// <inheritdoc cref="Models.Member.StudentNumber"/>
    public uint StudentNumber { get; set; }

    /// <inheritdoc cref="Models.Member.FirstName"/>
    public required string FirstName { get; set; }

    /// <inheritdoc cref="Models.Member.LastName"/>
    public required string LastName { get; set; }

    /// <inheritdoc cref="Models.Member.Email"/>
    [EmailAddress]
    public required string Email { get; set; }

    /// <inheritdoc cref="Models.Member.PhoneNumber"/>
    public required string PhoneNumber { get; set; }

    /// <inheritdoc cref="Models.Member.Street"/>
    [StringLength(40)]
    public required string Street { get; set; }

    /// <inheritdoc cref="Models.Member.HouseNumber"/>
    public required string HouseNumber { get; set; }

    /// <inheritdoc cref="Models.Member.PostalCode"/>
    [StringLength(10)]
    public required string PostalCode { get; set; }

    /// <inheritdoc cref="Models.Member.City"/>
    [StringLength(40)]
    public required string City { get; set; }

    /// <inheritdoc cref="Models.Member.DateOfBirth"/>
    public DateTimeOffset DateOfBirth { get; set; }

    /// <inheritdoc cref="Models.Member.ParentPhoneNumber"/>
    public string? ParentPhoneNumber { get; set; }

    /// <inheritdoc cref="Models.Member.MailSubscriptions"/>
    public MailSubscriptions MailSubscriptions { get; set; }

    /// <inheritdoc cref="Models.Member.Notes"/>
    public string? Notes { get; set; }

    /// <inheritdoc cref="Models.Member.RegisteredOn"/>
    public DateTimeOffset RegisteredOn { get; set; }

    /// <inheritdoc cref="Models.Member.PreferredLanguage"/>
    public Language PreferredLanguage { get; set; }

    /// <summary>
    /// Studies where the member is enrolled.
    /// </summary>
    public List<StudyEnrollmentResponseDTO> StudyEnrollments { get; set; } = new();

    /// <summary>
    /// Groups where the member is a part of.
    /// </summary>
    public List<GroupMembershipResponseDTO> GroupMemberships { get; set; } = new();
}

public class MemberUpdateDTO
{
    /// <inheritdoc cref="Models.Member.StudentNumber"/>
    public required uint StudentNumber { get; set; }

    /// <inheritdoc cref="Models.Member.FirstName"/>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string FirstName { get; set; }

    /// <inheritdoc cref="Models.Member.LastName"/>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string LastName { get; set; }

    /// <inheritdoc cref="Models.Member.Email"/>
    [StringLength(100)]
    [EmailAddress]
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }

    /// <inheritdoc cref="Models.Member.PhoneNumber"/>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public required string PhoneNumber { get; set; }

    /// <inheritdoc cref="Models.Member.Street"/>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string Street { get; set; }

    /// <inheritdoc cref="Models.Member.HouseNumber"/>
    [StringLength(10)]
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^[1-9][0-9]*\s?([a-zA-Z]|[a-zA-Z]{1,3}bis)?$", ErrorMessage = "Invalid house number format.")]
    public required string HouseNumber { get; set; }

    /// <inheritdoc cref="Models.Member.PostalCode"/>
    [StringLength(10)]
    [Required(AllowEmptyStrings = false)]
    public required string PostalCode { get; set; }

    /// <inheritdoc cref="Models.Member.City"/>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string City { get; set; }

    /// <inheritdoc cref="Models.Member.DateOfBirth"/>
    public required DateTimeOffset DateOfBirth { get; set; }

    /// <inheritdoc cref="Models.Member.ParentPhoneNumber"/>  
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public string? ParentPhoneNumber { get; set; }

    /// <inheritdoc cref="Models.Member.MailSubscriptions"/>
    public MailSubscriptions MailSubscriptions { get; set; }

    /// <inheritdoc cref="Models.Member.Notes"/>
    public string? Notes { get; set; }

    /// <inheritdoc cref="Models.Member.PreferredLanguage"/>
    public required Language PreferredLanguage { get; set; }

    /// <inheritdoc cref="Models.Member.Gratie"/>
    public bool Gratie { get; set; }

    /// <inheritdoc cref="Models.Member.LidVanVerdienste"/>
    public bool LidVanVerdienste { get; set; }

    /// <inheritdoc cref="Models.Member.EreLid"/>
    public bool EreLid { get; set; }

    /// <inheritdoc cref="Models.Member.Begunstiger"/>
    public bool Begunstiger { get; set; }

    /// <inheritdoc cref="Models.Member.Suspended"/>
    public bool Suspended { get; set; }
}

public record ForgotPasswordDTO(string Email);

public class MemberSummaryDTO
{
    public Guid? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePicturePath { get; set; }
}
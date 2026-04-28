using Backend.Models;
using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a member, containing the necessary information for creating a new member, including personal details, contact information, and other relevant properties. The PostMemberDTO is used to transfer data from the client to the server when creating a new member, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
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
    public uint MailSubscriptions { get; set; }

    /// <inheritdoc cref="Models.Member.PreferredLanguage"/>
    public required Language PreferredLanguage { get; set; }

    /// <summary>
    /// Studies where the member is enrolled.
    /// </summary>
    public List<PostStudyEnrollmentDTO>? StudyEnrollments { get; set; }
}

/// <summary>
/// Defines the DTO for retrieving member information, containing all relevant details about a member, including personal information, contact details, and any associated data such as study enrollments and group memberships. The MemberResponseDTO is used to transfer comprehensive member data from the server to the client when retrieving member information, allowing for a complete representation of the member's details in the response payload. The MemberResponseDTO can include properties such as the member's unique identifier, name, email, phone number, address details, date of birth, mail subscriptions, preferred language, and any associated study enrollments or group memberships, providing a comprehensive view of the member data for the client application.
/// </summary>
public class MemberResponseDTO
{
    /// <inheritdoc cref="Models.Member.Id"/>
    public Guid? Id { get; set; }

    /// <inheritdoc cref="Models.Member.StudentNumber"/>
    public uint? StudentNumber { get; set; }

    /// <inheritdoc cref="Models.Member.FirstName"/>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="Models.Member.LastName"/>
    public string? LastName { get; set; }

    /// <inheritdoc cref="Models.Member.Email"/>
    [EmailAddress]
    public string? Email { get; set; }

    /// <inheritdoc cref="Models.Member.PhoneNumber"/>
    public string? PhoneNumber { get; set; }

    /// <inheritdoc cref="Models.Member.Street"/>
    [StringLength(40)]
    public string? Street { get; set; }

    /// <inheritdoc cref="Models.Member.HouseNumber"/>
    public string? HouseNumber { get; set; }

    /// <inheritdoc cref="Models.Member.PostalCode"/>
    [StringLength(10)]
    public string? PostalCode { get; set; }

    /// <inheritdoc cref="Models.Member.City"/>
    [StringLength(40)]
    public string? City { get; set; }

    /// <inheritdoc cref="Models.Member.DateOfBirth"/>
    public DateTimeOffset? DateOfBirth { get; set; }

    /// <inheritdoc cref="Models.Member.ParentPhoneNumber"/>
    public string? ParentPhoneNumber { get; set; }

    /// <inheritdoc cref="Models.Member.MailSubscriptions"/>
    public uint? MailSubscriptions { get; set; }

    /// <inheritdoc cref="Models.Member.Notes"/>
    public string? Notes { get; set; }

    /// <inheritdoc cref="Models.Member.RegisteredOn"/>
    public DateTimeOffset? RegisteredOn { get; set; }

    /// <inheritdoc cref="Models.Member.PreferredLanguage"/>
    public Language? PreferredLanguage { get; set; }

    /// <summary>
    /// Studies where the member is enrolled.
    /// </summary>
    public List<StudyEnrollmentResponseDTO>? StudyEnrollments { get; set; }

    /// <summary>
    /// Groups where the member is a part of.
    /// </summary>
    public List<GroupMembershipResponseDTO>? GroupMemberships { get; set; }

    /// <inheritdoc cref="Models.Member.Gratie"/>
    public bool? Gratie { get; set; }

    /// <inheritdoc cref="Models.Member.LidVanVerdienste"/>
    public bool? LidVanVerdienste { get; set; }
    /// <inheritdoc cref="Models.Member.EreLid"/>
    public bool? EreLid { get; set; }
    /// <inheritdoc cref="Models.Member.Begunstiger"/>
    public bool? Begunstiger { get; set; }
    /// <inheritdoc cref="Models.Member.Suspended"/>
    public bool? Suspended { get; set; }

    /// <inheritdoc cref="Models.Member.ProfilePicturePath"/>
    public string? ProfilePicturePath { get; set; }
}

/// <summary>
/// Defines the DTO for updating an existing member, containing all necessary information for modifying a member's properties. The MemberUpdateDTO is used to transfer data from the client to the server when updating an existing member, allowing for changes to be made to the member's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
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
    public uint MailSubscriptions { get; set; }

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


/// <summary>
/// Defines the DTO for retrieving member information, containing all relevant details about a member, including personal information, contact details, and any associated data such as study enrollments and group memberships. The MemberResponseDTO is used to transfer comprehensive member data from the server to the client when retrieving member information, allowing for a complete representation of the member's details in the response payload. The MemberResponseDTO can include properties such as the member's unique identifier, name, email, phone number, address details, date of birth, mail subscriptions, preferred language, and any associated study enrollments or group memberships, providing a comprehensive view of the member data for the client application.
/// </summary>
public class GetMembersDto
{
    /// <summary>
    /// The search term used to filter members based on their first name, last name, or email. This field allows for searching and retrieving members that match the specified search criteria, enabling users to quickly find members based on their name or email information in the system. The Search property is essential for providing a convenient and efficient way to filter member data based on relevant keywords, enhancing the user experience when retrieving member information in the client application based on their name or email details in the system.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// The size of the page to be retrieved when paginating member data. This field is used to specify the number of member records to be included in each page of the response when retrieving member information in a paginated format. The PageSize property is essential for controlling the amount of member data returned in each page, allowing for efficient retrieval and display of member information in the client application based on pagination criteria in the system.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// The page number to be retrieved when paginating member data. This field is used to specify the page of member records to be included in the response when retrieving member information in a paginated format. The Page property is essential for controlling the specific page of member data returned in the response, allowing for efficient retrieval and display of member information in the client application based on pagination criteria in the system.
    /// </summary>
    public int Page { get; set; } = 0;

    /// <summary>
    /// The unique identifier of the study for which to filter members based on their study enrollments. This field is optional, and if provided, it allows for filtering members based on their association with a specific study, enabling the retrieval of members that are enrolled in a particular study within the system. If not provided, member data may be retrieved without any study-specific filtering, allowing for a broader retrieval of member information based on other criteria or without any specific filtering based on study enrollments in the system.
    /// </summary>
    public uint? StudyId { get; set; }

    /// <inheritdoc cref="Models.Member.Gratie"/>
    public bool? Gratie { get; set; }

    /// <inheritdoc cref="Models.Member.LidVanVerdienste"/>
    public bool? LidVanVerdienste { get; set; }

    /// <inheritdoc cref="Models.Member.EreLid"/>
    public bool? EreLid { get; set; }

    /// <inheritdoc cref="Models.Member.Begunstiger"/>
    public bool? Begunstiger { get; set; }

    /// <inheritdoc cref="Models.Member.Suspended"/>
    public bool? Suspended { get; set; }

    /// <inheritdoc cref="Models.Member.Inactive"/>
    public bool? Inactive { get; set; }

    /// <inheritdoc cref="Models.Member.PreferredLanguage"/>
    public StudyType? StudyType { get; set; }
    
}
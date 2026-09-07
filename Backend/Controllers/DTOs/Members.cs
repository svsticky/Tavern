using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for posting a member, containing the necessary information for creating a new member, including personal details, contact information, and other relevant properties. The PostMemberDTO is used to transfer data from the client to the server when creating a new member, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostMemberDTO
{
    /// <inheritdoc cref="Member.StudentNumber"/>
    public required string StudentNumber { get; set; }

    /// <inheritdoc cref="Member.FirstName"/>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string FirstName { get; set; }

    /// <inheritdoc cref="Member.LastName"/>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string LastName { get; set; }

    /// <inheritdoc cref="Member.Email"/>
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9!#$%&'*+/=?^_`{|}~.-]+@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)+$", ErrorMessage = "Invalid email format.")]
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }

    /// <inheritdoc cref="Member.PhoneNumber"/>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public required string PhoneNumber { get; set; }

    /// <inheritdoc cref="Member.Street"/>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string Street { get; set; }

    /// <inheritdoc cref="Member.HouseNumber"/>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^[1-9][0-9]*(?:[\s-]?[a-zA-Z0-9]{1,3})?$", ErrorMessage = "Invalid house number format.")]
    public required string HouseNumber { get; set; }

    /// <inheritdoc cref="Member.PostalCode"/>
    [StringLength(10)]
    [Required(AllowEmptyStrings = false)]
    public required string PostalCode { get; set; }

    /// <inheritdoc cref="Member.City"/>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string City { get; set; }

    /// <inheritdoc cref="Member.DateOfBirth"/>
    public required DateTimeOffset DateOfBirth { get; set; }

    /// <inheritdoc cref="Member.ParentPhoneNumber"/>
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public string? ParentPhoneNumber { get; set; }

    /// <summary>
    /// The IDs of the mailing lists the member should be subscribed to.
    /// </summary>
    public List<string>? SubscribedMailinglistIds { get; set; }

    /// <inheritdoc cref="Member.PreferredLanguage"/>
    public required Language PreferredLanguage { get; set; }

    /// <summary>
    /// Studies where the member is enrolled.
    /// </summary>
    public List<PostStudyEnrollmentDTO>? StudyEnrollments { get; set; }

    /// <inheritdoc cref="Member.Begunstiger"/>
    public bool? Begunstiger { get; set; }
}

/// <summary>
/// Defines the DTO for retrieving member information, containing all relevant details about a member, including personal information, contact details, and any associated data such as study enrollments and group memberships. The MemberResponseDTO is used to transfer comprehensive member data from the server to the client when retrieving member information, allowing for a complete representation of the member's details in the response payload. The MemberResponseDTO can include properties such as the member's unique identifier, name, email, phone number, address details, date of birth, mail subscriptions, preferred language, and any associated study enrollments or group memberships, providing a comprehensive view of the member data for the client application.
/// </summary>
public class MemberResponseDTO
{
    /// <inheritdoc cref="Member.Id"/>
    public Guid? Id { get; set; }

    /// <inheritdoc cref="Member.StudentNumber"/>
    public string? StudentNumber { get; set; }

    /// <inheritdoc cref="Member.FirstName"/>
    public string? FirstName { get; set; }

    /// <inheritdoc cref="Member.LastName"/>
    public string? LastName { get; set; }

    /// <inheritdoc cref="Member.Email"/>
    [EmailAddress]
    public string? Email { get; set; }

    /// <inheritdoc cref="Member.PhoneNumber"/>
    public string? PhoneNumber { get; set; }

    /// <inheritdoc cref="Member.Street"/>
    [StringLength(40)]
    public string? Street { get; set; }

    /// <inheritdoc cref="Member.HouseNumber"/>
    public string? HouseNumber { get; set; }

    /// <inheritdoc cref="Member.PostalCode"/>
    [StringLength(10)]
    public string? PostalCode { get; set; }

    /// <inheritdoc cref="Member.City"/>
    [StringLength(40)]
    public string? City { get; set; }

    /// <inheritdoc cref="Member.DateOfBirth"/>
    public DateTimeOffset? DateOfBirth { get; set; }

    /// <inheritdoc cref="Member.ParentPhoneNumber"/>
    public string? ParentPhoneNumber { get; set; }

    /// <inheritdoc cref="Member.Notes"/>
    public string? Notes { get; set; }

    /// <inheritdoc cref="Member.RegisteredOn"/>
    public DateTimeOffset? RegisteredOn { get; set; }

    /// <inheritdoc cref="Member.PreferredLanguage"/>
    public Language? PreferredLanguage { get; set; }

    /// <summary>
    /// Studies where the member is enrolled.
    /// </summary>
    public List<StudyEnrollmentResponseDTO>? StudyEnrollments { get; set; }

    /// <summary>
    /// Groups where the member is a part of.
    /// </summary>
    public List<GroupMembershipResponseDTO>? GroupMemberships { get; set; }

    /// <inheritdoc cref="Member.Gratie"/>
    public bool? Gratie { get; set; }

    /// <inheritdoc cref="Member.LidVanVerdienste"/>
    public bool? LidVanVerdienste { get; set; }
    /// <inheritdoc cref="Member.EreLid"/>
    public bool? EreLid { get; set; }
    /// <inheritdoc cref="Member.Begunstiger"/>
    public bool? Begunstiger { get; set; }
    /// <inheritdoc cref="Member.Suspended"/>
    public bool? Suspended { get; set; }

    /// <inheritdoc cref="Member.ProfilePicturePath"/>
    public string? ProfilePicturePath { get; set; }

    /// <summary>
    /// Projects a Member entity into a MemberResponseDTO, including conditional logic to determine which fields to include based on whether the requester is a board member or the member themselves. The method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information such as personal details, study enrollments, and group memberships based on the user's role. This projection is used to transform the data from the Member model into a format that is suitable for API responses, ensuring that sensitive information is only included when appropriate while still providing relevant details about the member when necessary. The inclusion of related entities like study enrollments and group memberships further enriches the response with contextual information about the member's involvement in various activities and groups within the system.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to project the member information.</param>
    /// <param name="hasViewMembers">
    /// A boolean indicating whether the requester has the ViewMembers permission (or is a (candidate) board
    /// member, who always has it).
    /// </param>
    /// <param name="isBoardOrCandidateBoard">
    /// A boolean indicating whether the requester is a (candidate) board member. Used only for the Notes
    /// field, which stays visible to the board only regardless of ViewMembers.
    /// </param>
    /// <returns>An expression that projects a Member entity into a MemberResponseDTO.</returns>
    public static Expression<Func<Member, MemberResponseDTO>> ToDto(Guid userId, bool hasViewMembers, bool isBoardOrCandidateBoard)
    {
        return m => new MemberResponseDTO
        {
            Id = m.Id,
            StudentNumber = hasViewMembers || userId == m.Id ? m.StudentNumber : null,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = hasViewMembers || userId == m.Id ? m.Email : null,
            PhoneNumber = hasViewMembers || userId == m.Id ? m.PhoneNumber : null,
            Street = hasViewMembers || userId == m.Id ? m.Street : null,
            HouseNumber = hasViewMembers || userId == m.Id ? m.HouseNumber : null,
            PostalCode = hasViewMembers || userId == m.Id ? m.PostalCode : null,
            City = hasViewMembers || userId == m.Id ? m.City : null,
            DateOfBirth = hasViewMembers || userId == m.Id ? m.DateOfBirth : null,
            ParentPhoneNumber = hasViewMembers || userId == m.Id ? m.ParentPhoneNumber : null,
            Notes = isBoardOrCandidateBoard ? m.Notes : null,
            RegisteredOn = hasViewMembers || userId == m.Id ? m.RegisteredOn : null,
            PreferredLanguage = hasViewMembers || userId == m.Id ? m.PreferredLanguage : null,
            ProfilePicturePath = m.ProfilePicturePath,
            StudyEnrollments = hasViewMembers || userId == m.Id
                ? m.StudyEnrollments
                    .AsQueryable()
                    .Select(StudyEnrollmentResponseDTO.ToDto())
                    .ToList()
                : null,
            Suspended = hasViewMembers || userId == m.Id ? m.Suspended : (bool?)null,
            Gratie = hasViewMembers || userId == m.Id ? m.Gratie : (bool?)null,
            LidVanVerdienste = hasViewMembers || userId == m.Id ? m.LidVanVerdienste : (bool?)null,
            EreLid = hasViewMembers || userId == m.Id ? m.EreLid : (bool?)null,
            Begunstiger = hasViewMembers || userId == m.Id ? m.Begunstiger : (bool?)null,
            GroupMemberships = hasViewMembers || userId == m.Id
                ? m.GroupMemberships
                    .AsQueryable()
                    .Select(GroupMembershipResponseDTO.ToDto(userId, hasViewMembers))
                    .ToList()
                : null
        };
    }
}

/// <summary>
/// Defines the DTO for updating an existing member, containing all necessary information for modifying a member's properties. The MemberUpdateDTO is used to transfer data from the client to the server when updating an existing member, allowing for changes to be made to the member's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class MemberUpdateDTO
{
    /// <inheritdoc cref="Member.StudentNumber"/>
    public required string StudentNumber { get; set; }

    /// <inheritdoc cref="Member.FirstName"/>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string FirstName { get; set; }

    /// <inheritdoc cref="Member.LastName"/>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string LastName { get; set; }

    /// <inheritdoc cref="Member.Email"/>
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9!#$%&'*+/=?^_`{|}~.-]+@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)+$", ErrorMessage = "Invalid email format.")]
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }

    /// <inheritdoc cref="Member.PhoneNumber"/>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public required string PhoneNumber { get; set; }

    /// <inheritdoc cref="Member.Street"/>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string Street { get; set; }

    /// <inheritdoc cref="Member.HouseNumber"/>
    [StringLength(10)]
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^[1-9][0-9]*(?:[\s-]?[a-zA-Z0-9]{1,3})?$", ErrorMessage = "Invalid house number format.")]
    public required string HouseNumber { get; set; }

    /// <inheritdoc cref="Member.PostalCode"/>
    [StringLength(10)]
    [Required(AllowEmptyStrings = false)]
    public required string PostalCode { get; set; }

    /// <inheritdoc cref="Member.City"/>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string City { get; set; }

    /// <inheritdoc cref="Member.DateOfBirth"/>
    public required DateTimeOffset DateOfBirth { get; set; }

    /// <inheritdoc cref="Member.ParentPhoneNumber"/>  
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public string? ParentPhoneNumber { get; set; }

    /// <inheritdoc cref="Member.Notes"/>
    public string? Notes { get; set; }

    /// <inheritdoc cref="Member.PreferredLanguage"/>
    public required Language PreferredLanguage { get; set; }

    /// <inheritdoc cref="Member.Gratie"/>
    public bool Gratie { get; set; }

    /// <inheritdoc cref="Member.LidVanVerdienste"/>
    public bool LidVanVerdienste { get; set; }

    /// <inheritdoc cref="Member.EreLid"/>
    public bool EreLid { get; set; }

    /// <inheritdoc cref="Member.Begunstiger"/>
    public bool Begunstiger { get; set; }

    /// <inheritdoc cref="Member.Suspended"/>
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

    /// <inheritdoc cref="Member.Gratie"/>
    public bool? Gratie { get; set; }

    /// <inheritdoc cref="Member.LidVanVerdienste"/>
    public bool? LidVanVerdienste { get; set; }

    /// <inheritdoc cref="Member.EreLid"/>
    public bool? EreLid { get; set; }

    /// <inheritdoc cref="Member.Begunstiger"/>
    public bool? Begunstiger { get; set; }

    /// <inheritdoc cref="Member.Suspended"/>
    public bool? Suspended { get; set; }

    /// <summary>
    /// Indicates whether to include inactive members in the retrieved member data. If set to true, both active and inactive members will be included in the response; if set to false, only active members will be included. This field allows for filtering members based on their active status, enabling the retrieval of either all members or only those that are currently active in the system based on the provided criteria in the request payload. The GetMembersDto ensures that member data is retrieved with the necessary information to effectively filter and manage member information based on their active status, providing a structured and validated approach to member retrieval in the application.
    /// </summary>
    public bool? Inactive { get; set; }

    /// <inheritdoc cref="Member.PreferredLanguage"/>
    public StudyType? StudyType { get; set; }

}

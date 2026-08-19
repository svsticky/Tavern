using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Domain;

/// <summary>
/// Defines the language preference of a member, which can be either Dutch (NL) or English (EN). This enumeration is used to indicate the preferred language for communication and content presentation for a member within the system.
/// </summary>
public enum Language
{
    /// <summary>
    /// Indicates that the member's preferred language is Dutch (NL), which may be used for communication, content presentation, and other interactions within the system that are tailored to Dutch-speaking members.
    /// </summary>
    NL,

    /// <summary>
    /// Indicates that the member's preferred language is English (EN), which may be used for communication, content presentation, and other interactions within the system that are tailored to English-speaking members.
    /// </summary>
    EN
}

/// <summary>
/// Represents a member of the organization. A member has various properties such as personal information, contact details, registration date, and relationships with other entities such as enrollments, group memberships, and announcements. This entity is used to manage and organize members within the system, allowing for better communication, access control, and personalized experiences based on member preferences and attributes.
/// </summary>
[PrimaryKey(nameof(Id))]
[Index(nameof(StudentNumber), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
public class Member
{
    /// <summary>
    /// A list of allowed field paths that can be modified by standard update operations (such as JSON Patch or partial updates via the API).
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "/phonenumber",
        "/parentphonenumber",
        "/street",
        "/housenumber",
        "/postalcode",
        "/city",
        "/preferredlanguage"
    };

    /// <summary>
    /// The unique identifier of a member, assigned incrementally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The id of the member in the authentication system, used for authentication and authorization.
    /// </summary>
    public Guid? AuthSystemUserId { get; set; }

    /// <summary>
    /// The student number of the member.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public required string StudentNumber { get; set; }

    /// <summary>
    /// The first name of the member.
    /// </summary>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string FirstName { get; set; }

    /// <summary>
    /// The last name of the member.
    /// </summary>
    [StringLength(60)]
    [Required(AllowEmptyStrings = false)]
    public required string LastName { get; set; }

    /// <summary>
    /// The email address of the member.
    /// </summary>
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9!#$%&'*+/=?^_`{|}~.-]+@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)+$", ErrorMessage = "Invalid email format.")]
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }

    /// <summary>
    /// The phone number of the member. 
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Phone number of the member's parent or guardian, if the member is a minor.
    /// </summary>
    [RegularExpression(@"^(\+?[1-9]\d{6,14}|0[1-9]\d{8})$", ErrorMessage = "Invalid phone number format.")]
    public string? ParentPhoneNumber { get; set; }

    /// <summary>
    /// The street of the member.
    /// </summary>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string Street { get; set; }

    /// <summary>
    /// The house number of the member.
    /// </summary>
    [StringLength(10)]
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^[1-9][0-9]*\s?([a-zA-Z]|[a-zA-Z]{1,3}bis)?$", ErrorMessage = "Invalid house number format.")]
    public required string HouseNumber { get; set; }

    /// <summary>
    /// The postal code of the member.
    /// </summary>
    [StringLength(10)]
    [Required(AllowEmptyStrings = false)]
    public required string PostalCode { get; set; }

    /// <summary>
    /// The city of the member.
    /// </summary>
    [StringLength(40)]
    [Required(AllowEmptyStrings = false)]
    public required string City { get; set; }

    /// <summary>
    /// The date of birth of the member.
    /// </summary>
    public DateTimeOffset DateOfBirth { get; set; }

    /// <summary>
    /// The notes about the member.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// The date and time at which the member registered.
    /// </summary>
    public DateTimeOffset RegisteredOn { get; set; }

    /// <summary>
    /// The enrollments associated with this member.
    /// </summary>
    [JsonIgnore] public virtual ICollection<StudyEnrollment> StudyEnrollments { get; set; } = new List<StudyEnrollment>();

    /// <summary>
    /// The activities this member is enrolled in.
    /// </summary>
    [JsonIgnore] public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    /// <summary>
    /// The groups this member is part of.
    /// </summary>
    [JsonIgnore] public virtual ICollection<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();

    /// <summary>
    /// The preferred language of the member.
    /// </summary>
    public Language PreferredLanguage { get; set; }

    /// <summary>
    /// Indicates whether the member is granted a fee waiver.
    /// </summary>
    public bool Gratie { get; set; } = false;

    /// <summary>
    /// Indicates whether the member is a "Lid van Verdienste".
    /// </summary>
    public bool LidVanVerdienste { get; set; } = false;

    /// <summary>
    /// Indicates whether the member is an honorary member.
    /// </summary>
    public bool EreLid { get; set; } = false;

    /// <summary>
    /// Indicates whether the member is a "Begunstiger".
    /// </summary>
    public bool Begunstiger { get; set; } = false;

    /// <summary>
    /// Indicates whether the member is suspended.
    /// </summary>
    public bool Suspended { get; set; } = false;

    /// <summary>
    /// Indicates whether the member account is soft-deleted and anonymized.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// The announcements created by this member.
    /// </summary>
    [JsonIgnore] public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    /// <summary>
    /// The path to the profile picture of the member.
    /// </summary>
    public string? ProfilePicturePath { get; set; }

    /// <summary>
    /// The file name of the profile picture of the member.
    /// </summary>
    public string? ProfilePictureFileName { get; set; }
}

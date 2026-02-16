using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public enum Language { NL, EN }

[PrimaryKey(nameof(Id))]
[Index(nameof(StudentNumber), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
public class Member
{
    /// <summary>
    /// The unique identifier of a member, assigned incrementally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The student number of the member.
    /// </summary>
    public uint StudentNumber { get; set; }

    /// <summary
    /// The first name of the member.
    /// </summary>
    [StringLength(60)]
    public required string FirstName { get; set; }

    /// <summary>
    /// The last name of the member.
    /// </summary>
    [StringLength(60)]
    public required string LastName { get; set; }

    /// <summary>
    /// The email address of the member.
    /// </summary>
    [StringLength(100)]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// The phone number of the member. 
    /// </summary>
    [StringLength(20)]
    [Phone]
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// The address of the member.
    /// </summary>
    [StringLength(200)]
    public required string Address { get; set; }

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
    /// The announcements created by this member.
    /// </summary>
    [JsonIgnore] public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
}

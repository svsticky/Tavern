using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public enum Language { NL, EN }

[PrimaryKey(nameof(Id))]
public class Member
{
    /// <summary>
    /// The unique identifier of a member, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The student number of the member.
    /// </summary>
    public uint StudentNumber { get; set; }

    /// <summary
    /// The first name of the member.
    /// </summary>
    [StringLength(60)]
    public string FirstName { get; set; }

    /// <summary>
    /// The last name of the member.
    /// </summary>
    [StringLength(60)]
    public string LastName { get; set; }

    /// <summary>
    /// The email address of the member.
    /// </summary>
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// The phone number of the member. 
    /// </summary>
    [StringLength(20)]
    [Phone]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// The address of the member.
    /// </summary>
    [StringLength(200)]
    public string Address { get; set; }

    /// <summary>
    /// The date of birth of the member.
    /// </summary>
    public DateTimeOffset DateOfBirth { get; set; }

    /// <summary>
    /// The notes about the member.
    /// </summary>
    public string Notes { get; set; }

    /// <summary>
    /// The date and time at which the member registered.
    /// </summary>
    public DateTimeOffset RegisteredOn { get; set; }

    /// <summary>
    /// The enrollments associated with this member.
    /// </summary>
    public List<StudyEnrollment> StudyEnrollments { get; set; }

    /// <summary>
    /// The activities this member is enrolled in.
    /// </summary>
    public List<Enrollment> Enrollments { get; set; }

    /// <summary>
    /// The preferred language of the member.
    /// </summary>
    public Language PreferredLanguage { get; set; }
}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[PrimaryKey(nameof(Id))]
public class Activity
{
    /// <summary>
    /// The unique identifier of an activity, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The name of the activity.
    /// </summary>
    [StringLength(120)]
    public string Name { get; set; }

    /// <summary>
    /// The price of the activity.
    /// </summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal Price { get; set; }

    public string? PosterUrl { get; set; }

    /// <summary>
    /// A dutch description or arbitrary length, explaining everything there is to know about the activity.
    /// </summary>
    [StringLength(240)]
    public string DutchDescription { get; set; }

    /// <summary>
    /// An english description or arbitrary length, explaining everything there is to know about the activity.
    /// </summary>
    [StringLength(240)]
    public string EnglishDescription { get; set; }

    /// <summary>
    /// The date and time at which the activity will start.
    /// </summary>
    public DateTimeOffset DateTimeStart { get; set; }

    /// <summary>
    /// The date and time at which the activity will end.
    /// </summary>
    public DateTimeOffset DateTimeEnd { get; set; }

    /// <summary>
    /// The deadline for unenrollment from the activity.
    /// </summary>
    public DateTimeOffset UnenrollmentDeadline { get; set; }

    /// <summary>
    /// The location where the activity will take place.
    /// </summary>
    [StringLength(200)]
    public string Location { get; set; }

    /// <summary>
    /// The maximum number of participants for the activity.
    /// </summary>
    public uint? ParticipantLimit { get; set; }

    /// <summary>
    /// The unique identifier of the organizer group.
    /// </summary>
    public uint OrganizerId { get; set; }

    /// <summary>
    /// The organizer of the activity.
    /// </summary>
    public Group Organizer { get; set; }

    /// <summary>
    /// An extra specification question for the activity.
    /// </summary>
    public virtual ICollection<SpecificationQuestion> SpecificationQuestions { get; set; } = new List<SpecificationQuestion>();

    /// <summary>
    /// Whether the activity is shown in Koala.
    /// </summary>
    public bool ShowInKoala { get; set; }

    /// <summary>
    /// Whether the activity is shown on the website.
    /// </summary>
    public bool ShowOnWebsite { get; set; }

    /// <summary>
    /// Whether the activity is open for enrollment.
    /// </summary>
    public bool IsEnrollable { get; set; }

    /// <summary>
    /// Whether the participants are visible to each other.
    /// </summary>
    public bool AreParticipantsVisible { get; set; }

    /// <summary>
    /// Whether the activity is 18+ only.
    /// </summary>
    public bool IsAdultOnly { get; set; }

    /// <summary>
    /// Whether the activity is open for first-year students.
    /// </summary>
    public bool IsOpenToFirstYears { get; set; }

    /// <summary>
    /// Whether the activity is open for second-year students.
    /// </summary>
    public bool IsOpenToSecondYears { get; set; }

    /// <summary>
    /// Whether the activity is open for third-year and higher students.
    /// </summary>
    public bool IsOpenToThirdYearsAndAbove { get; set; }

    /// <summary>
    /// Whether the activity is open for master's students.
    /// </summary>
    public bool IsOpenToMasters { get; set; }

    /// <summary>
    /// Whether the activity is open for payment.
    /// </summary>
    public bool IsOpenForPayment { get; set; }

    /// <summary>
    /// The VAT rate applicable to the activity.
    /// </summary>
    public uint VatRate { get; set; }

    /// <summary>
    /// The members enrolled in this activity.
    /// </summary>
    [JsonIgnore] public virtual ICollection<Enrollment> Enrollments { get; set; }

    /// <summary>
    /// The general ledger account associated with this activity for financial tracking.
    /// </summary>
    public string? GLAccountId { get; set; } = null;

    /// <summary>
    /// The cost center associated with this activity for financial tracking.
    /// </summary>
    public string? CostCenterId { get; set; } = null;

    /// <summary>
    /// The cost unit associated with this activity for financial tracking.
    /// </summary>
    public string? CostUnitId { get; set; } = null;
}

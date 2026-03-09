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

    /// <summary>
    /// The filename of the poster for the activity, if any.
    /// </summary>
    public string? PosterFileName { get; set; }

    /// <summary>
    /// The Path where the poster for the activity is stored, if any.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// A dutch description or arbitrary length, explaining everything there is to know about the activity.
    /// </summary>
    [StringLength(2000)]
    public string DutchDescription { get; set; }

    /// <summary>
    /// An english description or arbitrary length, explaining everything there is to know about the activity.
    /// </summary>
    [StringLength(2000)]
    public string EnglishDescription { get; set; }

    /// <summary>
    /// Gets the description of the activity in the specified locale.
    /// </summary>
    /// <param name="locale">The locale for which to get the description.</param>
    /// <returns>The description of the activity in the specified locale.</returns>
    public string GetDescription(Locale locale)
    {
        return locale switch
        {
            Locale.Dutch => DutchDescription,
            Locale.English => EnglishDescription,
            _ => EnglishDescription
        };
    }

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
    public DateTimeOffset? UnenrollmentDeadline { get; set; }

    /// <summary>
    /// The deadline for enrollment from the activity.
    /// </summary>
    public DateTimeOffset? EnrollmentDeadline { get; set; }

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
    public uint? OrganizerId { get; set; }

    /// <summary>
    /// The organizer of the activity.
    /// </summary>
    public Group? Organizer { get; set; }

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
    /// Bitflag representing which student years/levels are allowed to enroll.
    /// </summary>
    public TargetAudience AllowedAudience { get; set; }
    
    /// <summary>
    /// Whether the activity is open for payment.
    /// </summary>
    public bool IsOpenForPayment { get; set; }

    /// <summary>
    /// The VAT rate applicable to the activity.
    /// </summary>
    public uint? VatRate { get; set; }

    /// <summary>
    /// The members enrolled in this activity.
    /// </summary>
    [JsonIgnore] public virtual ICollection<Enrollment> Enrollments { get; set; }

    /// <summary>
    /// The general ledger account associated with this activity for financial tracking. Dutch: Grootboekrekening
    /// </summary>
    public string? GLAccountId { get; set; } = null;

    /// <summary>
    /// The cost center associated with this activity for financial tracking. Dutch: Kostenplaats
    /// </summary>
    public string? CostCenterId { get; set; } = null;

    /// <summary>
    /// The cost unit associated with this activity for financial tracking. Dutch: Kostendrager
    /// </summary>
    public string? CostUnitId { get; set; } = null;
}

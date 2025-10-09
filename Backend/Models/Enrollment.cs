#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

[PrimaryKey(nameof(ActivityId), nameof(UserId))]
public class Enrollment
{
    /// <summary>
    /// Reference to the unique identifier of the activity which is enrolled for.
    /// </summary>
    public uint   ActivityId { get; set; }
    /// <summary>
    /// The ID of the user, as determined by the used OAuth application, which enrolls for the activity.
    /// </summary>
    [StringLength(80)]
    public string UserId     { get; set; }
}

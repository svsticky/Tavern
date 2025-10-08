#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

[PrimaryKey(nameof(Id))]
public class Activity
{
    /// <summary>
    /// The unique identifier of an activity, assigned incrementally.
    /// </summary>
    public uint   Id          { get; set; }

    /// <summary>
    /// The name of the activity.
    /// </summary>
    [StringLength(120)]
    public string Name        { get; set; }

    /// <summary>
    /// A description or arbitrary length, explaining everything there is to know about the activity.
    /// </summary>
    [StringLength(240)]
    public string Description { get; set; } // TODO: this is of course not good for localisation

    /// <summary>
    /// The date and time at which the activity will start.
    /// </summary>
    public DateTimeOffset DateTimeStart { get; set; }
}

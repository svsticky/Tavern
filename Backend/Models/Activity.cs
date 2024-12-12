using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
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
    public string Name        { get; set; } // TODO: set max string length for performance?
    /// <summary>
    /// A description or arbitrary length, explaining everything there is to know about the activity.
    /// </summary>
    public string Description { get; set; } // TODO: this is of course not good for localisation

    /// <summary>
    /// The date and time at which the activity will start.
    /// </summary>
    public DateTimeOffset DateTimeStart { get; set; }
}

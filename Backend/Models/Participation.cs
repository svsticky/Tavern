#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Backend.Models;

public class Participation
{
    /// <summary>
    /// Reference to the unique identifier of the activity which is participated.
    /// </summary>
    public uint   ActivityId { get; set; }
    /// <summary>
    /// The ID of the user, as determined by the used OAuth application, which participated in the activity.
    /// </summary>
    public string UserId     { get; set; }
}

namespace Backend.Validators;

/// <summary>
/// Provides validation helpers for group-related fields.
/// </summary>
public static class GroupValidator
{
    /// <summary>
    /// Validates that a group name does not contain reserved delimiter characters.
    /// </summary>
    /// <param name="groupName">The group name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the group name contains invalid characters.</exception>
    public static void ValidateName(string groupName)
    {
        if (groupName.Contains(';') || groupName.Contains(':'))
            throw new ArgumentException("Group names cannot contain ';' or ':'.");
    }
}

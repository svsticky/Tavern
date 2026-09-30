namespace Backend.Utils;

/// <summary>
/// Provides utility methods for handling optional string values.
/// </summary>
public static class StringUtils
{
    /// <summary>
    /// Treats an empty or whitespace-only value as "not set", so it doesn't block a fallback.
    /// </summary>
    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

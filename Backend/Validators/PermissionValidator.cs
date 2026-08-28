using Backend.Models;

namespace Backend.Validators;

/// <summary>
/// Validates the permission keys granted to a Group or Role. The 12 known <see cref="Permission"/>
/// values are unrestricted; custom (free-form) permission strings - for other applications sharing
/// this Keycloak instance to interpret - are capped in length and count to bound JWT/cookie size.
/// </summary>
public static class PermissionValidator
{
    /// <summary>
    /// The maximum length, in characters, of a single custom permission string.
    /// </summary>
    public const int MaxCustomPermissionLength = 100;

    /// <summary>
    /// The maximum number of custom permission strings a single Group or Role may be granted.
    /// </summary>
    public const int MaxCustomPermissionCount = 20;

    /// <summary>
    /// Validates a full-replace set of permission keys, throwing when a custom entry exceeds the
    /// length cap or when too many custom entries are present. Known Permission names are exempt
    /// from both checks.
    /// </summary>
    /// <param name="permissionKeys">The distinct, non-empty permission keys to validate.</param>
    public static void ValidateCustomPermissions(IEnumerable<string> permissionKeys)
    {
        var customKeys = permissionKeys.Where(key => !Enum.TryParse<Permission>(key, out _)).ToList();

        foreach (var key in customKeys)
        {
            if (key.Length > MaxCustomPermissionLength)
                throw new ArgumentException($"Custom permission \"{key}\" exceeds the maximum length of {MaxCustomPermissionLength} characters.");
        }

        if (customKeys.Count > MaxCustomPermissionCount)
            throw new ArgumentException($"A group or role may have at most {MaxCustomPermissionCount} custom permissions.");
    }
}

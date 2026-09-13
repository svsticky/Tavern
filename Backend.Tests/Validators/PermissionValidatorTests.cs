using Backend.Validators;

namespace Backend.Tests.Validators;

public class PermissionValidatorTests
{
    [Fact]
    public void ValidateCustomPermissions_OnlyKnownPermissions_DoesNotThrow()
    {
        var keys = new[] { "ViewMembers", "ManageMembers", "EditActivityForGroup" };

        var exception = Record.Exception(() => PermissionValidator.ValidateCustomPermissions(keys));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateCustomPermissions_ShortCustomPermission_DoesNotThrow()
    {
        var keys = new[] { "ViewMembers", "CanApproveBudget" };

        var exception = Record.Exception(() => PermissionValidator.ValidateCustomPermissions(keys));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateCustomPermissions_CustomPermissionExceedsMaxLength_ThrowsArgumentException()
    {
        var tooLong = new string('a', PermissionValidator.MaxCustomPermissionLength + 1);
        var keys = new[] { tooLong };

        Assert.Throws<ArgumentException>(() => PermissionValidator.ValidateCustomPermissions(keys));
    }

    [Fact]
    public void ValidateCustomPermissions_CustomPermissionAtMaxLength_DoesNotThrow()
    {
        var atMax = new string('a', PermissionValidator.MaxCustomPermissionLength);
        var keys = new[] { atMax };

        var exception = Record.Exception(() => PermissionValidator.ValidateCustomPermissions(keys));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateCustomPermissions_TooManyCustomPermissions_ThrowsArgumentException()
    {
        var keys = Enumerable.Range(0, PermissionValidator.MaxCustomPermissionCount + 1)
            .Select(i => $"Custom{i}");

        Assert.Throws<ArgumentException>(() => PermissionValidator.ValidateCustomPermissions(keys));
    }

    [Fact]
    public void ValidateCustomPermissions_ExactlyMaxCustomPermissions_DoesNotThrow()
    {
        var keys = Enumerable.Range(0, PermissionValidator.MaxCustomPermissionCount)
            .Select(i => $"Custom{i}");

        var exception = Record.Exception(() => PermissionValidator.ValidateCustomPermissions(keys));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateCustomPermissions_ManyKnownPermissionsDoNotCountTowardCustomCap()
    {
        // All 12 known permissions, repeated - still zero custom entries, so the count cap never applies.
        var knownKeys = Enum.GetNames<Backend.Models.Permission>();
        var keys = knownKeys.Concat(knownKeys).Concat(knownKeys);

        var exception = Record.Exception(() => PermissionValidator.ValidateCustomPermissions(keys));

        Assert.Null(exception);
    }
}

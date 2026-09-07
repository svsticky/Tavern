namespace Backend.Models;

/// <summary>
/// A fine-grained permission that can be granted to a Group (applies to every member of that group) or to a
/// Role (applies to every member currently holding that role). Members of the (candidate) board always have
/// every permission, regardless of what is granted here.
/// </summary>
public enum Permission
{
    /// <summary>
    /// Create and edit activities for a group this permission applies to, as long as the activity is not
    /// online (not shown in Koala and not shown on the website). The one group-scoped permission.
    /// </summary>
    EditActivityForGroup,

    /// <summary>
    /// Create and edit any activity, for any group, without the not-online restriction.
    /// </summary>
    EditAllActivities,

    /// <summary>
    /// View all finance data, read-only.
    /// </summary>
    ViewFinances,

    /// <summary>
    /// Manage (create/edit) all finance data.
    /// </summary>
    ManageFinances,

    /// <summary>
    /// View members and member details.
    /// </summary>
    ViewMembers,

    /// <summary>
    /// Create, edit, and remove members.
    /// </summary>
    ManageMembers,

    /// <summary>
    /// Create and edit groups, including their membership composition.
    /// </summary>
    ManageGroups,

    /// <summary>
    /// Create and edit roles.
    /// </summary>
    ManageRoles,

    /// <summary>
    /// Edit which permissions a Group has.
    /// </summary>
    ManageGroupPermissions,

    /// <summary>
    /// Edit which permissions a Role has.
    /// </summary>
    ManageRolePermissions,

    /// <summary>
    /// Create and edit announcements.
    /// </summary>
    EditAnnouncements,

    /// <summary>
    /// View past activities in the admin activities list.
    /// </summary>
    ViewPastActivities
}

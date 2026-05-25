using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for reading and managing application settings.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Retrieves settings visible to the requesting user.
    /// </summary>
    /// <param name="UserId">The ID of the requesting user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The settings.</returns>
    Task<IEnumerable<Setting>> GetSettings(Guid UserId, CancellationToken ct);

    /// <summary>
    /// Retrieves a setting by name.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <param name="UserId">The ID of the requesting user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The setting when found; otherwise <c>null</c>.</returns>
    Task<Setting?> GetSetting(string name, Guid? UserId, CancellationToken ct);

    /// <summary>
    /// Creates a new setting.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="userId">The ID of the user creating the setting.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created setting entity.</returns>
    Task<Setting> CreateSetting(string name, string value, Guid userId, CancellationToken ct);

    /// <summary>
    /// Replaces a setting value by name.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <param name="value">The replacement value.</param>
    /// <param name="userId">The ID of the user updating the setting.</param>
    /// <param name="ct">The cancellation token.</param>
    Task UpdateSetting(string name, string value, Guid userId, CancellationToken ct);

    /// <summary>
    /// Deletes a setting by name.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <param name="userId">The ID of the user deleting the setting.</param>
    /// <param name="ct">The cancellation token.</param>
    Task DeleteSetting(string name, Guid userId, CancellationToken ct);

    /// <summary>
    /// Applies a JSON Patch document to a setting.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <param name="patchDoc">The patch document to apply.</param>
    /// <param name="userId">The ID of the user updating the setting.</param>
    /// <param name="ct">The cancellation token.</param>
    Task PatchSetting(string name, JsonPatchDocument<Setting> patchDoc, Guid userId, CancellationToken ct);
}

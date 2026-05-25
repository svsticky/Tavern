using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Backend.Controllers.DTOs;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing global application settings and system-wide configurations. The Settings controller provides a suite of administrative endpoints for viewing, defining, and modifying key-value pairs that govern the behavior of the application. This centralized configuration management allows authorized administrators to adjust system parameters—such as feature toggles, business rules, or integration endpoints—without requiring code changes. The controller ensures strict authorization for all operations, utilizing the ISettingsRepository to handle the underlying persistence and validation logic while maintaining a secure and audit-ready configuration layer.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class Settings : ControllerBase
{
    private readonly ISettingsRepository _settingsRepository;

    /// <summary>
    /// Initializes a new instance of the Settings controller with the required configuration management service.
    /// </summary>
    /// <param name="settingsRepository">The settings repository responsible for managing configuration data.</param>
    public Settings(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    /// <summary>
    /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>A Guid representing the authenticated user's ID.</returns>
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: settings
    /// <summary>
    /// Retrieves a comprehensive list of all system settings. The GetSettings endpoint allows authorized administrators to fetch the entire collection of configuration parameters, providing a complete overview of the current system state. This endpoint is designed to facilitate administrative dashboards and auditing processes, ensuring that those with the appropriate permissions can review all active configuration keys and their associated values.
    /// </summary>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A collection of all system setting entities.</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Setting>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<Setting>>> GetSettings(CancellationToken ct)
    {
        try
        {
            var result = await _settingsRepository.GetSettings(GetUserId(), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // GET: settings/{id}
    /// <summary>
    /// Retrieves a specific system setting by its unique identifier (name). The GetSetting endpoint provides a focused view of a single configuration parameter, allowing clients to fetch the value and metadata for a specific key. This is particularly useful for individual feature checks or targeted administrative updates, ensuring that setting data is accessible in a granular fashion while maintaining strict authorization checks.
    /// </summary>
    /// <param name="id">The unique identifier or name of the setting to retrieve.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The requested setting object if found; otherwise, a 404 Not Found status.</returns>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Setting), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [AllowAnonymous]
    public async Task<ActionResult<Setting>> GetSetting(string id, CancellationToken ct)
    {
        try
        {
            Guid? userId;
            try
            {
                userId = GetUserId();
            }
            catch
            {
                userId = null;
            }

            var result = await _settingsRepository.GetSetting(id, userId, ct);
            return result != null ? Ok(result) : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // POST: settings
    /// <summary>
    /// Creates a new system setting with a specified key and value. The PostSetting endpoint allows for the dynamic expansion of the system's configuration by defining new parameters. This endpoint validates that the provided setting does not already exist and that the requesting user has the necessary administrative rights to add new global configurations. Upon successful creation, the endpoint returns the newly established setting detail.
    /// </summary>
    /// <param name="id">The name/key for the new setting.</param>
    /// <param name="value">The initial value to assign to the setting.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The newly created setting entity with a 201 Created status.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Setting), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Setting>> PostSetting([FromQuery] string id, [FromQuery] string value, CancellationToken ct)
    {
        try
        {
            var result = await _settingsRepository.CreateSetting(id, value, GetUserId(), ct);
            return CreatedAtAction(nameof(GetSetting), new { id = result.Name }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // DELETE: settings/{id}
    /// <summary>
    /// Permanently removes a specific system setting by its identifier. The DeleteSetting endpoint is used to decommission configuration parameters that are no longer required by the application. This operation is destructive and restricted to authorized personnel, ensuring that critical system settings are not removed accidentally. Upon success, the endpoint returns a 204 No Content status to signify the resource has been removed.
    /// </summary>
    /// <param name="id">The unique identifier or name of the setting to delete.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon successful deletion.</returns>
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteSetting(string id, CancellationToken ct)
    {
        try
        {
            await _settingsRepository.DeleteSetting(id, GetUserId(), ct);
            return NoContent();
        }
        catch(KeyNotFoundException )
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // PATCH: settings/{id}
    /// <summary>
    /// Partially updates a specific system setting using a JSON Patch document. The PatchSetting endpoint enables granular modifications to a setting's properties, allowing administrators to change only specific fields without overwriting the entire resource. This is ideal for adjusting metadata or performing surgical updates to configuration values while ensuring all changes are validated and authorized.
    /// </summary>
    /// <param name="id">The identifier of the setting to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the intended changes.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status if the update was successful.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PatchSetting(string id, JsonPatchDocument<Setting> patchDoc, CancellationToken ct)
    {
        try
        {
            await _settingsRepository.PatchSetting(id, patchDoc, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // PUT: settings/{id}
    /// <summary>
    /// Updates the value of an existing system setting. The PutSetting endpoint is designed for straightforward value replacements, allowing administrators to reconfigure existing keys with new data. This operation ensures that the setting exists and that the update is performed within the bounds of the user's permissions, returning a 204 No Content status upon a successful modification of the configuration value.
    /// </summary>
    /// <param name="id">The unique identifier or name of the setting to update.</param>
    /// <param name="value">The new value to assign to the specified setting.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon a successful value update.</returns>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PutSetting(string id, string value, CancellationToken ct)
    {
        try
        {
            await _settingsRepository.UpdateSetting(id, value, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }
}
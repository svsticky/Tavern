using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing register reasons and their icons.
/// </summary>
/// <param name="registerReasonService">The register reason service used for managing register reasons and icons.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class RegisterReasonsController(IRegisterReasonService registerReasonService) : ControllerBase
{
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: registerreasons
    /// <summary>
    /// Retrieves all register reasons in display order.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<RegisterReasonResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<RegisterReasonResponseDTO>>> GetRegisterReasons(CancellationToken ct)
    {
        return Ok(await registerReasonService.GetRegisterReasons(ct));
    }

    // GET: registerreasons/{id}
    /// <summary>
    /// Retrieves a specific register reason by its ID.
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RegisterReasonResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterReasonResponseDTO>> GetRegisterReason(int id, CancellationToken ct)
    {
        var result = await registerReasonService.GetRegisterReason(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    // POST: registerreasons
    /// <summary>
    /// Creates a new register reason.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RegisterReasonResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterReasonResponseDTO>> PostRegisterReason(PostRegisterReasonDTO dto, CancellationToken ct)
    {
        var result = await registerReasonService.CreateRegisterReason(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetRegisterReason), new { id = result.Id }, result);
    }

    // PUT: registerreasons/{id}
    /// <summary>
    /// Updates an existing register reason.
    /// </summary>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutRegisterReason(int id, RegisterReasonUpdateDTO dto, CancellationToken ct)
    {
        await registerReasonService.UpdateRegisterReason(id, dto, GetUserId(), ct);
        return NoContent();
    }

    // DELETE: registerreasons/{id}
    /// <summary>
    /// Deletes a register reason.
    /// </summary>
    [HttpDelete("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteRegisterReason(int id, CancellationToken ct)
    {
        await registerReasonService.DeleteRegisterReason(id, GetUserId(), ct);
        return NoContent();
    }

    // POST: registerreasons/{id}/icon
    /// <summary>
    /// Uploads or clears the icon for a register reason.
    /// </summary>
    [HttpPost("{id}/icon")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UploadPictureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadPictureResponse>> UploadIcon(int id, IFormFile? icon)
    {
        var path = await registerReasonService.UploadRegisterReasonIcon(id, GetUserId(), icon);
        return Ok(new UploadPictureResponse { Path = path });
    }

    // GET: registerreasons/{id}/icon
    /// <summary>
    /// Retrieves the icon file for a register reason.
    /// </summary>
    [HttpGet("{id}/icon")]
    [AllowAnonymous]
    [Produces("image/webp", "image/jpeg", "image/png", "image/gif")]
    [ProducesResponseType(typeof(Stream), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Stream>> GetIcon(int id, CancellationToken ct)
    {
        var reason = await registerReasonService.GetRegisterReason(id, ct);
        if (reason == null || string.IsNullOrEmpty(reason.IconPath))
        {
            return NotFound("Register reason or icon not found.");
        }

        var file = await registerReasonService.GetRegisterReasonIconFile(reason.IconPath);
        if (file == null)
        {
            return NotFound("File is no longer present on the server.");
        }

        return File(file.Stream, file.ContentType);
    }
}

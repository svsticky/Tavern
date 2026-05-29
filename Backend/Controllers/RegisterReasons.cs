using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing register reasons and their icons.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class RegisterReasonsController(IRegisterReasonRepository registerReasonRepository) : ControllerBase
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
    public async Task<ActionResult<IEnumerable<RegisterReasonResponseDTO>>> GetRegisterReasons(CancellationToken ct)
    {
        try
        {
            return Ok(await registerReasonRepository.GetRegisterReasons(ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
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
    public async Task<ActionResult<RegisterReasonResponseDTO>> GetRegisterReason(int id, CancellationToken ct)
    {
        try
        {
            var result = await registerReasonRepository.GetRegisterReason(id, ct);
            return result != null ? Ok(result) : NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
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
    public async Task<ActionResult<RegisterReasonResponseDTO>> PostRegisterReason(PostRegisterReasonDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await registerReasonRepository.CreateRegisterReason(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetRegisterReason), new { id = result.Id }, result);
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
    public async Task<ActionResult> PutRegisterReason(int id, RegisterReasonUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            await registerReasonRepository.UpdateRegisterReason(id, dto, GetUserId(), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
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
    public async Task<ActionResult> DeleteRegisterReason(int id, CancellationToken ct)
    {
        try
        {
            await registerReasonRepository.DeleteRegisterReason(id, GetUserId(), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
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
    public async Task<ActionResult<UploadPictureResponse>> UploadIcon(int id, IFormFile? icon)
    {
        try
        {
            var path = await registerReasonRepository.UploadRegisterReasonIcon(id, GetUserId(), icon);
            return Ok(new UploadPictureResponse { Path = path });
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
    public async Task<ActionResult<Stream>> GetIcon(int id, CancellationToken ct)
    {
        try
        {
            var reason = await registerReasonRepository.GetRegisterReason(id, ct);
            if (reason == null || string.IsNullOrEmpty(reason.IconPath))
            {
                return NotFound("Register reason or icon not found.");
            }

            var file = await registerReasonRepository.GetRegisterReasonIconFile(reason.IconPath);
            if (file == null)
            {
                return NotFound("File is no longer present on the server.");
            }

            return File(file.Stream, file.ContentType);
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }
}

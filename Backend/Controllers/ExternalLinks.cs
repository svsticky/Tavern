using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing external links and their icons.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class ExternalLinksController(IExternalLinkRepository externalLinkRepository) : ControllerBase
{
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: externallinks
    /// <summary>
    /// Retrieves all external links in display order.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<ExternalLinkResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ExternalLinkResponseDTO>>> GetExternalLinks(CancellationToken ct)
    {
        try
        {
            return Ok(await externalLinkRepository.GetExternalLinks(ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // GET: externallinks/{id}
    /// <summary>
    /// Retrieves a specific external link by its ID.
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExternalLinkResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExternalLinkResponseDTO>> GetExternalLink(int id, CancellationToken ct)
    {
        try
        {
            var result = await externalLinkRepository.GetExternalLink(id, ct);
            return result != null ? Ok(result) : NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // POST: externallinks
    /// <summary>
    /// Creates a new external link.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ExternalLinkResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExternalLinkResponseDTO>> PostExternalLink(PostExternalLinkDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await externalLinkRepository.CreateExternalLink(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetExternalLink), new { id = result.Id }, result);
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

    // PUT: externallinks/{id}
    /// <summary>
    /// Updates an existing external link.
    /// </summary>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PutExternalLink(int id, ExternalLinkUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            await externalLinkRepository.UpdateExternalLink(id, dto, GetUserId(), ct);
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

    // DELETE: externallinks/{id}
    /// <summary>
    /// Deletes an external link.
    /// </summary>
    [HttpDelete("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteExternalLink(int id, CancellationToken ct)
    {
        try
        {
            await externalLinkRepository.DeleteExternalLink(id, GetUserId(), ct);
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

    // POST: externallinks/{id}/icon
    /// <summary>
    /// Uploads or clears the icon for an external link.
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
            var path = await externalLinkRepository.UploadExternalLinkIcon(id, GetUserId(), icon);
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

    // GET: externallinks/{id}/icon
    /// <summary>
    /// Retrieves the icon file for an external link.
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
            var link = await externalLinkRepository.GetExternalLink(id, ct);
            if (link == null || string.IsNullOrEmpty(link.IconPath))
            {
                return NotFound("External link or icon not found.");
            }

            var file = await externalLinkRepository.GetExternalLinkIconFile(link.IconPath);
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

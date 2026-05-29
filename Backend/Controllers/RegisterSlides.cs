using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing registration page slideshow images.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class RegisterSlidesController(IRegisterSlideRepository registerSlideRepository) : ControllerBase
{
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: registerslides
    /// <summary>
    /// Retrieves all register slides in display order.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<RegisterSlideResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<RegisterSlideResponseDTO>>> GetRegisterSlides(CancellationToken ct)
    {
        try
        {
            return Ok(await registerSlideRepository.GetRegisterSlides(ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // GET: registerslides/{id}
    /// <summary>
    /// Retrieves a specific register slide by ID.
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RegisterSlideResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterSlideResponseDTO>> GetRegisterSlide(int id, CancellationToken ct)
    {
        try
        {
            var result = await registerSlideRepository.GetRegisterSlide(id, ct);
            return result != null ? Ok(result) : NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // POST: registerslides
    /// <summary>
    /// Creates a new register slide with an uploaded image.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(RegisterSlideResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterSlideResponseDTO>> PostRegisterSlide([FromForm] PostRegisterSlideDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await registerSlideRepository.CreateRegisterSlide(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetRegisterSlide), new { id = result.Id }, result);
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

    // PUT: registerslides/{id}
    /// <summary>
    /// Updates an existing register slide.
    /// </summary>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PutRegisterSlide(int id, RegisterSlideUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            await registerSlideRepository.UpdateRegisterSlide(id, dto, GetUserId(), ct);
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

    // DELETE: registerslides/{id}
    /// <summary>
    /// Deletes a register slide.
    /// </summary>
    [HttpDelete("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteRegisterSlide(int id, CancellationToken ct)
    {
        try
        {
            await registerSlideRepository.DeleteRegisterSlide(id, GetUserId(), ct);
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

    // POST: registerslides/{id}/image
    /// <summary>
    /// Uploads or clears the image for a register slide.
    /// </summary>
    [HttpPost("{id}/image")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UploadPictureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadPictureResponse>> UploadImage(int id, IFormFile? image)
    {
        try
        {
            var path = await registerSlideRepository.UploadRegisterSlideImage(id, GetUserId(), image);
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

    // GET: registerslides/{id}/image
    /// <summary>
    /// Retrieves the image file for a register slide.
    /// </summary>
    [HttpGet("{id}/image")]
    [AllowAnonymous]
    [Produces("image/webp", "image/jpeg", "image/png", "image/gif")]
    [ProducesResponseType(typeof(Stream), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<Stream>> GetImage(int id, CancellationToken ct)
    {
        try
        {
            var slide = await registerSlideRepository.GetRegisterSlide(id, ct);
            if (slide == null || string.IsNullOrEmpty(slide.ImagePath))
            {
                return NotFound("Register slide or image not found.");
            }

            var file = await registerSlideRepository.GetRegisterSlideImageFile(slide.ImagePath);
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

using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing registration page slideshow images.
/// </summary>
/// <param name="registerSlideService">The register slide service used for managing registration slideshow images.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class RegisterSlidesController(IRegisterSlideService registerSlideService) : ControllerBase
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
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<RegisterSlideResponseDTO>>> GetRegisterSlides(CancellationToken ct)
    {
        return Ok(await registerSlideService.GetRegisterSlides(ct));
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
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterSlideResponseDTO>> GetRegisterSlide(int id, CancellationToken ct)
    {
        var result = await registerSlideService.GetRegisterSlide(id, ct);
        return result != null ? Ok(result) : NotFound();
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
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterSlideResponseDTO>> PostRegisterSlide([FromForm] PostRegisterSlideDTO dto, CancellationToken ct)
    {
        var result = await registerSlideService.CreateRegisterSlide(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetRegisterSlide), new { id = result.Id }, result);
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
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutRegisterSlide(int id, RegisterSlideUpdateDTO dto, CancellationToken ct)
    {
        await registerSlideService.UpdateRegisterSlide(id, dto, GetUserId(), ct);
        return NoContent();
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
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteRegisterSlide(int id, CancellationToken ct)
    {
        await registerSlideService.DeleteRegisterSlide(id, GetUserId(), ct);
        return NoContent();
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
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadPictureResponse>> UploadImage(int id, IFormFile? image)
    {
        var path = await registerSlideService.UploadRegisterSlideImage(id, GetUserId(), image);
        return Ok(new UploadPictureResponse { Path = path });
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
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Stream>> GetImage(int id, CancellationToken ct)
    {
        var slide = await registerSlideService.GetRegisterSlide(id, ct);
        if (slide == null || string.IsNullOrEmpty(slide.ImagePath))
        {
            return NotFound();
        }

        var file = await registerSlideService.GetRegisterSlideImageFile(slide.ImagePath);
        if (file == null)
        {
            return NotFound();
        }

        return File(file.Stream, file.ContentType);
    }
}

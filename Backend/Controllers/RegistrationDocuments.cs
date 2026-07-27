using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing registration documents (terms, rules, privacy statements) that members agree to upon registering.
/// </summary>
[ApiController]
[Route("[controller]")]
public class RegistrationDocumentsController(IRegistrationDocumentRepository repository) : ControllerBase
{
    /// <summary>
    /// Retrieves all registration documents in display order.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RegistrationDocumentResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RegistrationDocumentResponseDTO>>> GetRegistrationDocuments(CancellationToken ct)
    {
        try
        {
            return Ok(await repository.GetRegistrationDocuments(ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a specific registration document by its ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RegistrationDocumentResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationDocumentResponseDTO>> GetRegistrationDocument(int id, CancellationToken ct)
    {
        try
        {
            var doc = await repository.GetRegistrationDocument(id, ct);
            if (doc == null) return NotFound();
            return Ok(doc);
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new registration document.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RegistrationDocumentResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RegistrationDocumentResponseDTO>> PostRegistrationDocument(PostRegistrationDocumentDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await repository.CreateRegistrationDocument(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetRegistrationDocument), new { id = result.Id }, result);
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

    /// <summary>
    /// Updates an existing registration document.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> PutRegistrationDocument(int id, RegistrationDocumentUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            await repository.UpdateRegistrationDocument(id, dto, GetUserId(), ct);
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

    /// <summary>
    /// Deletes a registration document by ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteRegistrationDocument(int id, CancellationToken ct)
    {
        try
        {
            await repository.DeleteRegistrationDocument(id, GetUserId(), ct);
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

    private Guid GetUserId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (User.Identity?.IsAuthenticated != true || claim == null || !Guid.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("User context missing.");
        return userId;
    }
}

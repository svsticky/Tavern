using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing registration documents (terms, rules, privacy statements) that members agree to upon registering.
/// </summary>
[ApiController]
[Route("[controller]")]
public class RegistrationDocumentsController(IRegistrationDocumentService service) : ControllerBase
{
    /// <summary>
    /// Retrieves all registration documents in display order.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RegistrationDocumentResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<RegistrationDocumentResponseDTO>>> GetRegistrationDocuments(CancellationToken ct)
    {
        return Ok(await service.GetRegistrationDocuments(ct));
    }

    /// <summary>
    /// Retrieves a specific registration document by its ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RegistrationDocumentResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegistrationDocumentResponseDTO>> GetRegistrationDocument(int id, CancellationToken ct)
    {
        var doc = await service.GetRegistrationDocument(id, ct);
        if (doc == null) return NotFound();
        return Ok(doc);
    }

    /// <summary>
    /// Creates a new registration document.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RegistrationDocumentResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegistrationDocumentResponseDTO>> PostRegistrationDocument(PostRegistrationDocumentDTO dto, CancellationToken ct)
    {
        var result = await service.CreateRegistrationDocument(dto, GetUserId(), ct);
        return CreatedAtAction(nameof(GetRegistrationDocument), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing registration document.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutRegistrationDocument(int id, RegistrationDocumentUpdateDTO dto, CancellationToken ct)
    {
        await service.UpdateRegistrationDocument(id, dto, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes a registration document by ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteRegistrationDocument(int id, CancellationToken ct)
    {
        await service.DeleteRegistrationDocument(id, GetUserId(), ct);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (User.Identity?.IsAuthenticated != true || claim == null || !Guid.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("User context missing.");
        return userId;
    }
}

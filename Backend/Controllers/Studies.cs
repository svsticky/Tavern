using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing academic studies and curricula within the system. The StudiesController provides a centralized interface for defining, retrieving, and updating study programs, which are essential for categorizing members and academic activities. This controller supports the full range of CRUD operations, allowing authorized administrators to manage the academic catalog while providing public access for browsing available studies. By coordinating with the IStudyService, the controller ensures that study data is kept consistent and secure, enforcing authorization rules to prevent unauthorized modification of the educational framework while facilitating ease of access for the general user base.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class StudiesController : ControllerBase
{
    private readonly IStudyService _service;

    /// <summary>
    /// Initializes a new instance of the StudiesController with the required study management service.
    /// </summary>
    /// <param name="service">The study service responsible for academic data logic and persistence.</param>
    public StudiesController(IStudyService service)
    {
        _service = service;
    }

    /// <summary>
    /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>A Guid representing the authenticated user's ID.</returns>
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: studies
    /// <summary>
    /// Retrieves a list of all academic studies available in the system. The GetStudies endpoint is accessible to all users, including anonymous guests, to allow for public browsing of the study catalog. This endpoint provides a comprehensive overview of the different academic programs supported by the organization, serving as a foundational data source for registration forms and informational displays.
    /// </summary>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A collection of all study entities.</returns>
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Study>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<Study>>> GetStudies(CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetStudies(ct));
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // GET: studies/{id}
    /// <summary>
    /// Retrieves the details of a specific study by its unique identifier. The GetStudy endpoint allows clients to fetch in-depth information about a single academic program, including its properties and associated metadata. This granular access is vital for displaying detailed program descriptions or verifying specific study configurations before administrative updates. If the study is not found, the endpoint returns a 404 Not Found status.
    /// </summary>
    /// <param name="id">The unique identifier of the study to retrieve.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The requested study entity if found; otherwise, a 404 status.</returns>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Study), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Study>> GetStudy(uint id, CancellationToken ct)
    {
        try
        {
            var study = await _service.GetStudy(id, ct);
            return study != null ? Ok(study) : NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
    }

    // POST: studies
    /// <summary>
    /// Creates a new academic study within the system based on the provided data. The PostStudy endpoint processes requests from authorized administrators to add new programs to the catalog using the PostStudyDTO. This process ensures that the study data is validated against system requirements and that the creation event is correctly attributed to the authenticated user. Upon successful creation, the endpoint returns the new study details and its unique resource location.
    /// </summary>
    /// <param name="dto">The data transfer object containing the initial study configuration.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The newly created study entity with a 201 Created status.</returns>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Study), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Study>> PostStudy(PostStudyDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateStudy(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetStudy), new { id = result.Id }, result);
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

    // DELETE: studies/{id}
    /// <summary>
    /// Permanently removes a specific study from the system by its identifier. The DeleteStudy endpoint facilitates the removal of obsolete academic programs, ensuring that the operation is only executed by users with sufficient administrative permissions. The service layer handles the cleanup of associated data and ensures system integrity is maintained after the deletion. Upon success, a 204 No Content status is returned.
    /// </summary>
    /// <param name="id">The unique identifier of the study to be deleted.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon successful deletion.</returns>
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteStudy(uint id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteStudy(id, GetUserId(), ct);
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

    // PATCH: studies/{id}
    /// <summary>
    /// Partially updates the properties of an existing study using a JSON Patch document. The PatchStudy endpoint allows for precise modifications—such as renaming a program or updating a specific attribute—without requiring the submission of the full study object. This approach is efficient and minimizes data transfer, while ensuring that all changes are validated against business rules and authorized by the proper security checks.
    /// </summary>
    /// <param name="id">The unique identifier of the study to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the intended modifications.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status if the patch was applied successfully.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PatchStudy(uint id, JsonPatchDocument<Study> patchDoc, CancellationToken ct)
    {
        try
        {
            await _service.PatchStudy(id, patchDoc, GetUserId(), ct);
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

    // PUT: studies/{id}
    /// <summary>
    /// Performs a full update of an existing study's representation. The PutStudy endpoint is designed for comprehensive edits where the entire state of a study record needs to be refreshed using the data provided in the StudyUpdateDTO. This endpoint enforces strict authorization to ensure only academic administrators can modify the catalog, returning a 204 No Content status once the changes have been successfully persisted.
    /// </summary>
    /// <param name="id">The unique identifier of the study to update.</param>
    /// <param name="dto">The data transfer object containing the full updated study information.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon a successful full update.</returns>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PutStudy(uint id, StudyUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateStudy(id, dto, GetUserId(), ct);
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
using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing the association between members and their specific academic studies. The StudyEnrollmentsController provides a set of endpoints to track when and how users are enrolled in various programs, maintaining a historical and current record of their academic status. This controller is crucial for verifying eligibility for student-specific activities and benefits within the system. It enforces strict authorization to ensure that enrollment data—which often contains sensitive academic timelines—is only accessible to the account owner or authorized administrators. By utilizing the IStudyEnrollmentRepository, the controller abstracts the complex logic of managing overlapping enrollments and status transitions.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class StudyEnrollmentsController : ControllerBase
{
    private readonly IStudyEnrollmentRepository _studyEnrollmentRepository;

    /// <summary>
    /// Initializes a new instance of the StudyEnrollmentsController with the required enrollment management repository.
    /// </summary>
    /// <param name="studyEnrollmentRepository">The study enrollment repository responsible for study enrollment business logic and data persistence.</param>
    public StudyEnrollmentsController(IStudyEnrollmentRepository studyEnrollmentRepository)
    {
        _studyEnrollmentRepository = studyEnrollmentRepository;
    }

    /// <summary>
    /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>A Guid representing the authenticated user's ID.</returns>
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: studyenrollments
    /// <summary>
    /// Retrieves a list of study enrollments based on the provided query parameters. The GetStudyEnrollments endpoint allows authorized users to fetch enrollment records, which can be filtered and paginated via the GetStudyEnrollmentsDTO. This is primarily used by administrators to oversee student demographics or by individual users to view their own academic history within the system. The endpoint ensures that the returned data is scoped according to the requester's permissions, preventing unauthorized access to other members' academic records.
    /// </summary>
    /// <param name="dto">The data transfer object containing filtering and pagination criteria.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A collection of study enrollment response objects matching the criteria.</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<StudyEnrollmentResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<StudyEnrollmentResponseDTO>>> GetStudyEnrollments([FromQuery] GetStudyEnrollmentsDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await _studyEnrollmentRepository.GetStudyEnrollments(dto, GetUserId(), ct);
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

    // GET: studyenrollments/5
    /// <summary>
    /// Retrieves the details of a specific study enrollment by its unique identifier. The GetStudyEnrollment endpoint provides a comprehensive view of a single enrollment record, including the associated study details, start/end dates, and the current status of the enrollment. This granular access is necessary for verifying specific academic claims or troubleshooting individual member profiles. If the enrollment record is not found or access is denied, the endpoint returns the appropriate HTTP status code.
    /// </summary>
    /// <param name="id">The unique identifier of the study enrollment to retrieve.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The detailed study enrollment record if found; otherwise, a 404 status.</returns>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StudyEnrollmentResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudyEnrollmentResponseDTO>> GetStudyEnrollment(uint id, CancellationToken ct)
    {
        try
        {
            var result = await _studyEnrollmentRepository.GetStudyEnrollment(id, GetUserId(), ct);
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

    // POST: studyenrollments
    /// <summary>
    /// Creates a new study enrollment record for a member. The PostStudyEnrollment endpoint processes requests to link a member to a specific academic study using the PostStudyEnrollmentDTO. This operation involves validating the enrollment period and ensuring the member is not already enrolled in a conflicting program. The endpoint enforces authorization to ensure that users can only create enrollments for themselves or, in the case of staff, for other members. Upon success, it returns the newly created enrollment details.
    /// </summary>
    /// <param name="dto">The data transfer object containing the new enrollment configuration.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The newly created study enrollment response object with a 201 Created status.</returns>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(StudyEnrollmentResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudyEnrollmentResponseDTO>> PostStudyEnrollment(PostStudyEnrollmentDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await _studyEnrollmentRepository.CreateStudyEnrollment(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetStudyEnrollment), new { id = result.Id }, result);
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

    // DELETE: studyenrollments/5
    /// <summary>
    /// Permanently removes a study enrollment record from the system. The DeleteStudyEnrollment endpoint allows for the removal of incorrectly entered or obsolete enrollment data. This operation is strictly guarded to prevent accidental loss of academic history and requires the requester to have administrative rights or ownership of the record. Following successful deletion, a 204 No Content status is returned to the client.
    /// </summary>
    /// <param name="id">The unique identifier of the study enrollment to delete.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon successful deletion.</returns>
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteStudyEnrollment(uint id, CancellationToken ct)
    {
        try
        {
            await _studyEnrollmentRepository.DeleteStudyEnrollment(id, GetUserId(), ct);
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

    // PATCH: studyenrollments/5
    /// <summary>
    /// Partially updates an existing study enrollment using a JSON Patch document. The PatchStudy endpoint (targeting a specific StudyEnrollment) allows for the modification of specific enrollment attributes—such as adjusting a graduation date or changing a status—without the need to resend the entire record. This ensures that updates are targeted and efficient. The endpoint validates the proposed changes against the enrollment domain rules and verifies the user's authority to modify the record before persisting the changes.
    /// </summary>
    /// <param name="id">The unique identifier of the study enrollment to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the intended modifications.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status if the patch was successfully applied.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PatchStudy(uint id, [FromBody] JsonPatchDocument<StudyEnrollment> patchDoc, CancellationToken ct)
    {
        try
        {
            await _studyEnrollmentRepository.PatchStudy(id, patchDoc, GetUserId(), ct);
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
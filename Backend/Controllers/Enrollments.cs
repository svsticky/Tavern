using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing enrollments within the system. The EnrollmentsController provides endpoints for creating, retrieving, updating, and deleting enrollments, as well as handling related operations such as partial updates using JSON Patch. This controller is designed to ensure proper authorization for all operations, allowing only authorized users to access and modify enrollment data while providing appropriate error handling for various scenarios. The EnrollmentsController interacts with the IEnrollmentService to perform the necessary business logic and data manipulation, ensuring a clean separation of concerns and maintainable code structure for managing enrollments effectively within the application.
/// </summary>
/// <param name="enrollmentService">The enrollment service for managing enrollment operations.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class EnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    /// <summary>
    /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>A Guid representing the authenticated user's ID.</returns>
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: enrollments
    /// <summary>
    /// Retrieves a list of enrollments based on the provided filtering criteria. The GetEnrollments endpoint allows clients to fetch a collection of enrollments that match the specified criteria in the GetEnrollmentsDTO, such as filtering by member ID or activity ID. This endpoint is designed to return a list of enrollments, ensuring that proper authorization is enforced to allow only authorized users to access enrollment data, while also providing appropriate error handling for cases where enrollments may not be found or the user does not have access rights. Upon successful retrieval, the endpoint returns a list of enrollments with a 200 OK status code, allowing clients to easily access and display enrollment information as needed. This endpoint provides a convenient way for clients to stay informed about enrollment details related to specific members or activities by accessing the relevant enrollment data available within the system.
    /// </summary>
    /// <param name="dto">The data transfer object containing the filtering criteria.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of enrollments.</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<EnrollmentResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<EnrollmentResponseDTO>>> GetEnrollments([FromQuery] GetEnrollmentsDTO dto, CancellationToken cancellationToken)
    {
        var enrollments = await enrollmentService.GetEnrollments(dto, GetUserId(), cancellationToken);
        return Ok(enrollments);
    }

    // GET: enrollments/1/{memberId}
    /// <summary>
    /// Retrieves a specific enrollment based on the provided activity ID and member ID. The GetEnrollment endpoint allows clients to fetch the details of a single enrollment by providing the unique combination of activity ID and member ID in the EnrollmentKeyDTO. This endpoint is designed to return the enrollment data, ensuring that proper authorization is enforced to allow only authorized users to access the enrollment information, while also providing appropriate error handling for cases where the enrollment may not be found or the user does not have access rights. Upon successful retrieval, the endpoint returns the details of the specified enrollment with a 200 OK status code, allowing clients to easily access and display specific enrollment information as needed. This endpoint provides a convenient way for clients to stay informed about enrollment details related to specific members and activities by accessing detailed information about individual enrollments available within the system.
    /// </summary>
    /// <param name="activityId">The unique identifier of the activity for which to retrieve enrollment.</param>
    /// <param name="memberId">The unique identifier of the member for whom to retrieve enrollment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The enrollment matching the criteria.</returns>
    [HttpGet("{activityId}/{memberId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EnrollmentResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EnrollmentResponseDTO>> GetEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken)
    {
        var enrollment = await enrollmentService.GetEnrollment(activityId, memberId, GetUserId(), cancellationToken);
        if (enrollment == null)
            return NotFound();

        return Ok(enrollment);
    }

    // POST: enrollments
    /// <summary>
    /// Creates a new enrollment based on the provided data. The PostEnrollment endpoint allows clients to submit a request to create a new enrollment by providing the necessary information in the PostEnrollmentDTO. This endpoint is designed to handle the creation of enrollments, ensuring that the provided data is validated and processed correctly, while also enforcing proper authorization to ensure that only authorized users can create new enrollments within the system. Upon successful creation, the endpoint returns the details of the newly created enrollment along with a 201 Created status code, allowing clients to easily access and reference the new enrollment in subsequent operations.
    /// </summary>
    /// <param name="dto">The data transfer object containing the enrollment data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created enrollment.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EnrollmentResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EnrollmentResponseDTO>> PostEnrollment(
        PostEnrollmentDTO dto,
        CancellationToken cancellationToken)
    {
        var created = await enrollmentService.CreateEnrollment(dto, GetUserId(), cancellationToken);
        return CreatedAtAction(
            nameof(GetEnrollment),
            new { activityId = created.Activity.Id, memberId = created.Member.Id },
            created
        );
    }

    // DELETE: enrollments/1/{memberId}
    /// <summary>
    /// Deletes a specific enrollment by its unique identifier. The DeleteEnrollment endpoint allows clients to remove an existing enrollment from the system based on the provided activity ID and member ID. This endpoint is designed to handle the deletion of enrollments, ensuring that proper authorization is enforced to allow only authorized users to delete enrollments, while also providing appropriate error handling for cases where the enrollment may not be found or the user does not have access rights. Upon successful deletion, the endpoint returns a 204 No Content status code, indicating that the enrollment has been successfully removed from the system without returning any content in the response body.
    /// </summary>
    /// <param name="activityId">The unique identifier of the activity for which to delete enrollment.</param>
    /// <param name="memberId">The unique identifier of the member for whom to delete enrollment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{activityId}/{memberId}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken)
    {
        await enrollmentService.DeleteEnrollment(activityId, memberId, GetUserId(), cancellationToken);
        return NoContent();
    }

    // PUT: enrollments/1/{memberId}
    /// <summary>
    /// Updates a specific enrollment by its unique identifier with the provided data. The PutEnrollment endpoint allows clients to submit a request to update an existing enrollment by providing the necessary information in the PostEnrollmentDTO. This endpoint is designed to handle the updating of enrollments, ensuring that the provided data is validated and processed correctly, while also enforcing proper authorization to ensure that only authorized users can update existing enrollments within the system. Upon successful update, the endpoint returns a 204 No Content status code, indicating that the enrollment has been successfully updated without returning any content in the response body, allowing clients to easily manage and modify enrollment details as needed.
    /// </summary>
    /// <param name="activityId">The unique identifier of the activity for which to update enrollment.</param>
    /// <param name="memberId">The unique identifier of the member for whom to update enrollment.</param>
    /// <param name="dto">The data transfer object containing the updated enrollment data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut("{activityId}/{memberId}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutEnrollment(
        uint activityId, Guid memberId,
        [FromBody] PostEnrollmentDTO dto,
        CancellationToken cancellationToken)
    {
        if (activityId != dto.ActivityId || memberId != dto.MemberId)
            return BadRequest("ActivityId and MemberId in the URL must match those in the body.");

        await enrollmentService.UpdateEnrollment(dto, GetUserId(), cancellationToken);
        return NoContent();
    }

    // PATCH: enrollments/1/{MemberId}
    /// <summary>
    /// Partially updates a specific enrollment by its unique identifier using a JSON Patch document. The PatchEnrollment endpoint allows clients to submit a request to modify an existing enrollment by providing a JSON Patch document that specifies the changes to be made to the enrollment's properties. This endpoint is designed to handle partial updates of enrollments, ensuring that the provided patch document is validated and applied correctly, while also enforcing proper authorization to ensure that only authorized users can modify existing enrollments within the system. Upon successful application of the patch, the endpoint returns a 204 No Content status code, indicating that the enrollment has been successfully updated without returning any content in the response body. This approach allows for efficient updates to enrollment data without requiring clients to send the entire enrollment object, enabling more flexible and targeted modifications to enrollment properties as needed.
    /// </summary>
    /// <param name="activityId">The unique identifier of the activity for which to patch enrollment.</param>
    /// <param name="memberId">The unique identifier of the member for whom to patch enrollment.</param>
    /// <param name="patchDoc">The JSON Patch document specifying the changes to be made.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{activityId}/{memberId}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PatchEnrollment(
        uint activityId, Guid memberId,
        [FromBody] JsonPatchDocument<Enrollment> patchDoc,
        CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            return BadRequest();

        await enrollmentService.PatchEnrollment(activityId, memberId, patchDoc, GetUserId(), cancellationToken);
        return NoContent();
    }
}
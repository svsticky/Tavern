using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Backend.Models.Domain;
using Backend.Controllers.DTOs;
using Microsoft.AspNetCore.Authorization;
using Backend.Interfaces;
using Microsoft.AspNetCore.Cors;

namespace Backend.Controllers
{
    /// <summary>
    /// Controller for managing activities within the system. The ActivitiesController provides endpoints for creating, retrieving, updating, and deleting activities, as well as handling related operations such as uploading posters and exporting enrollments. This controller is designed to ensure proper authorization for all operations, allowing only authorized users to access and modify activity data while providing appropriate error handling for various scenarios. The ActivitiesController interacts with the IActivityRepository to perform the necessary business logic and data manipulation, ensuring a clean separation of concerns and maintainable code structure for managing activities effectively within the application.
    /// </summary>
    /// <param name="activitiesRepository">The activity repository for managing activity operations.</param>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ActivitiesController(IActivityRepository activitiesRepository) : ControllerBase
    {
        /// <summary>
        /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
        /// </summary>
        /// <returns>A Guid representing the authenticated user's ID.</returns>
        private Guid GetUserId()
        {
            return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
        }

        // GET: activities
        /// <summary>
        /// Retrieves a list of activities based on the provided query parameters. The GetActivities endpoint allows clients to fetch a collection of activities, optionally filtered and paginated according to the criteria specified in the GetActivitiesDTO. This endpoint is designed to return a comprehensive list of activities that match the given parameters, enabling clients to efficiently retrieve relevant activity data while supporting various filtering and pagination options for optimal performance and usability.
        /// </summary>
        /// <param name="dto">The data transfer object containing the query parameters.</param>
        /// <returns>A list of activities matching the criteria.</returns>
        [HttpGet]
        [AllowAnonymous]
        [EnableCors("PublicCorsPolicy")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<ActivityResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<ActivityResponseDTO>>> GetActivities([FromQuery] GetActivitiesDTO dto)
        {
            try
            {
                Guid? userId = User.Identity?.IsAuthenticated == true ? GetUserId() : null;

                var result = await activitiesRepository.GetActivities(userId, dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }

        // GET: activities/5
        /// <summary>
        /// Retrieves a specific activity by its unique identifier. The GetActivity endpoint allows clients to fetch detailed information about a single activity based on the provided ID. This endpoint is designed to return comprehensive data about the specified activity, including its properties and any associated information, enabling clients to access specific activity details efficiently while ensuring proper authorization and error handling for cases where the activity may not be found or the user does not have access rights.
        /// </summary>
        /// <param name="id">The unique identifier of the activity to retrieve.</param>
        /// <returns>The activity matching the criteria.</returns>
        [HttpGet("{id}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ActivityResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ActivityResponseDTO>> GetActivity(uint id)
        {
            try
            {
                var activity = await activitiesRepository.GetActivity(GetUserId(), id);

                if (activity == null)
                    return NotFound();

                return Ok(activity);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }

        // POST: activities
        /// <summary>
        /// Creates a new activity based on the provided data. The PostActivity endpoint allows clients to submit a request to create a new activity by providing the necessary information in the PostActivityDTO. This endpoint is designed to handle the creation of activities, ensuring that the provided data is validated and processed correctly, while also enforcing proper authorization to ensure that only authorized users can create new activities within the system. Upon successful creation, the endpoint returns the details of the newly created activity along with a 201 Created status code, allowing clients to easily access and reference the new activity in subsequent operations.
        /// </summary>
        /// <param name="dto">The data transfer object containing the activity data.</param>
        /// <returns>The newly created activity.</returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Activity), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Activity>> PostActivity([FromForm] PostActivityDTO dto)
        {
            try
            {
                var activity = await activitiesRepository.CreateActivity(GetUserId(), dto);

                return CreatedAtAction(nameof(GetActivity), new { id = activity.Id }, activity);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }

        // DELETE: activities/5
        /// <summary>
        /// Deletes a specific activity by its unique identifier. The DeleteActivity endpoint allows clients to remove an existing activity from the system based on the provided ID. This endpoint is designed to handle the deletion of activities, ensuring that proper authorization is enforced to allow only authorized users to delete activities, while also providing appropriate error handling for cases where the activity may not be found or the user does not have access rights. Upon successful deletion, the endpoint returns a 204 No Content status code, indicating that the activity has been successfully removed from the system without returning any content in the response body.
        /// </summary>
        /// <param name="id">The unique identifier of the activity to delete.</param>
        /// <returns>No content.</returns>
        [HttpDelete("{id}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> DeleteActivity(uint id)
        {
            try
            {
                await activitiesRepository.DeleteActivity(GetUserId(), id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }

        // PATCH: activities/5
        /// <summary>
        /// Partially updates a specific activity by its unique identifier using a JSON Patch document. The PatchActivity endpoint allows clients to submit a request to modify an existing activity by providing a JSON Patch document that specifies the changes to be made to the activity's properties. This endpoint is designed to handle partial updates of activities, ensuring that the provided patch document is validated and applied correctly, while also enforcing proper authorization to ensure that only authorized users can modify existing activities within the system. Upon successful application of the patch, the endpoint returns a 204 No Content status code, indicating that the activity has been successfully updated without returning any content in the response body. This approach allows for efficient updates to activity data without requiring clients to send the entire activity object, enabling more flexible and targeted modifications to activity properties as needed.
        /// </summary>
        /// <param name="id">The unique identifier of the activity to update.</param>
        /// <param name="patchDoc">The JSON Patch document containing the changes to apply.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> PatchActivity(uint id, [FromBody] JsonPatchDocument<Activity> patchDoc, CancellationToken ct)
        {
            try
            {
                await activitiesRepository.PatchActivity(GetUserId(), id, patchDoc, ct);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }

        // POST: activities/5/poster
        /// <summary>
        /// Uploads a poster image for a specific activity by its unique identifier. The UploadPoster endpoint allows clients to submit a request to upload a poster image file for an existing activity, associating the uploaded image with the specified activity ID. This endpoint is designed to handle file uploads, ensuring that the provided file is validated and processed correctly, while also enforcing proper authorization to ensure that only authorized users can upload posters for activities within the system. Upon successful upload, the endpoint returns a 200 OK status code, indicating that the poster has been successfully uploaded and associated with the activity, allowing clients to easily manage and update activity posters as needed.
        /// </summary>
        /// <param name="id">The unique identifier of the activity for which to upload a poster.</param>
        /// <param name="poster">The poster image file to upload.</param>
        /// <returns>OK status code.</returns>
        [HttpPost("{id}/poster")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> UploadPoster(uint id, IFormFile? poster)
        {
            try
            {
                await activitiesRepository.UploadPoster(GetUserId(), id, poster);
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }

        // PUT: activities/5
        /// <summary>
        /// Updates a specific activity by its unique identifier with the provided data. The PutActivity endpoint allows clients to submit a request to update an existing activity by providing the necessary information in the PutActivityDTO. This endpoint is designed to handle the updating of activities, ensuring that the provided data is validated and processed correctly, while also enforcing proper authorization to ensure that only authorized users can update existing activities within the system. Upon successful update, the endpoint returns a 204 No Content status code, indicating that the activity has been successfully updated without returning any content in the response body, allowing clients to easily manage and modify activity details as needed.
        /// </summary>
        /// <param name="id">The unique identifier of the activity to update.</param>
        /// <param name="dto">The data transfer object containing the updated activity information.</param>
        /// <returns>No content.</returns>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> PutActivity(uint id, [FromForm] PutActivityDTO dto)
        {
            try
            {
                await activitiesRepository.UpdateActivity(GetUserId(), id, dto);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }

        // GET: activities/5/poster
        /// <summary>
        /// Retrieves the poster image for a specific activity by its unique identifier. The GetPoster endpoint allows clients to fetch the poster image associated with an existing activity based on the provided ID. This endpoint is designed to return the poster image file, ensuring that proper authorization is enforced to allow only authorized users to access activity posters, while also providing appropriate error handling for cases where the activity or poster may not be found. Upon successful retrieval, the endpoint returns the poster image file with the correct content type, allowing clients to easily display or manage activity posters as needed. Additionally, this endpoint supports an optional download parameter that allows clients to specify whether they want to download the poster file directly or display it in the browser, providing flexibility in how clients can access and utilize activity posters within their applications.
        /// </summary>
        /// <param name="id">The unique identifier of the activity for which to retrieve the poster.</param>
        /// <returns>The poster image file.</returns>
        [HttpGet("{id}/poster")]
        [AllowAnonymous]
        [EnableCors("PublicCorsPolicy")]
        [Produces("image/webp", "image/jpeg", "image/png", "image/gif")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Stream>> GetPoster(uint id)
        {
            try
            {
                Guid? guid = User.Identity?.IsAuthenticated == true ? GetUserId() : null;

                var result = await activitiesRepository.GetPoster(guid, id, download: false);

                if (result == null)
                    return NotFound();

                return File(result.Value.Stream, result.Value.ContentType);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }

        // GET: activities/5/poster/download
        /// <summary>
        /// Downloads the poster image for a specific activity by its unique identifier. The DownloadPoster endpoint allows clients to fetch the poster image associated with an existing activity based on the provided ID, specifically for the purpose of downloading the file directly. This endpoint is designed to return the poster image file with the appropriate content type and a content disposition that prompts the client to download the file, ensuring that proper authorization is enforced to allow only authorized users to access activity posters, while also providing appropriate error handling for cases where the activity or poster may not be found. Upon successful retrieval, the endpoint returns the poster image file with a filename, allowing clients to easily download and manage activity posters as needed. This endpoint provides a convenient way for clients to access and utilize activity posters within their applications by enabling direct downloads of poster files.
        /// </summary>
        /// <param name="id">The unique identifier of the activity for which to download the poster.</param>
        /// <returns>The poster image file.</returns>
        [HttpGet("{id}/poster/download")]
        [AllowAnonymous]
        [EnableCors("PublicCorsPolicy")]
        [Produces("image/webp", "image/jpeg", "image/png", "image/gif")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Stream>> DownloadPoster(uint id)
        {
            try
            {
                Guid? userId = User.Identity?.IsAuthenticated == true ? GetUserId() : null;

                var result = await activitiesRepository.GetPoster(userId, id, download: true);

                if (result == null)
                    return NotFound();

                return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }

        /// <summary>
        /// Exports the enrollments for a specific activity by its unique identifier in CSV format. The ExportEnrollments endpoint allows clients to fetch the enrollment data associated with an existing activity based on the provided ID, specifically formatted as a CSV file for easy analysis and reporting. This endpoint is designed to return the enrollment data in a structured CSV format, ensuring that proper authorization is enforced to allow only authorized users to access enrollment information, while also providing appropriate error handling for cases where the activity or enrollments may not be found. Upon successful retrieval, the endpoint returns the enrollment data as a downloadable CSV file with a filename, allowing clients to easily export and manage enrollment information for activities as needed. This endpoint provides a convenient way for clients to access and utilize enrollment data within their applications by enabling direct downloads of enrollment information in a widely used format.
        /// </summary>
        /// <param name="id">The unique identifier of the activity for which to export enrollments.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The enrollment data in CSV format.</returns>
        [HttpGet("{id}/enrollments/export")]
        [Produces("text/csv")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Stream>> ExportEnrollments(uint id, CancellationToken ct)
        {
            try
            {
                var result = await activitiesRepository.GetEnrollmentsCsv(GetUserId(), id, ct);

                return File(result.Content, "text/csv", result.FileName);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest( new ErrorResponseDto { Message = e.Message });;
            }
        }
    }
}
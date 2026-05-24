using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing announcements within the system. The AnnouncementsController provides endpoints for creating, retrieving, updating, and deleting announcements, as well as handling related operations such as partial updates using JSON Patch. This controller is designed to ensure proper authorization for all operations, allowing only authorized users to access and modify announcement data while providing appropriate error handling for various scenarios. The AnnouncementsController interacts with the IAnnouncementRepository to perform the necessary business logic and data manipulation, ensuring a clean separation of concerns and maintainable code structure for managing announcements effectively within the application.
/// </summary>
[Route("[controller]")]
[ApiController]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementRepository _announcementRepository;

    /// <summary>
    /// Initializes a new instance of the AnnouncementsController class with the specified announcement repository. The constructor takes an IAnnouncementRepository as a parameter, which is used to perform various operations related to announcements, such as creating, retrieving, updating, and deleting announcements. This dependency injection allows for better separation of concerns and promotes a more modular and testable code structure, enabling the controller to focus on handling HTTP requests and responses while delegating the business logic to the repository layer.
    /// </summary>
    /// <param name="announcementRepository">The announcement repository for managing announcement operations.</param>
    public AnnouncementsController(IAnnouncementRepository announcementRepository)
    {
        _announcementRepository = announcementRepository;
    }

    /// <summary>
    /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>A Guid representing the authenticated user's ID.</returns>
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: announcements
    /// <summary>
    /// Retrieves a list of all announcements. The GetAnnouncements endpoint allows clients to fetch a collection of announcements that have been created within the system. This endpoint is designed to return a list of announcements, ensuring that proper authorization is enforced to allow only authorized users to access the announcement data, while also providing appropriate error handling for cases where announcements may not be found or the user does not have access rights. Upon successful retrieval, the endpoint returns a list of announcements with a 200 OK status code, allowing clients to easily access and display announcement information as needed. This endpoint provides a convenient way for clients to stay informed about important updates, events, or news related to the organization or community by accessing the latest announcements available within the system.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of announcements.</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<GetAnnouncementResponseDTO>>> GetAnnouncements(CancellationToken cancellationToken)
    {
        try
        {
            var announcements = await _announcementRepository.GetAnnouncements(GetUserId(), cancellationToken);
            return Ok(announcements);
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

    // GET: announcements/5
    /// <summary>
    /// Retrieves a specific announcement by its unique identifier. The GetAnnouncement endpoint allows clients to fetch the details of a single announcement based on the provided ID. This endpoint is designed to return the announcement data, ensuring that proper authorization is enforced to allow only authorized users to access the announcement information, while also providing appropriate error handling for cases where the announcement may not be found or the user does not have access rights. Upon successful retrieval, the endpoint returns the details of the specified announcement with a 200 OK status code, allowing clients to easily access and display specific announcement information as needed. This endpoint provides a convenient way for clients to stay informed about important updates, events, or news related to the organization or community by accessing detailed information about individual announcements available within the system.
    /// </summary>
    /// <param name="id">The unique identifier of the announcement to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The announcement matching the criteria.</returns>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GetAnnouncementResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAnnouncementResponseDTO>> GetAnnouncement(uint id, CancellationToken cancellationToken)
    {
        try
        {
            var announcement = await _announcementRepository.GetAnnouncement(id, GetUserId(), cancellationToken);
            
            if (announcement == null)
                return NotFound();
        
            return Ok(announcement);
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

    // POST: announcements
    /// <summary>
    /// Creates a new announcement based on the provided data. The PostAnnouncement endpoint allows clients to submit a request to create a new announcement by providing the necessary information in the PostAnnouncementDTO. This endpoint is designed to handle the creation of announcements, ensuring that the provided data is validated and processed correctly, while also enforcing proper authorization to ensure that only authorized users can create new announcements within the system. Upon successful creation, the endpoint returns the details of the newly created announcement along with a 201 Created status code, allowing clients to easily access and reference the new announcement in subsequent operations.
    /// </summary>
    /// <param name="dto">The data transfer object containing the announcement data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created announcement.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Announcement), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Announcement>> PostAnnouncement(PostAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _announcementRepository.CreateAnnouncement(GetUserId(), dto, cancellationToken);
            return CreatedAtAction(nameof(GetAnnouncement), new { id = created.Id }, created);
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

    // DELETE: announcements/5
    /// <summary>
    /// Deletes a specific announcement by its unique identifier. The DeleteAnnouncement endpoint allows clients to remove an existing announcement from the system based on the provided ID. This endpoint is designed to handle the deletion of announcements, ensuring that proper authorization is enforced to allow only authorized users to delete announcements, while also providing appropriate error handling for cases where the announcement may not be found or the user does not have access rights. Upon successful deletion, the endpoint returns a 204 No Content status code, indicating that the announcement has been successfully removed from the system without returning any content in the response body.
    /// </summary>
    /// <param name="id">The unique identifier of the announcement to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteAnnouncement(uint id, CancellationToken cancellationToken)
    {
        try
        {
            await _announcementRepository.DeleteAnnouncement(id, GetUserId(), cancellationToken);
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
        catch (Exception e)
        {
            return BadRequest( new ErrorResponseDto { Message = e.Message });;
        }
    }

    // PATCH: announcements/5
    /// <summary>
    /// Partially updates a specific announcement by its unique identifier using a JSON Patch document. The PatchAnnouncement endpoint allows clients to submit a request to modify an existing announcement by providing a JSON Patch document that specifies the changes to be made to the announcement's properties. This endpoint is designed to handle partial updates of announcements, ensuring that the provided patch document is validated and applied correctly, while also enforcing proper authorization to ensure that only authorized users can modify existing announcements within the system. Upon successful application of the patch, the endpoint returns a 204 No Content status code, indicating that the announcement has been successfully updated without returning any content in the response body. This approach allows for efficient updates to announcement data without requiring clients to send the entire announcement object, enabling more flexible and targeted modifications to announcement properties as needed.
    /// </summary>
    /// <param name="id">The unique identifier of the announcement to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the changes to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PatchAnnouncement(uint id, [FromBody] JsonPatchDocument<Announcement> patchDoc, CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            return BadRequest();

        try
        {
            await _announcementRepository.PatchAnnouncement(id, patchDoc, GetUserId(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
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

    // PUT: announcements/5
    /// <summary>
    /// Updates a specific announcement by its unique identifier with the provided data. The PutAnnouncement endpoint allows clients to submit a request to update an existing announcement by providing the necessary information in the UpdateAnnouncementDTO. This endpoint is designed to handle the updating of announcements, ensuring that the provided data is validated and processed correctly, while also enforcing proper authorization to ensure that only authorized users can update existing announcements within the system. Upon successful update, the endpoint returns a 204 No Content status code, indicating that the announcement has been successfully updated without returning any content in the response body, allowing clients to easily manage and modify announcement details as needed.
    /// </summary>
    /// <param name="id">The unique identifier of the announcement to update.</param>
    /// <param name="dto">The data transfer object containing the updated announcement data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PutAnnouncement(uint id, UpdateAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            await _announcementRepository.UpdateAnnouncement(id, dto, GetUserId(), cancellationToken);
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
        catch (Exception e)
        {
            return BadRequest( new ErrorResponseDto { Message = e.Message });;
        }
    }
}
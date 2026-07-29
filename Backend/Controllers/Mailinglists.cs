using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing mailing lists. The Mailinglists controller provides a set of endpoints for authorized users to perform CRUD operations on mailing list entities. This includes retrieving all mailing lists, fetching specific mailing list details, creating new mailing lists, updating existing ones, and deleting mailing lists as needed. The controller ensures that only users with appropriate permissions can access these operations, leveraging the IMailinglistService to handle the underlying business logic and data persistence while maintaining a secure and efficient interface for managing communication channels within the application.
/// </summary>
/// <param name="mailinglistService">The mailing list service to use.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class Mailinglists(IMailinglistService mailinglistService) : ControllerBase
{
    /// <summary>
    /// Retrieves the user ID from the claims of the authenticated user. This method is used to identify the user making the request and is typically used for authorization checks and associating actions with specific users. The user ID is expected to be stored in a claim with the type "UserId" and is parsed as a GUID. This allows the controller to perform operations that require knowledge of the user's identity, such as ensuring that only authorized users can create, update, or delete mailing lists, and to log user actions for auditing purposes.
    /// </summary>
    /// <returns>The user ID.</returns>
    private Guid GetUserId() => Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

    /// <summary>
    /// Retrieves a list of all mailing lists. This endpoint allows authorized users to fetch the complete collection of mailing lists available in the system. The method calls the GetMailinglists function of the IMailinglistService, which interacts with the data layer to retrieve the mailing list entities. The result is returned as an HTTP 200 OK response containing the list of mailing lists. If any exceptions occur during the process, appropriate error responses are returned, such as 403 Forbidden for unauthorized access or 400 Bad Request for other errors. This endpoint is essential for displaying available mailing lists to users and enabling them to manage their email communication preferences effectively.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The list of mailing lists.</returns>
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Mailinglist>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Mailinglist>>> GetMailinglists(CancellationToken ct)
    {
        var result = await mailinglistService.GetMailinglists(ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a specific mailing list by its ID. This endpoint allows authorized users to fetch the details of a single mailing list identified by its unique ID. The method calls the GetMailinglist function of the IMailinglistService, passing the ID and cancellation token to retrieve the corresponding mailing list entity from the data layer. If the mailing list is found, it is returned as an HTTP 200 OK response; if not found, a 404 Not Found response is returned. The endpoint also handles exceptions, returning a 403 Forbidden response for unauthorized access and a 400 Bad Request response for other errors. This functionality is crucial for users to view and manage specific mailing lists within the application.
    /// </summary>
    /// <param name="id">The ID of the mailing list to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The mailing list if found, otherwise a 404 Not Found response.</returns>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Mailinglist), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Mailinglist>> GetMailinglist(int id, CancellationToken ct)
    {
        var result = await mailinglistService.GetMailinglist(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Creates a new mailing list. This endpoint allows authorized users to create a new mailing list by providing the necessary details in the request body, encapsulated in the PostMailinglistDTO. The method calls the CreateMailinglist function of the IMailinglistService, passing the DTO, user ID, and cancellation token to handle the creation logic. If the mailing list is successfully created, it returns an HTTP 201 Created response with the details of the newly created mailing list. The endpoint also handles exceptions, returning a 403 Forbidden response for unauthorized access and a 400 Bad Request response for other errors. This functionality is essential for users to expand their communication channels by adding new mailing lists as needed.
    /// </summary>
    /// <param name="mailinglist">The DTO containing the details for the new mailing list.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created mailing list.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Mailinglist), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Mailinglist>> PostMailinglist([FromBody] PostMailinglistDTO mailinglist, CancellationToken ct)
    {
        var result = await mailinglistService.CreateMailinglist(mailinglist, GetUserId(), ct);
        return CreatedAtAction(nameof(GetMailinglist), new { id = result.Id, bitValue = result.BitValue }, result);
    }

    /// <summary>
    /// Updates an existing mailing list identified by its ID. This endpoint allows authorized users to modify the details of an existing mailing list by providing the updated information in the request body, encapsulated in the PostMailinglistDTO. The method calls the UpdateMailinglist function of the IMailinglistService, passing the ID, DTO, user ID, and cancellation token to handle the update logic. If the update is successful, it returns an HTTP 204 No Content response; if the mailing list is not found, a 404 Not Found response is returned. The endpoint also handles exceptions, returning a 403 Forbidden response for unauthorized access and a 400 Bad Request response for other errors. This functionality is crucial for users to maintain accurate and up-to-date mailing list information within the application.
    /// </summary>
    /// <param name="id">The ID of the mailing list to update.</param>
    /// <param name="mailinglist">The DTO containing the updated details for the mailing list.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutMailinglist(int id, [FromBody] PostMailinglistDTO mailinglist, CancellationToken ct)
    {
        await mailinglistService.UpdateMailinglist(id, mailinglist, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>
    /// Partially updates an existing mailing list identified by its ID using a JSON Patch document. This endpoint allows authorized users to modify specific fields of an existing mailing list without needing to provide the entire entity. The method accepts a <see cref="JsonPatchDocument{MMailinglistr}"/> in the request body, which specifies the operations to be performed on the mailing list. The UpdateMailinglist function of the IMailinglistService is called with the ID, patch document, user ID, and cancellation token to handle the patching logic. If the patch is successful, it returns an HTTP 204 No Content response; if the mailing list is not found, a 404 Not Found response is returned. The endpoint also handles exceptions, returning a 403 Forbidden response for unauthorized access and a 400 Bad Request response for other errors. This functionality provides flexibility for users to make partial updates to mailing lists efficiently.
    /// </summary>
    /// <param name="id">The ID of the mailing list to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the update operations.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PatchMailinglist(int id, [FromBody] JsonPatchDocument<Mailinglist> patchDoc, CancellationToken ct)
    {
        await mailinglistService.PatchMailinglist(id, patchDoc, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes an existing mailing list identified by its ID. This endpoint allows authorized users to remove a mailing list from the system. The method calls the DeleteMailinglist function of the IMailinglistService, passing the ID, user ID, and cancellation token to handle the deletion logic. If the deletion is successful, it returns an HTTP 204 No Content response; if the mailing list is not found, a 404 Not Found response is returned. The endpoint also handles exceptions, returning a 403 Forbidden response for unauthorized access and a 400 Bad Request response for other errors. This functionality is essential for users to manage their mailing lists effectively by removing those that are no longer needed.
    /// </summary>
    /// <param name="id">The ID of the mailing list to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteMailinglist(int id, CancellationToken ct)
    {
        await mailinglistService.DeleteMailinglist(id, GetUserId(), ct);
        return NoContent();
    }
}
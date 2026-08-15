using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing group memberships within the system. The GroupMembershipsController provides endpoints for creating, retrieving, updating, and deleting group memberships, as well as handling related operations such as partial updates using JSON Patch. This controller is designed to ensure proper authorization for all operations, allowing only authorized users to access and modify group membership data while providing appropriate error handling for various scenarios. The GroupMembershipsController interacts with the IGroupMembershipService to perform the necessary business logic and data manipulation, ensuring a clean separation of concerns and maintainable code structure for managing group memberships effectively within the application.
/// </summary>
/// <param name="groupMembershipService">The group membership service for managing group membership operations.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class GroupMembershipsController(IGroupMembershipService groupMembershipService) : ControllerBase
{
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: groupMemberships
    /// <summary>
    /// Retrieves a list of group memberships based on the provided query parameters. The GetGroupMemberships endpoint allows clients to fetch a collection of group memberships that match the specified criteria, such as filtering by group ID, member ID, or other relevant parameters defined in the GetGroupMembershipsDTO. This endpoint ensures that the requesting user is authorized to access the group membership information and returns a list of GroupMembershipResponseDTO objects that represent the matching group memberships. If no group memberships are found that match the criteria, it returns an empty list with a 200 OK status code. Additionally, if the user is not authorized to access the resource, it returns a 403 Forbidden status code, and any other exceptions encountered during the process will result in a 400 Bad Request response with an appropriate error message.
    /// </summary>
    /// <param name="dto">The data transfer object containing the filtering criteria.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of group memberships.</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<GroupMembershipResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<GroupMembershipResponseDTO>>> GetGroupMemberships(
        [FromQuery] GetGroupMembershipsDTO dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var result = await groupMembershipService.GetGroupMemberships(dto, userId, cancellationToken);
        return Ok(result);
    }

    // GET: groupMemberships/5
    /// <summary>
    /// Retrieves a specific group membership by its unique identifier. The GetGroupMembership endpoint allows clients to fetch the details of a single group membership based on the provided ID. This endpoint ensures that the requesting user is authorized to access the group membership information and returns the corresponding GroupMembershipResponseDTO if found. If the specified group membership does not exist, it returns a 404 Not Found status code, and if the user is not authorized to access the resource, it returns a 403 Forbidden status code. Additionally, any other exceptions encountered during the process will result in a 400 Bad Request response with an appropriate error message.
    /// </summary>
    /// <param name="id">The unique identifier of the group membership to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The group membership matching the criteria.</returns>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GroupMembershipResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GroupMembershipResponseDTO>> GetGroupMembership(uint id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await groupMembershipService.GetGroupMembership(id, userId, cancellationToken);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // POST: groupMemberships
    /// <summary>
    /// Creates a new group membership based on the provided data. The PostGroupMembership endpoint allows clients to submit a request to create a new group membership by providing the necessary information in the form of a PostGroupMembershipDTO. This endpoint ensures that the requesting user is authorized to create a group membership and processes the creation logic through the IGroupMembershipService. Upon successful creation, it returns a 201 Created status code along with the details of the newly created group membership in the response body. If the user is not authorized to perform this action, it returns a 403 Forbidden status code, and any other exceptions encountered during the process will result in a 400 Bad Request response with an appropriate error message.
    /// </summary>
    /// <param name="membershipDto">The data transfer object containing the group membership information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created group membership.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(GroupMembership), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GroupMembership>> PostGroupMembership(
        PostGroupMembershipDTO membershipDto,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var created = await groupMembershipService.CreateGroupMembership(membershipDto, userId, cancellationToken);

        return CreatedAtAction(
            nameof(GetGroupMembership),
            new { id = created.Id },
            created
        );
    }

    // DELETE: groupMemberships/5
    /// <summary>
    /// Deletes a specific group membership by its unique identifier. The DeleteGroupMembership endpoint allows clients to submit a request to remove an existing group membership based on the provided ID. This endpoint ensures that the requesting user is authorized to delete the group membership and processes the deletion logic through the IGroupMembershipService. Upon successful deletion, it returns a 204 No Content status code, indicating that the group membership has been successfully removed. If the specified group membership does not exist, it returns a 404 Not Found status code, and if the user is not authorized to perform this action, it returns a 403 Forbidden status code. Additionally, any other exceptions encountered during the process will result in a 400 Bad Request response with an appropriate error message.
    /// </summary>
    /// <param name="id">The unique identifier of the group membership to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteGroupMembership(uint id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await groupMembershipService.DeleteGroupMembership(id, userId, cancellationToken);
        return NoContent();
    }

    // PATCH: groupMemberships/5
    /// <summary>
    /// Partially updates a specific group membership by its unique identifier using a JSON Patch document. The PatchGroupMembership endpoint allows clients to submit a request to modify an existing group membership by providing a JSON Patch document that specifies the changes to be made to the group membership's properties. This endpoint is designed to handle partial updates of group memberships, ensuring that the provided patch document is validated and applied correctly, while also enforcing proper authorization to ensure that only authorized users can modify existing group memberships within the system. Upon successful application of the patch, the endpoint returns a 204 No Content status code, indicating that the group membership has been successfully updated without returning any content in the response body. This approach allows for efficient updates to group membership data without requiring clients to send the entire group membership object, enabling more flexible and targeted modifications to group membership properties as needed.
    /// </summary>
    /// <param name="id">The unique identifier of the group membership to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the changes to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PatchGroupMembership(
        uint id,
        [FromBody] JsonPatchDocument<GroupMembership> patchDoc,
        CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            return BadRequest();

        var userId = GetUserId();
        await groupMembershipService.PatchGroupMembership(id, userId, patchDoc, cancellationToken);
        return NoContent();
    }

    // PUT: groupMemberships/5
    /// <summary>
    /// Fully updates a specific group membership by its unique identifier. The PutGroupMembership endpoint allows clients to submit a request to replace an existing group membership with new data based on the provided ID. This endpoint ensures that the requesting user is authorized to update the group membership and processes the update logic through the IGroupMembershipService. Upon successful update, it returns a 204 No Content status code, indicating that the group membership has been successfully updated without returning any content in the response body. If the specified group membership does not exist, it returns a 404 Not Found status code, and if the user is not authorized to perform this action, it returns a 403 Forbidden status code. Additionally, any other exceptions encountered during the process will result in a 400 Bad Request response with an appropriate error message.
    /// </summary>
    /// <param name="id">The unique identifier of the group membership to update.</param>
    /// <param name="membershipDto">The data transfer object containing the updated group membership information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutGroupMembership(
        uint id,
        GroupMembershipUpdateDTO membershipDto,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await groupMembershipService.UpdateGroupMembership(id, userId, membershipDto, cancellationToken);
        return NoContent();
    }
}

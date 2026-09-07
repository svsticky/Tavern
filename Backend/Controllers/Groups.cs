using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing organizational groups within the system. The GroupsController provides a comprehensive set of endpoints for handling the lifecycle of groups, including creation, retrieval, full and partial updates, and deletion. It also manages group-specific assets such as group profile pictures. This controller is designed to enforce strict ownership and authorization rules, ensuring that only authorized users can modify group data. By interacting with the IGroupService, the controller maintains a clean separation between the API layer and the business logic required to manage group structures and their associated metadata effectively.
/// </summary>
/// <param name="groupService">The group service used for managing group-related data operations.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class GroupsController(IGroupService groupService) : ControllerBase
{
    /// <summary>
    /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>A Guid representing the authenticated user's ID.</returns>
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: groups
    /// <summary>
    /// Retrieves a collection of groups based on the provided query filters and pagination parameters. The GetGroups endpoint allows clients to fetch a list of groups that match specific criteria defined in the GetGroupDTO. This endpoint is designed to support efficient data retrieval by allowing users to filter through available groups while ensuring that the returned results are scoped according to the user's authorization level. It provides a robust way for clients to browse and search for groups within the application's ecosystem.
    /// </summary>
    /// <param name="dto">The data transfer object containing filtering and pagination parameters.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A list of group response objects matching the specified criteria.</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<GroupResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<GroupResponseDTO>>> GetGroups(
        [FromQuery] GetGroupDTO dto,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var result = await groupService.GetGroups(userId, dto, cancellationToken);
        return Ok(result);
    }

    // GET: groups/5
    /// <summary>
    /// Retrieves the detailed information of a specific group by its unique identifier. The GetGroup endpoint is designed to return a single, comprehensive group record based on the provided ID. This allows clients to access full details about a specific group's properties and configuration. If the group is not found within the system, the endpoint provides appropriate error feedback, ensuring the client is aware of the missing resource while maintaining a secure and predictable API response.
    /// </summary>
    /// <param name="id">The unique identifier of the group to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The detailed group information if found; otherwise, a 404 Not Found status.</returns>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GroupResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GroupResponseDTO>> GetGroup(uint id, CancellationToken cancellationToken)
    {
        var result = await groupService.GetGroup(id, cancellationToken);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // POST: groups
    /// <summary>
    /// Creates a new group within the system using the provided data. The PostGroup endpoint processes requests to establish a new group entity, taking inputs from the PostGroupDTO. This process includes validating the provided data, assigning ownership to the creating user, and persisting the new group to the data store. Upon successful creation, the endpoint returns the newly created group's details and its unique location, following standard RESTful practices for resource creation.
    /// </summary>
    /// <param name="groupDto">The data transfer object containing the initial group configuration.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The details of the newly created group with a 201 Created status.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Group), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Group>> PostGroup(
        [FromForm] PostGroupDTO groupDto,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var created = await groupService.CreateGroup(groupDto, userId, cancellationToken);

        return CreatedAtAction(
            nameof(GetGroup),
            new { id = created.Id },
            created
        );
    }

    // POST: groups/{id}/group-picture
    /// <summary>
    /// Uploads and associates a profile picture with a specific group. The UploadGroupPicture endpoint handles multipart form-data requests to save an image file and link it to the group identified by the provided ID. This endpoint ensures that only authorized administrators of the group can modify its visual identity. Once processed, the path to the stored image is returned, allowing the client to immediately reference the new asset within the application's interface.
    /// </summary>
    /// <param name="id">The unique identifier of the group for which the picture is being uploaded.</param>
    /// <param name="image">The image file to be used as the group's profile picture.</param>
    /// <returns>An OK status containing the new image path upon successful upload.</returns>
    [HttpPost("{id}/group-picture")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UploadPictureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadPictureResponse>> UploadGroupPicture(uint id, IFormFile? image)
    {
        var path = await groupService.UploadGroupPicture(id, GetUserId(), image);
        return Ok(new UploadPictureResponse { Path = path });
    }

    // GET: groups/5/group-picture
    /// <summary>
    /// Retrieves the binary file content of a specific group's profile picture. The GetGroupPicture endpoint fetches the stored image associated with a group and streams it back to the client with the appropriate content type. This endpoint includes checks to ensure both the group and the physical file exist on the server. It provides a direct way for client applications to render group imagery while centralizing the file retrieval logic through the service layer.
    /// </summary>
    /// <param name="id">The unique identifier of the group whose picture is being retrieved.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The image file stream with the correct MIME type.</returns>
    [HttpGet("{id}/group-picture")]
    [Produces("image/webp", "image/jpeg", "image/png", "image/gif")]
    [ProducesResponseType(typeof(Stream), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Stream>> GetGroupPicture(uint id, CancellationToken cancellationToken)
    {
        var group = await groupService.GetGroup(id, cancellationToken);
        if (group == null || string.IsNullOrEmpty(group.GroupPicturePath))
            return NotFound();

        var file = await groupService.GetGroupPictureFile(group.GroupPicturePath);
        if (file == null)
            return NotFound();

        return File(file.Stream, file.ContentType);
    }

    // DELETE: groups/5
    /// <summary>
    /// Permanently removes a group from the system based on its unique identifier. The DeleteGroup endpoint ensures that the requested group is deleted only after verifying that the requesting user has the necessary administrative permissions. This operation is destructive and removes all associated group metadata. Upon successful completion, the endpoint returns a 204 No Content status, signaling that the resource no longer exists without returning an unnecessary response body.
    /// </summary>
    /// <param name="id">The unique identifier of the group to be deleted.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status if deletion is successful.</returns>
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteGroup(uint id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await groupService.DeleteGroup(id, userId, cancellationToken);
        return NoContent();
    }

    // POST: groups/promote-board
    /// <summary>
    /// Promotes candidate board members to board members and resets member special statuses during board rotation.
    /// </summary>
    /// <param name="createNewBoardService">Service handling board rotation.</param>
    /// <returns>A 200 OK status on success.</returns>
    [HttpPost("promote-board")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PromoteBoard([FromServices] ICreateNewBoardService createNewBoardService)
    {
        var userId = GetUserId();
        await createNewBoardService.PromoteCandidateBoardToBoardAsync(userId);
        return Ok();
    }

    // PATCH: groups/5
    /// <summary>
    /// Performs a partial update on an existing group's properties using a JSON Patch document. The PatchGroup endpoint allows clients to modify specific fields of a group without providing the entire resource representation. This is particularly useful for making small adjustments to group metadata while minimizing data transfer. The endpoint validates the patch operations against the group domain model and ensures that the user is authorized to perform these specific modifications before applying changes to the database.
    /// </summary>
    /// <param name="id">The unique identifier of the group to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the set of modifications.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon successful update.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PatchGroup(
        uint id,
        [FromBody] JsonPatchDocument<Group> patchDoc,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await groupService.PatchGroup(id, userId, patchDoc, cancellationToken);
        return NoContent();
    }

    // PUT: groups/5
    /// <summary>
    /// Updates the entire representation of an existing group with the provided data. The PutActivity endpoint replaces the current group information with the data provided in the GroupUpdateDTO. This endpoint is typically used for comprehensive updates where multiple group attributes are changed simultaneously. It enforces authorization to ensure only group managers can perform the update and provides detailed error handling for validation failures or missing resources, returning a 204 No Content status upon a successful operation.
    /// </summary>
    /// <param name="id">The unique identifier of the group to update.</param>
    /// <param name="groupDto">The data transfer object containing the updated group information.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status if the group was successfully updated.</returns>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutGroup(
        uint id,
        GroupUpdateDTO groupDto,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await groupService.UpdateGroup(id, userId, groupDto, cancellationToken);
        return NoContent();
    }

    // GET: groups/{id}/permissions
    /// <summary>
    /// Retrieves the permissions currently granted to a group.
    /// </summary>
    /// <param name="id">The unique identifier of the group.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>The permissions granted to the group.</returns>
    [HttpGet("{id}/permissions")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> GetGroupPermissions(uint id, CancellationToken cancellationToken)
    {
        return Ok(await groupService.GetGroupPermissions(id, cancellationToken));
    }

    // PUT: groups/{id}/permissions
    /// <summary>
    /// Replaces the full set of permissions granted to a group. Each entry is either the string name
    /// of one of the 12 known permissions, or an arbitrary custom string for other applications
    /// sharing this Keycloak instance to interpret - Tavern's own backend only evaluates the known ones.
    /// </summary>
    /// <param name="id">The unique identifier of the group.</param>
    /// <param name="permissions">The full set of permission keys the group should have.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
    /// <returns>A 204 No Content status upon a successful update.</returns>
    [HttpPut("{id}/permissions")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SetGroupPermissions(uint id, [FromBody] List<string> permissions, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await groupService.SetGroupPermissions(id, permissions, userId, cancellationToken);
        return NoContent();
    }
}

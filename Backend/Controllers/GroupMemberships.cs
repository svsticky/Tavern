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
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GroupMembershipsController : ControllerBase
{
    private readonly IGroupMembershipService _groupMembershipService;

    /// <summary>
    /// Initializes a new instance of the GroupMembershipsController class with the specified group membership service. The constructor takes an IGroupMembershipService as a parameter, which is used to perform various operations related to group memberships, such as creating, retrieving, updating, and deleting group memberships. This dependency injection allows for better separation of concerns and promotes a more modular and testable code structure, enabling the controller to focus on handling HTTP requests and responses while delegating the business logic to the service layer.
    /// </summary>
    /// <param name="groupMembershipService">The group membership service for managing group membership operations.</param>
    public GroupMembershipsController(IGroupMembershipService groupMembershipService)
    {
        _groupMembershipService = groupMembershipService;
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: api/groupMemberships
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupMembershipResponseDTO>>> GetGroupMemberships(
        [FromQuery] GetGroupMembershipsDTO dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();

            var result = await _groupMembershipService.GetGroupMemberships(dto, userId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // GET: api/groupMemberships/5
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupMembershipResponseDTO>> GetGroupMembership(uint id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            var result = await _groupMembershipService.GetGroupMembership(id, userId, cancellationToken);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST: api/groupMemberships
    [HttpPost]
    public async Task<ActionResult<GroupMembership>> PostGroupMembership(
        PostGroupMembershipDTO membershipDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            var created = await _groupMembershipService.CreateGroupMembership(membershipDto, userId, cancellationToken);

            return CreatedAtAction(
                nameof(GetGroupMembership),
                new { id = created.Id },
                created
            );
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // DELETE: api/groupMemberships/5
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteGroupMembership(uint id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            await _groupMembershipService.DeleteGroupMembership(id, userId, cancellationToken);

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
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // PATCH: api/groupMemberships/5
    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchGroupMembership(
        uint id,
        [FromBody] JsonPatchDocument<GroupMembership> patchDoc,
        CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            return BadRequest();

        try
        {
            var userId = GetUserId();

            await _groupMembershipService.PatchGroupMembership(id, userId, patchDoc, cancellationToken);

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
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // PUT: api/groupMemberships/5
    [HttpPut("{id}")]
    public async Task<ActionResult> PutGroupMembership(
        uint id,
        GroupMembershipUpdateDTO membershipDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            await _groupMembershipService.UpdateGroupMembership(id, userId, membershipDto, cancellationToken);

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
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
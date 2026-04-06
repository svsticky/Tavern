using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GroupMembershipsController : ControllerBase
{
    private readonly IGroupMembershipService _groupMembershipService;

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
        [FromQuery] bool onlyOwnMemberships = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();

            var result = await _groupMembershipService.GetGroupMemberships(userId, onlyOwnMemberships, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
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
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
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
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
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
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
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
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
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
            return StatusCode(500, ex.Message);
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
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
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
            return StatusCode(500, ex.Message);
        }
    }
}
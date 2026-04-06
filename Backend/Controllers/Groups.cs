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
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // GET: api/groups
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupResponseDTO>>> GetGroups(
        [FromQuery] GetGroupDTO dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            var result = await _groupService.GetGroups(userId, dto, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    // GET: api/groups/5
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupResponseDTO>> GetGroup(uint id, CancellationToken cancellationToken)
    {
        var result = await _groupService.GetGroup(id, cancellationToken);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // POST: api/groups
    [HttpPost]
    public async Task<ActionResult<Group>> PostGroup(
        PostGroupDTO groupDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            var created = await _groupService.CreateGroup(groupDto, userId, cancellationToken);

            return CreatedAtAction(
                nameof(GetGroup),
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

    // DELETE: api/groups/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(uint id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            await _groupService.DeleteGroup(id, userId, cancellationToken);

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

    // PATCH: api/groups/5
    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchGroup(
        uint id,
        [FromBody] JsonPatchDocument<Group> patchDoc,
        CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            return BadRequest();

        try
        {
            var userId = GetUserId();

            await _groupService.PatchGroup(id, userId, patchDoc, cancellationToken);

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

    // PUT: api/groups/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutGroup(
        uint id,
        GroupUpdateDTO groupDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            await _groupService.UpdateGroup(id, userId, groupDto, cancellationToken);

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
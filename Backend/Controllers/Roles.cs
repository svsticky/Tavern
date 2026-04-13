using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _service;

    public RolesController(IRoleService service)
    {
        _service = service;
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Role>>> GetRoles(CancellationToken ct)
    {
        return Ok(await _service.GetRoles(ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Role>> GetRole(uint id, CancellationToken ct)
    {
        var role = await _service.GetRole(id, ct);
        return role != null ? Ok(role) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult> PostRole(PostRoleDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateRole(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetRole), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(uint id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteRole(id, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchRole(uint id, JsonPatchDocument<Role> patchDoc, CancellationToken ct)
    {
        try
        {
            await _service.PatchRole(id, patchDoc, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> PutRole(uint id, RoleUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateRole(id, dto, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
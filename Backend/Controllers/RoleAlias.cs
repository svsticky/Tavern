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
public class RoleAliasesController : ControllerBase
{
    private readonly IRoleAliasService _service;

    public RoleAliasesController(IRoleAliasService service)
    {
        _service = service;
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleAlias>>> GetRoleAliases(CancellationToken ct)
    {
        return Ok(await _service.GetRoleAliases(ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleAlias>> GetRoleAlias(uint id, CancellationToken ct)
    {
        var result = await _service.GetRoleAlias(id, ct);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> PostRoleAlias(PostRoleAliasDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateRoleAlias(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetRoleAlias), new { id = result.Id }, result);
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
    public async Task<IActionResult> DeleteRoleAlias(uint id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteRoleAlias(id, GetUserId(), ct);
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
    public async Task<IActionResult> PatchRoleAlias(uint id, JsonPatchDocument<RoleAlias> patchDoc, CancellationToken ct)
    {
        try
        {
            await _service.PatchRoleAlias(id, patchDoc, GetUserId(), ct);
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
    public async Task<IActionResult> PutRoleAlias(uint id, RoleAliasUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateRoleAlias(id, dto, GetUserId(), ct);
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
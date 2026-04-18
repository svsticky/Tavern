using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class Settings : ControllerBase
{
    private readonly ISettingsService _service;

    public Settings(ISettingsService service)
    {
        _service = service;
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Setting>>> GetSettings(CancellationToken ct)
    {
        try
        {
            var result = await _service.GetSettings(GetUserId(), ct);
            return Ok(result);
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

    [HttpGet("{id}")]
    public async Task<ActionResult<Setting>> GetSetting([FromQuery] string id, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetSetting(id, GetUserId(), ct);
            return result != null ? Ok(result) : NotFound();
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

    [HttpPost]
    public async Task<ActionResult> PostSetting([FromQuery] string id, [FromQuery] string value, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateSetting(id, value, GetUserId(), ct);
            return CreatedAtAction(nameof(GetSetting), new { id = result.Name }, result);
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
    public async Task<ActionResult> DeleteSetting([FromQuery] string id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteSetting(id, GetUserId(), ct);
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
    public async Task<ActionResult> PatchSetting([FromQuery] string id, JsonPatchDocument<Setting> patchDoc, CancellationToken ct)
    {
        try
        {
            await _service.PatchSetting(id, patchDoc, GetUserId(), ct);
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
    public async Task<ActionResult> PutSetting([FromQuery] string id, [FromQuery] string value, CancellationToken ct)
    {
        try
        {
            await _service.UpdateSetting(id, value, GetUserId(), ct);
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
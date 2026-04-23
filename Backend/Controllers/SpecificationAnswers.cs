using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SpecificationAnswers(ISpecificationAnswerService service) : ControllerBase
{
    [HttpPatch("{answerId}")]
    public async Task<ActionResult> PatchSpecificationAnswer(uint answerId, [FromBody] JsonPatchDocument<SpecificationAnswer> patchDoc, CancellationToken ct)
    {
        try
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            await service.PatchSpecificationAnswersAsync(userId, answerId, patchDoc, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
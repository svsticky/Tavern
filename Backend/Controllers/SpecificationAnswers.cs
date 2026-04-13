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
        if (patchDoc == null)
            return BadRequest("Invalid patch document.");

        var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
        try
        {
            await service.PatchSpecificationAnswersAsync(userId, answerId, patchDoc);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
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
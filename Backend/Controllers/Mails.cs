using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MailsController : ControllerBase
{
    private readonly AbstractMailService _service;

    public MailsController(AbstractMailService service)
    {
        _service = service;
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }
    
    [HttpPost]
    public async Task<ActionResult> PostMail(AbstractPostMailDTO dto, CancellationToken ct)
    {
        try
        {
            Guid userId = GetUserId();

            switch (dto)
            {
                case PostMailDTO normalMail:
                    await _service.SendEmailAsync(normalMail, userId, ct);
                    break;

                case PostActivityMailDTO activityMail:
                    await _service.SendEmailAsync(activityMail, userId, ct);
                    break;

                default:
                    return BadRequest("Unknown mail type.");
            }

            return Ok();
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
}
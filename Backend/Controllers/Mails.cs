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
    
    [HttpPost("normal")]
    public async Task<ActionResult> PostNormalMail(PostMailDTO dto, CancellationToken ct)
    {
        await _service.SendEmailAsync(dto, GetUserId(), ct);
        return Ok();
    }

    [HttpPost("activity")]
    public async Task<ActionResult> PostActivityMail(PostActivityMailDTO dto, CancellationToken ct)
    {
        await _service.SendEmailAsync(dto, GetUserId(), ct);
        return Ok();
    }
}
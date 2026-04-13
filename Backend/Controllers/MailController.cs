using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MailController : ControllerBase
{
    private readonly AbstractMailService _mailService;

    public MailController(AbstractMailService mailService)
    {
        _mailService = mailService;
    }

    [HttpPost]
    public async Task<ActionResult> PostMail(PostMailDTO dto, CancellationToken ct)
    {
        var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        try
        {
            await _mailService.SendEmailAsync(dto, userId, ct);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
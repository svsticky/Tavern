using Backend.Controllers.DTOs;
using Backend.Services.MailServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing email communications within the system. The MailsController provides centralized endpoints for sending various types of emails, including general correspondence and activity-specific notifications. This controller ensures that all mailing operations are authenticated and authorized, leveraging the AbstractMailService to handle the underlying delivery logic and template processing. By encapsulating mail operations here, the application maintains a consistent approach to user communication while enforcing security policies and providing robust error handling for email delivery scenarios.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class MailsController : ControllerBase
{
    private readonly AbstractMailService _service;

    /// <summary>
    /// Initializes a new instance of the MailsController with the required mail service.
    /// </summary>
    /// <param name="service">The abstract mail service used for email dispatch operations.</param>
    public MailsController(AbstractMailService service)
    {
        _service = service;
    }

    /// <summary>
    /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>A Guid representing the authenticated user's ID.</returns>
    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    // POST: mails/normal
    /// <summary>
    /// Sends a standard email based on the provided recipient and content data. The PostNormalMail endpoint allows authorized users to dispatch general-purpose emails by providing a PostMailDTO containing the necessary details such as recipient address, subject, and body content. This endpoint is designed to facilitate flexible communication within the system, ensuring that the request is validated and the sender is authorized before the mail service processes the delivery. Upon successful dispatch, the endpoint returns a 200 OK status, confirming that the email has been queued or sent successfully.
    /// </summary>
    /// <param name="dto">The data transfer object containing the standard email details.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>An OK status code if the email was sent successfully.</returns>
    [HttpPost("normal")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PostNormalMail(PostMailDTO dto, CancellationToken ct)
    {
        await _service.SendEmailAsync(dto, GetUserId(), ct);
        return Ok();
    }

    // POST: mails/activity
    /// <summary>
    /// Sends an activity-specific email notification using specialized templates and data. The PostActivityMail endpoint is designed to handle communications related specifically to system activities, such as enrollment confirmations or activity updates. By utilizing the PostActivityMailDTO, clients can trigger emails that are context-aware, ensuring that relevant activity data is correctly injected into the communication. This endpoint enforces strict authorization to prevent unauthorized users from sending activity-related notifications and provides clear feedback through appropriate HTTP status codes in case of delivery failure or permission issues.
    /// </summary>
    /// <param name="dto">The data transfer object containing activity-specific email parameters.</param>
    /// <param name="ct">The cancellation token to monitor for request cancellation.</param>
    /// <returns>An OK status code if the activity email was sent successfully.</returns>
    [HttpPost("activity")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PostActivityMail(PostActivityMailDTO dto, CancellationToken ct)
    {
        await _service.SendEmailAsync(dto, GetUserId(), ct);
        return Ok();
    }
}

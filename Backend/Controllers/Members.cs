using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MembersController(IMemberService memberService, IProfilePictureService profilePictureService, KeycloakOutboxWorker keycloakOutboxWorker) : ControllerBase
    {
        private readonly string? _keycloakWebhookSecret = Environment.GetEnvironmentVariable("KEYCLOAK_WEBHOOK_SECRET");
        private Guid GetUserId()
        {
            return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberResponseDTO>>> GetMembers([FromQuery] GetMembersDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                var result = await memberService.GetMembers(dto, userId, cancellationToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MemberResponseDTO>> GetMember(Guid id, CancellationToken cancellationToken)
        {

            try
            {
                var userId = GetUserId();
                var result = await memberService.GetMember(id, userId, cancellationToken);
                if (result == null) return NotFound();

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<Member>> PostMember(PostMemberDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var member = await memberService.CreateMember(dto, cancellationToken);
                return CreatedAtAction(nameof(GetMember), new { id = member.Id }, member);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMember(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                await memberService.DeleteMember(id, userId, cancellationToken);

                return NoContent();
            }
            catch(UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch(KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500, "Error deleting member.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> PatchMember(Guid id, JsonPatchDocument<Member> patchDoc, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                await memberService.PatchMember(id, patchDoc, userId, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> PutMember(Guid id, MemberUpdateDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                await memberService.UpdateMember(id, dto, userId, cancellationToken);

                return NoContent();
            }
            catch(UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/profile-picture")]
        public async Task<IActionResult> GetProfilePicture(Guid id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var member = await memberService.GetMember(id, userId, cancellationToken);
            if (member == null || string.IsNullOrEmpty(member.ProfilePicturePath))
                return NotFound("Member or profile picture not found.");

            var file = await profilePictureService.GetProfilePictureByPath(member.ProfilePicturePath, cancellationToken);
            if (file == null)
                return NotFound("File is no longer present on the server.");

            return File(file.Value.Stream, file.Value.ContentType);
        }

        [HttpDelete("{id}/profile-picture")]
        public async Task<ActionResult> DeleteProfilePicture(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
                await memberService.DeleteProfilePicture(id, userId, cancellationToken);
                return NoContent();
            }
            catch(UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("webhook/refresh-email")]
        public async Task<IActionResult> UpdateEmailWebhook([FromHeader(Name = "X-Webhook-Secret")] string secret, [FromBody] Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                if (_keycloakWebhookSecret == null || secret != _keycloakWebhookSecret)
                {
                    return Unauthorized("Invalid webhook secret.");
                }

                await keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.RefreshEmail, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
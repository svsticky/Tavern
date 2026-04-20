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
    public class MembersController(IMemberService memberService, IPermissionService permissionService, KeycloakOutboxWorker keycloakOutboxWorker) : ControllerBase
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
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MemberResponseDTO>> GetMember(Guid id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var isBoard = permissionService.IsBoardOrCandidateBoardMember(userId);

            try
            {
                var result = await memberService.GetMember(id, userId, isBoard, cancellationToken);
                if (result == null) return NotFound();

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
            catch (Exception)
            {
                return StatusCode(500, "Error creating member.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMember(Guid id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (!permissionService.IsBoardOrCandidateBoardMember(userId) && id != userId)
                return Forbid();

            try
            {
                var success = await memberService.DeleteMember(id, userId, cancellationToken);
                if (!success) return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error deleting member.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> PatchMember(Guid id, JsonPatchDocument<Member> patchDoc, CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (patchDoc == null)
                return BadRequest();

            try
            {
                var success = await memberService.PatchMember(id, patchDoc, userId, cancellationToken);
                if (!success) return NotFound();

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "Error updating member.");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> PutMember(Guid id, MemberUpdateDTO dto, CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (!permissionService.IsBoardOrCandidateBoardMember(userId) && id != userId)
                return Forbid();

            try
            {
                var success = await memberService.UpdateMember(id, dto, userId, cancellationToken);
                if (!success) return NotFound();

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error updating member.");
            }
        }

        [HttpGet("{id}/profile-picture")]
        public async Task<IActionResult> GetProfilePicture(Guid id)
        {
            var member = await memberService.GetMemberEntity(id);
            if (member == null || string.IsNullOrEmpty(member.ProfilePicturePath))
                return NotFound("Member or profile picture not found.");

            var file = await memberService.GetProfilePictureFile(member.ProfilePicturePath);
            if (file == null)
                return NotFound("File is no longer present on the server.");

            return File(file.Stream, file.ContentType);
        }

        [HttpDelete("{id}/profile-picture")]
        public async Task<ActionResult> DeleteProfilePicture(Guid id)
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if (id != userId && !permissionService.IsBoardOrCandidateBoardMember(userId))
                return Forbid("You can only delete your own profile picture.");

            var success = await memberService.DeleteProfilePicture(id);
            if (!success) return NotFound();

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("webhook/refresh-email")]
        public async Task<IActionResult> UpdateEmailWebhook([FromHeader(Name = "X-Webhook-Secret")] string secret, [FromBody] Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                if (_keycloakWebhookSecret == null || secret == _keycloakWebhookSecret)
                {
                    return Unauthorized("Invalid webhook secret.");
                }

                await keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.RefreshEmail, userId);
                return Ok();
            }
            catch
            {
                return StatusCode(500, "Error updating email via webhook.");
            }
        }
    }
}
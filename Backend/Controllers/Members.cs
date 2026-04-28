using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// Controller for managing system members and their profiles. The MembersController provides a comprehensive suite of endpoints for user lifecycle management, including registration, profile retrieval, data updates, and account deletion. It also handles profile picture management and integrates with identity provider webhooks to ensure data synchronization. This controller enforces security through role-based or ownership-based authorization while allowing specific public actions like self-registration. By coordinating between the IMemberService and specialized services like the KeycloakOutboxWorker, it ensures that member data remains consistent across the internal database and external identity providers.
    /// </summary>
    /// <param name="memberService">The service responsible for member business logic and persistence.</param>
    /// <param name="profilePictureService">The service dedicated to handling profile picture file operations.</param>
    /// <param name="keycloakOutboxWorker">The background worker used for synchronizing identity provider data.</param>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MembersController(IMemberService memberService, IProfilePictureService profilePictureService) : ControllerBase
    {
        private readonly string? _keycloakWebhookSecret = Environment.GetEnvironmentVariable("KEYCLOAK_WEBHOOK_SECRET");

        /// <summary>
        /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
        /// </summary>
        /// <returns>A Guid representing the authenticated user's ID.</returns>
        private Guid GetUserId()
        {
            return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
        }

        // GET: api/members
        /// <summary>
        /// Retrieves a filtered and paginated list of members. The GetMembers endpoint allows authorized users to search through the system's member directory using criteria specified in the GetMembersDto. This endpoint is designed to return summarized member profiles, facilitating directory browsing and administrative oversight while respecting privacy constraints and authorization rules.
        /// </summary>
        /// <param name="dto">The data transfer object containing search filters and pagination settings.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A collection of member response objects matching the query.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberResponseDTO>>> GetMembers([FromQuery] GetMembersDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                var result = await memberService.GetMembers(dto, userId, cancellationToken);
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

        // GET: api/members/{id}
        /// <summary>
        /// Retrieves the detailed profile of a specific member by their unique identifier. The GetMember endpoint provides full access to a single member's data, including contact information and system preferences. It ensures that the requesting user has the appropriate permissions to view the target member's details, returning a 404 status if the member does not exist or a 403 status if access is denied.
        /// </summary>
        /// <param name="id">The unique identifier (Guid) of the member to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
        /// <returns>The detailed member profile if found and authorized.</returns>
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

        // POST: api/members
        /// <summary>
        /// Registers a new member in the system. The PostMember endpoint is accessible without authentication to allow for new user sign-ups. It processes the PostMemberDTO to create a new member record, validates the input for business rule compliance (such as email uniqueness), and returns the created member details. This endpoint serves as the primary entry point for user onboarding.
        /// </summary>
        /// <param name="dto">The data transfer object containing registration information.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
        /// <returns>The newly created member object with a 201 Created status.</returns>
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

        // DELETE: api/members/{id}
        /// <summary>
        /// Permanently deletes a member's account and associated data. The DeleteMember endpoint handles the removal of a member from the system, ensuring that only the member themselves or an administrator can execute the action. This operation involves cleaning up related resources and may trigger cascading deletions where appropriate to maintain data integrity and comply with privacy regulations.
        /// </summary>
        /// <param name="id">The unique identifier of the member to be deleted.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A 204 No Content status upon successful deletion.</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMember(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                await memberService.DeleteMember(id, userId, cancellationToken);

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500, "Error deleting member.");
            }
        }

        // PATCH: api/members/{id}
        /// <summary>
        /// Updates specific fields of a member's profile using JSON Patch. The PatchMember endpoint provides a flexible way to modify individual attributes of a member record without requiring the full object. This is ideal for background updates or specific profile settings changes, ensuring that only the specified fields are touched while validating the resulting state against domain requirements.
        /// </summary>
        /// <param name="id">The unique identifier of the member to update.</param>
        /// <param name="patchDoc">The JSON Patch document containing the intended modifications.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A 204 No Content status upon successful application of the patch.</returns>
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

        // PUT: api/members/{id}
        /// <summary>
        /// Performs a full update of a member's profile data. The PutMember endpoint replaces the existing member information with the data provided in the MemberUpdateDTO. This is used for comprehensive profile edits where a user modifies several aspects of their account at once. The endpoint verifies ownership and validates the new data before persisting changes.
        /// </summary>
        /// <param name="id">The unique identifier of the member to update.</param>
        /// <param name="dto">The data transfer object containing the complete updated profile information.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A 204 No Content status if the update was successful.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult> PutMember(Guid id, MemberUpdateDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                await memberService.UpdateMember(id, dto, userId, cancellationToken);

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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

        // GET: api/members/{id}/profile-picture
        /// <summary>
        /// Retrieves the profile picture file for a specific member. The GetProfilePicture endpoint locates the image asset associated with the member's profile and streams it to the client with the correct content type. This allows the application to dynamically render user avatars while keeping the storage logic abstracted within the service layer.
        /// </summary>
        /// <param name="id">The unique identifier of the member whose picture is being requested.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
        /// <returns>The image file stream or a 404 status if the member or file is missing.</returns>
        [HttpGet("{id}/profile-picture")]
        public async Task<IActionResult> GetProfilePicture(Guid id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var member = await memberService.GetMember(id, userId, cancellationToken);
            if (member == null || string.IsNullOrEmpty(member.ProfilePicturePath))
                return NotFound("Member or profile picture not found.");

            var file = await profilePictureService.GetProfilePictureByPath(member.ProfilePicturePath);
            if (file == null)
                return NotFound("File is no longer present on the server.");

            return File(file.Value.Stream, file.Value.ContentType);
        }

        // DELETE: api/members/{id}/profile-picture
        /// <summary>
        /// Removes a member's profile picture and reverts their profile to use a default avatar. The DeleteProfilePicture endpoint handles the deletion of the physical image file and updates the member's record to clear the picture path. This action is restricted to the account owner or authorized staff.
        /// </summary>
        /// <param name="id">The unique identifier of the member whose picture is to be removed.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A 204 No Content status upon successful removal.</returns>
        [HttpDelete("{id}/profile-picture")]
        public async Task<ActionResult> DeleteProfilePicture(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
                await memberService.DeleteProfilePicture(id, userId, cancellationToken);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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

        // POST: api/members/webhook/refresh-email
        /// <summary>
        /// Handles incoming webhooks from Keycloak to synchronize email changes. The UpdateEmailWebhook endpoint is a specialized administrative entry point that listens for external signals regarding identity updates. It validates the request using a shared secret and enqueues a background task to refresh the member's email address in the local database, ensuring the application stays in sync with the central identity provider.
        /// </summary>
        /// <param name="secret">the shared webhook secret provided in the request headers.</param>
        /// <param name="userId">The unique identifier of the user whose email needs refreshing.</param>
        /// <param name="cancellationToken">The cancellation token to monitor for request cancellation.</param>
        /// <returns>A 200 OK status if the task was successfully enqueued.</returns>
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

                await memberService.RefreshEmail(userId, cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
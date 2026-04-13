using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Backend.Models;
using Backend.Controllers.DTOs;
using Microsoft.AspNetCore.Authorization;
using Backend.Interfaces;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivitiesController(IActivityService service) : ControllerBase
    {
        private Guid GetUserId()
        {
            return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
        }

        // GET: api/activities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActivityResponseDTO>>> GetActivities([FromQuery] GetActivitiesDTO dto)
        {
            try
            {
                var result = await service.GetActivities(GetUserId(), dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // GET: api/activities/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ActivityResponseDTO>> GetActivity(uint id)
        {
            try
            {
                var activity = await service.GetActivity(GetUserId(), id);

                if (activity == null)
                    return NotFound();

                return Ok(activity);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // POST: api/activities
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Activity>> PostActivity([FromForm] PostActivityDTO dto)
        {
            try
            {
                var activity = await service.CreateActivity(GetUserId(), dto);

                return CreatedAtAction(nameof(GetActivity), new { id = activity.Id }, activity);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // DELETE: api/activities/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteActivity(uint id)
        {
            try
            {
                await service.DeleteActivity(GetUserId(), id);
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
        }

        // PATCH: api/activities/5
        [HttpPatch("{id}")]
        public async Task<ActionResult> PatchActivity(uint id, [FromBody] JsonPatchDocument<Activity> patchDoc, CancellationToken ct)
        {
            try
            {
                await service.PatchActivity(GetUserId(), id, patchDoc, ct);
                return NoContent();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // POST: api/activities/5/poster
        [HttpPost("{id}/poster")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadPoster(uint id, IFormFile? poster)
        {
            try
            {
                await service.UploadPoster(GetUserId(), id, poster);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // PUT: api/activities/5
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> PutActivity(uint id, [FromForm] PutActivityDTO dto)
        {
            try
            {
                await service.PutActivity(GetUserId(), id, dto);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // GET: api/activities/5/poster
        [HttpGet("{id}/poster")]
        public async Task<IActionResult> GetPoster(uint id)
        {
            try
            {
                var result = await service.GetPoster(GetUserId(), id, download: false);

                if (result == null)
                    return NotFound();

                return File(result.Value.Stream, result.Value.ContentType);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // GET: api/activities/5/poster/download
        [HttpGet("{id}/poster/download")]
        public async Task<IActionResult> DownloadPoster(uint id)
        {
            try
            {
                var result = await service.GetPoster(GetUserId(), id, download: true);

                if (result == null)
                    return NotFound();

                return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
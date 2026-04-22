using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfilePictureController : ControllerBase
    {
        private readonly IProfilePictureService _service;

        public ProfilePictureController(IProfilePictureService service)
        {
            _service = service;
        }

        // GET: api/profilepicture/view/{path}
        [HttpGet("view/{path}")]
        public async Task<IActionResult> GetProfilePictureByPath(string path)
        {
            var result = await _service.GetProfilePictureByPath(path);

            if (result == null)
                return NotFound();

            return File(result.Value.Stream, result.Value.ContentType);
        }

        // POST: api/profilepicture/{id}/profile-picture
        [HttpPost("{id}/profile-picture")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadProfilePicture(Guid id, IFormFile? image)
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            try
            {
                var path = await _service.UploadProfilePicture(id, userId, image);

                return Ok(new { path });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
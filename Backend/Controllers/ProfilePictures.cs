using Microsoft.AspNetCore.Mvc;
using Backend.Database;
using Microsoft.EntityFrameworkCore;
using Backend.Utils;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfilePictureController(IStorageService storageService) : ControllerBase
    {
        // GET: api/profilepicture/view/{path}
        /// <summary>
        /// Retrieves a profile picture by its path.
        /// </summary>
        [HttpGet("view/{path}")]
        public async Task<IActionResult> GetProfilePictureByPath(string path)
        {
            var decodedPath = Uri.UnescapeDataString(path);
            
            var file = await storageService.GetFileAsync("profile-pictures", decodedPath);
            if (file == null)
            {
                return NotFound();
            }

            return File(file.Stream, file.ContentType);
        }

    }
}
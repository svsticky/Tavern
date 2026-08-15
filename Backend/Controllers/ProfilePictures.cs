using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// Controller for managing profile picture assets across the system. The ProfilePictureController provides dedicated endpoints for the retrieval and uploading of user avatars. It serves as a specialized handler for file-based operations, ensuring that image data is correctly processed, stored, and served with the appropriate MIME types. This controller integrates with the IProfilePictureService to abstract the underlying storage mechanism—whether local or cloud-based—while enforcing authorization rules to ensure that only authenticated users can access or modify personal profile imagery.
    /// </summary>
    /// <param name="profilePictureService">The service responsible for file system interactions and image processing logic.</param>
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class ProfilePictureController(IProfilePictureService profilePictureService) : ControllerBase
    {
        // GET: profilepicture/view/{path}
        /// <summary>
        /// Retrieves and streams a profile picture file based on its storage path. The GetProfilePictureByPath endpoint allows the system to serve image assets directly to the client. By providing the internal path, the endpoint retrieves the file stream and returns it with the correct content-type header, enabling browsers and applications to render the image correctly. This approach avoids exposing direct file system paths to the client and centralizes image delivery through a secured API route.
        /// </summary>
        /// <param name="path">The encoded or relative path to the profile picture asset.</param>
        /// <returns>A file stream result containing the image data; otherwise, a 404 Not Found status.</returns>
        [HttpGet("view/{path}")]
        [Produces("image/webp", "image/jpeg", "image/png")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Stream>> GetProfilePictureByPath(string path)
        {
            var result = await profilePictureService.GetProfilePictureByPath(path);

            if (result == null)
                return NotFound();

            return File(result.Value.Stream, result.Value.ContentType);
        }

        // POST: profilepicture/{id}/profile-picture
        /// <summary>
        /// Uploads a new profile picture for a specific member. The UploadProfilePicture endpoint processes multipart form-data containing an image file and associates it with the member identified by the provided ID. This endpoint ensures that the upload is authorized by verifying the requester's identity against the target profile. Upon successful processing, it returns the generated path of the stored asset, which can then be used for subsequent retrieval or profile updates.
        /// </summary>
        /// <param name="id">The unique identifier of the member for whom the picture is being uploaded.</param>
        /// <param name="image">The image file provided via the form-data request.</param>
        /// <returns>An OK status containing the newly generated asset path.</returns>
        [HttpPost("{id}/profile-picture")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(UploadPictureResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UploadPictureResponse>> UploadProfilePicture(Guid id, IFormFile? image)
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var path = await profilePictureService.UploadProfilePicture(id, userId, image);

            return Ok(new UploadPictureResponse { Path = path });
        }
    }
}

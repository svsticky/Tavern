using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Backend.Database;
using Backend.Models;
using Backend.Controllers.DTOs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Backend.Utils;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Activities(PostgresDbContext db) : ControllerBase
    {
        private readonly string[] _allowedExtensions = ["jpg", "jpeg", "png", "gif", "pdf"];

        // GET: api/activities
        /// <summary>
        /// Lists all activities in the database.
        /// </summary>
        /// <returns>Said list.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Activity>>> GetActivities()
        {
            return await db.Activities.ToListAsync();
        }

        // GET: api/activities/5
        /// <summary>
        /// Fetches a single activity.
        /// </summary>
        /// <param name="id">The id of the activity to fetch.</param>
        /// <returns>The full activity.</returns> // TODO: perhaps replace this with a DTO to prevent exposing unneeded fields?
        [HttpGet("{id}")]
        public async Task<ActionResult<Activity>> GetActivity(uint id)
        {
            Activity? activity = await db.Activities.FindAsync(id);

            return activity != null ? activity : NotFound();
        }

        // POST: api/activities
        /// <summary>
        /// Creates a new activity with a unique ID assigned by the database.
        /// </summary>
        /// <param name="activity">The activity to be added to the database.</param>
        /// <returns>Fully created activity in body and api route of where to fetch it in the headers.</returns>
        [HttpPost]
        public async Task<ActionResult<Activity>> PostActivity(PostActivityDTO activityDto)
        {
            Activity newActivity = new()
            {
                Name = activityDto.Name,
                Price = activityDto.Price,
                DutchDescription = activityDto.DutchDescription,
                EnglishDescription = activityDto.EnglishDescription,
                DateTimeStart = activityDto.DateTimeStart,
                DateTimeEnd = activityDto.DateTimeEnd,
                UnenrollmentDeadline = activityDto.UnenrollmentDeadline,
                Location = activityDto.Location,
                ParticipantLimit = activityDto.ParticipantLimit,
                OrganizerId = activityDto.OrganizerId,
                ShowInKoala = activityDto.ShowInKoala,
                ShowOnWebsite = activityDto.ShowOnWebsite,
                IsEnrollable = activityDto.IsEnrollable,
                AreParticipantsVisible = activityDto.AreParticipantsVisible,
                IsAdultOnly = activityDto.IsAdultOnly
            };

            if(activityDto.Poster != null)
            {
                try
                {
                    newActivity.PosterPath = await PosterUtils.SavePosterAsync(activityDto.Poster);
                    newActivity.PosterFileName = activityDto.Poster.FileName;
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            EntityEntry<Activity> newEntry = db.Activities.Add(newActivity);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetActivity), new { id = newEntry.Entity.Id }, newEntry.Entity);
        }

        // DELETE: api/activities/5
        /// <summary>
        /// Deletes an activity.
        /// </summary>
        /// <param name="id">The id of the activity to delete.</param>
        /// <returns>Nothing, really.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActivity(uint id)
        {
            Activity? activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            if(activity.PosterPath != null)
            {
                System.IO.File.Delete(activity.PosterPath);
            }

            db.Activities.Remove(activity);
            await db.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/activities/5
        /// <summary>
        /// Partially updates an activity's details.
        /// </summary>
        /// <param name="id">The id of the activity to update.</param>
        /// <param name="patchDoc">The patch document containing the changes.</param>
        /// <returns>No Content.</returns>
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchActivity(uint id, [FromBody] JsonPatchDocument<Activity> patchDoc, CancellationToken cancellationToken)
        {
            if (patchDoc == null)
                return BadRequest();

            Activity? activity = await db.Activities.FindAsync(new object[] { id }, cancellationToken);
            if (activity == null)
                return NotFound();

            patchDoc.ApplyTo(activity, ModelState);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // POST: api/activities/5/poster
        /// <summary>
        /// Uploads or replaces the poster of an activity. If a poster already exists, it will be deleted from the server after the new one is successfully saved and linked to the activity in the database, to prevent orphaned files. If no file is provided, the existing poster will be removed without replacement. This endpoint allows clients to manage activity posters separately from other activity details, which can be useful for performance and user experience when only the poster needs to be updated.
        /// </summary>
        /// <param name="id">The id of the activity for which to upload or replace the poster.</param>
        /// <param name="poster">The new poster file to upload. If null, the existing poster will be removed.</param>
        /// <returns>Ok with the new poster path if successful, or an error message if something goes wrong.</returns>
        [HttpPost("{id}/poster")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPoster(uint id, IFormFile? poster)
        {
            var activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            string? oldPath = activity.PosterPath;

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if(poster != null)
                {
                    string newFileName = Guid.NewGuid().ToString() + Path.GetExtension(poster.FileName);
                    string newPath = Path.Combine("Posters", newFileName);
                    await FileUtils.SaveFileAsync(poster, newPath);
                    activity.PosterPath = newPath;
                    activity.PosterFileName = poster.FileName;
                }
                else
                {
                    activity.PosterPath = null;
                    activity.PosterFileName = null;
                }

                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                if (!string.IsNullOrEmpty(oldPath) && System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }

                return Ok(new { path = activity.PosterPath });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error uploading poster.");
            }
        }
        
        // PUT: api/activities/5
        /// <summary>
        /// Updates an activity.
        /// </summary>
        /// <param name="id">The id of the activity to update.</param>
        /// <param name="activityDto">The new details of the activity.</param>
        /// <returns>No content.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutActivity(uint id, PostActivityDTO activityDto)
        {
            Activity? activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            activity.Name = activityDto.Name;
            activity.Price = activityDto.Price;
            activity.DutchDescription = activityDto.DutchDescription;
            activity.EnglishDescription = activityDto.EnglishDescription;
            activity.DateTimeStart = activityDto.DateTimeStart;
            activity.DateTimeEnd = activityDto.DateTimeEnd;
            activity.UnenrollmentDeadline = activityDto.UnenrollmentDeadline;
            activity.Location = activityDto.Location;
            activity.ParticipantLimit = activityDto.ParticipantLimit;
            activity.OrganizerId = activityDto.OrganizerId;
            activity.ShowInKoala = activityDto.ShowInKoala;
            activity.ShowOnWebsite = activityDto.ShowOnWebsite;
            activity.IsEnrollable = activityDto.IsEnrollable;
            activity.AreParticipantsVisible = activityDto.AreParticipantsVisible;
            activity.IsAdultOnly = activityDto.IsAdultOnly;
            activity.IsOpenToFirstYears = activityDto.IsOpenToFirstYears;
            activity.IsOpenToSecondYears = activityDto.IsOpenToSecondYears;
            activity.IsOpenToThirdYearsAndAbove = activityDto.IsOpenToThirdYearsAndAbove;
            activity.IsOpenToMasters = activityDto.IsOpenToMasters;
            activity.IsOpenForPayment = activityDto.IsOpenForPayment;
            activity.VatRate = activityDto.VatRate;
            activity.GLAccountId = activityDto.GLAccountId;
            activity.CostCenterId = activityDto.CostCenterId;
            activity.CostUnitId = activityDto.CostUnitId;

            using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

            try
            {
                string? existingPosterPath = activity.PosterPath;

                if(activityDto.Poster != null)
                {
                    try
                    {
                        activity.PosterPath = await PosterUtils.SavePosterAsync(activityDto.Poster);
                        activity.PosterFileName = activityDto.Poster.FileName;
                    }
                    catch (InvalidOperationException ex)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(ex.Message);
                    }
                }
                else
                {
                    activity.PosterFileName = null;
                    activity.PosterPath = null;
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                if(existingPosterPath != null)
                {
                    System.IO.File.Delete(existingPosterPath);
                    activity.PosterPath = null;
                    activity.PosterFileName = null;
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while updating the activity.");
            }

            return NoContent();
        }

        // GET: api/activities/5/poster
        /// <summary>
        /// Views or downloads the poster of an activity, depending on the client's needs.
        /// </summary>
        [HttpGet("{id}/poster")]
        public async Task<IActionResult> GetPoster(uint id)
        {
            var activity = await db.Activities.FindAsync(id);

            if (activity == null || string.IsNullOrEmpty(activity.PosterPath))
                return NotFound("Activity or poster not found.");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), activity.PosterPath);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File is no longer present on the server.");

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(filePath, contentType);
        }

        // GET: api/activities/5/poster/download
        /// <summary>
        /// Downloads the poster of an activity as an attachment, prompting the client to save it with the original filename.
        /// </summary>
        [HttpGet("{id}/poster/download")]
        public async Task<IActionResult> DownloadPoster(uint id)
        {
            var activity = await db.Activities.FindAsync(id);

            if (activity == null || string.IsNullOrEmpty(activity.PosterPath))
                return NotFound("Activity or poster not found.");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), activity.PosterPath);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File is no longer present on the server.");

            return PhysicalFile(filePath, "application/octet-stream", activity.PosterFileName);
        }
    }
}
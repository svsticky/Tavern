using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Backend.Database;
using Backend.Models;
using Backend.Controllers.DTOs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Backend.Interfaces;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class Activities(PostgresDbContext db, IStorageService storageService, IFileCompressor fileCompressor) : ControllerBase
    {
        // GET: api/activities
        /// <summary>
        /// Lists activities with optional filtering for enrollment and timeframe.
        /// </summary>
        /// <param name="onlyEnrolled">If true, only returns activities the user is signed up for.</param>
        /// <param name="includePast">If false (default), only returns future or ongoing activities.</param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActivityResponseDTO>>> GetActivities([FromQuery] bool includePast = false)
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            
            IQueryable<ActivityResponseDTO> query = db.Activities.Select(
                a => new ActivityResponseDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Price = a.Price,
                    PosterPath = a.PosterPath,
                    PosterFileName = a.PosterFileName,
                    DutchDescription = a.DutchDescription,
                    EnglishDescription = a.EnglishDescription,
                    DateTimeStart = a.DateTimeStart,
                    DateTimeEnd = a.DateTimeEnd,
                    UnenrollmentDeadline = a.UnenrollmentDeadline,
                    EnrollmentDeadline = a.EnrollmentDeadline,
                    Location = a.Location,
                    ParticipantLimit = a.ParticipantLimit,
                    OrganizerId = a.OrganizerId,
                    ShowInKoala = a.ShowInKoala,
                    ShowOnWebsite = a.ShowOnWebsite,
                    IsEnrollable = a.IsEnrollable,
                    AreParticipantsVisible = a.AreParticipantsVisible,
                    IsAdultOnly = a.IsAdultOnly,
                    AllowedAudience = a.AllowedAudience,
                    VatRate = a.VatRate,
                    GLAccountId = a.GLAccountId,
                    CostCenterId = a.CostCenterId,
                    CostUnitId = a.CostUnitId,
                    Enrollments = a.Enrollments.Select(e => new EnrollmentSummaryDTO
                        {
                            IsOnWaitingList = e.IsOnWaitingList,
                            Member = new MemberSummaryDTO
                            { 
                                Id = e.MemberId == userId ? e.MemberId : null,
                                FirstName = a.AreParticipantsVisible || PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db) ? e.Member.FirstName : null, 
                                LastName = a.AreParticipantsVisible || PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db) ? e.Member.LastName : null, 
                                ProfilePicturePath = a.AreParticipantsVisible || PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db) ? e.Member.ProfilePicturePath : null
                            }
                        }).ToList()
                }
            );

            if(includePast && !PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

            if (!includePast)
            {
                DateTime now = DateTime.UtcNow;
                query = query.Where(a => a.DateTimeEnd > now && a.ShowInKoala);
            }

            query = query.OrderBy(a => a.DateTimeStart);

            return await query.ToListAsync();
        }

        // GET: api/activities/5
        /// <summary>
        /// Fetches a single activity.
        /// </summary>
        /// <param name="id">The id of the activity to fetch.</param>
        /// <returns>The activity with the given id, or NotFound if it doesn't exist.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ActivityResponseDTO>> GetActivity(uint id)
        {
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            bool isBoard = PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db);

            ActivityResponseDTO? activity = await db.Activities.Select(
                a => new ActivityResponseDTO
                {
                    Id = a.Id,
                    Name = a.Name,
                    Price = a.Price,
                    PosterPath = a.PosterPath,
                    PosterFileName = a.PosterFileName,
                    DutchDescription = a.DutchDescription,
                    EnglishDescription = a.EnglishDescription,
                    DateTimeStart = a.DateTimeStart,
                    DateTimeEnd = a.DateTimeEnd,
                    UnenrollmentDeadline = a.UnenrollmentDeadline,
                    EnrollmentDeadline = a.EnrollmentDeadline,
                    Location = a.Location,
                    ParticipantLimit = a.ParticipantLimit,
                    OrganizerId = a.OrganizerId,
                    ShowInKoala = a.ShowInKoala,
                    ShowOnWebsite = a.ShowOnWebsite,
                    IsEnrollable = a.IsEnrollable,
                    AreParticipantsVisible = a.AreParticipantsVisible,
                    IsAdultOnly = a.IsAdultOnly,
                    AllowedAudience = a.AllowedAudience,
                    VatRate = a.VatRate,
                    GLAccountId = a.GLAccountId,
                    CostCenterId = a.CostCenterId,
                    CostUnitId = a.CostUnitId,
                    Enrollments = a.Enrollments.Select(e => new EnrollmentSummaryDTO
                        {
                            IsOnWaitingList = e.IsOnWaitingList,
                            Member = new MemberSummaryDTO
                            { 
                                Id = e.MemberId == userId ? e.MemberId : null,
                                FirstName = a.AreParticipantsVisible || isBoard ? e.Member.FirstName : null, 
                                LastName = a.AreParticipantsVisible || isBoard ? e.Member.LastName : null, 
                                ProfilePicturePath = a.AreParticipantsVisible || isBoard ? e.Member.ProfilePicturePath : null
                            }
                        }).ToList()
                }
            ).FirstOrDefaultAsync(a => a.Id == id);

            if(activity == null)
            {
                return NotFound();
            }

            if(activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow && PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

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
            if(activityDto.DateTimeEnd < activityDto.DateTimeStart)
            {
                return BadRequest("Activity cannot end before it starts.");
            }

            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if((activityDto.ShowInKoala || activityDto.ShowOnWebsite) && !PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

            if(activityDto.ParticipantLimit < 0)
            {
                return BadRequest("Participant limit cannot be negative.");
            }

            if(activityDto.Poster != null && !ExtensionUtils.IsValidPosterExtension(activityDto.Poster))
            {
                return BadRequest("Invalid poster file type. Allowed types are: .jpg, .jpeg, .png, .gif, .pdf");
            }

            IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

            try
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
                    EnrollmentDeadline = activityDto.EnrollmentDeadline,
                    Location = activityDto.Location,
                    ParticipantLimit = activityDto.ParticipantLimit,
                    OrganizerId = activityDto.OrganizerId,
                    ShowInKoala = activityDto.ShowInKoala,
                    ShowOnWebsite = activityDto.ShowOnWebsite,
                    IsEnrollable = activityDto.IsEnrollable,
                    AreParticipantsVisible = activityDto.AreParticipantsVisible,
                    IsAdultOnly = activityDto.IsAdultOnly,
                    AllowedAudience = activityDto.AllowedAudience,
                    VatRate = activityDto.VatRate,
                    GLAccountId = activityDto.GLAccountId,
                    CostCenterId = activityDto.CostCenterId,
                    CostUnitId = activityDto.CostUnitId
                };

                newActivity.SpecificationQuestions = activityDto.SpecificationQuestions.Select(q => new SpecificationQuestion 
                { 
                    Activity = newActivity,
                    QuestionDutch = q.QuestionDutch, 
                    QuestionEnglish = q.QuestionEnglish,
                    Type = q.Type,
                    Options = q.Options != null ? string.Join(';', q.Options) : null,
                    IsMandatory = q.IsMandatory,
                    IsPublic = q.IsPublic
                }).ToList();

                if(activityDto.Poster != null)
                {
                    try
                    {
                        var compressedImage = await fileCompressor.CompressFileAsync(activityDto.Poster);
                        newActivity.PosterPath = await storageService.SaveFileAsync(compressedImage.Stream, compressedImage.ContentType, "posters");
                        newActivity.PosterFileName = activityDto.Poster.FileName;
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(ex.Message);
                    }
                }

                EntityEntry<Activity> newEntry = db.Activities.Add(newActivity);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return CreatedAtAction(nameof(GetActivity), new { id = newEntry.Entity.Id }, newEntry.Entity);
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while creating the activity.");
            }
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
            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

            Activity? activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            if(activity.PosterPath != null)
            {
                await storageService.DeleteFileAsync("posters", activity.PosterPath);
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

            if(activity.DateTimeEnd < activity.DateTimeStart)
            {
                return BadRequest("Activity cannot end before it starts.");
            }

            if(activity.ParticipantLimit < 0)
            {
                return BadRequest("Participant limit cannot be negative.");
            }

            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if((activity.ShowInKoala || activity.ShowOnWebsite || patchDoc.Operations.Any(op => op.path.ToLower() == "/showinkoala" || op.path == "/showonwebsite" || op.path == "/isopenforpayment")) && !PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                uint? oldLimit = activity.ParticipantLimit;
                decimal oldPrice = activity.Price;

                patchDoc.ApplyTo(activity, ModelState);

                TryValidateModel(activity);

                if (!ModelState.IsValid) 
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return BadRequest(ModelState);
                }

                if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit))
                {
                    await ProcessWaitingList(id, activity.ParticipantLimit, cancellationToken);
                }

                if (oldPrice != activity.Price)
                {
                    var enrollmentsToUpdate = await db.Enrollments
                        .Where(e => e.ActivityId == id && e.Price == oldPrice)
                        .ToListAsync(cancellationToken);

                    foreach (var enrollment in enrollmentsToUpdate)
                    {
                        enrollment.Price = activity.Price;
                    }
                }

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return NoContent();
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                return StatusCode(500, "An error occurred while updating the activity.");
            }   
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

            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if((activity.ShowInKoala || activity.ShowOnWebsite) && !PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

            if(poster != null && !ExtensionUtils.IsValidPosterExtension(poster))
            {
                return BadRequest("Invalid poster file type. Allowed types are: .jpg, .jpeg, .png, .gif, .pdf");
            }

            string? oldPath = activity.PosterPath;

            using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if(poster != null)
                {
                    var compressedImage = await fileCompressor.CompressFileAsync(poster);
                    string path = await storageService.SaveFileAsync(compressedImage.Stream, compressedImage.ContentType, "posters");
                    activity.PosterPath = path;
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
                    await storageService.DeleteFileAsync("posters", oldPath);
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

            if(activity.DateTimeEnd < activity.DateTimeStart)
            {
                return BadRequest("Activity cannot end before it starts.");
            }
            
            if(activityDto.ParticipantLimit < 0)
            {
                return BadRequest("Participant limit cannot be negative.");
            }

            if(activityDto.Poster != null && !ExtensionUtils.IsValidPosterExtension(activityDto.Poster))
            {
                return BadRequest("Invalid poster file type. Allowed types are: .jpg, .jpeg, .png, .gif, .pdf");
            }

            if(activityDto.DateTimeEnd < activityDto.DateTimeStart)
            {
                return BadRequest("Activity cannot end before it starts.");
            }

            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            if((activity.ShowInKoala || activity.ShowOnWebsite || activityDto.ShowInKoala || activityDto.ShowOnWebsite) && !PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

            using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

            try
            {
                decimal oldPrice = activity.Price;

                activity.Name = activityDto.Name;
                activity.Price = activityDto.Price;
                activity.DutchDescription = activityDto.DutchDescription;
                activity.EnglishDescription = activityDto.EnglishDescription;
                activity.DateTimeStart = activityDto.DateTimeStart;
                activity.DateTimeEnd = activityDto.DateTimeEnd;
                activity.UnenrollmentDeadline = activityDto.UnenrollmentDeadline;
                activity.EnrollmentDeadline = activityDto.EnrollmentDeadline;
                activity.Location = activityDto.Location;
                activity.ParticipantLimit = activityDto.ParticipantLimit;
                activity.OrganizerId = activityDto.OrganizerId;
                activity.SpecificationQuestions = activityDto.SpecificationQuestions.Select(q => new SpecificationQuestion 
                { 
                    Activity = activity,
                    QuestionDutch = q.QuestionDutch, 
                    QuestionEnglish = q.QuestionEnglish,
                    Type = q.Type,
                    Options = q.Options != null ? string.Join(';', q.Options) : null,
                    IsMandatory = q.IsMandatory,
                    IsPublic = q.IsPublic
                }).ToList();
                activity.ShowInKoala = activityDto.ShowInKoala;
                activity.ShowOnWebsite = activityDto.ShowOnWebsite;
                activity.IsEnrollable = activityDto.IsEnrollable;
                activity.AreParticipantsVisible = activityDto.AreParticipantsVisible;
                activity.IsAdultOnly = activityDto.IsAdultOnly;
                activity.AllowedAudience = activityDto.AllowedAudience;
                activity.VatRate = activityDto.VatRate;
                activity.GLAccountId = activityDto.GLAccountId;
                activity.CostCenterId = activityDto.CostCenterId;
                activity.CostUnitId = activityDto.CostUnitId;

                if (oldPrice != activity.Price)
                {
                    var enrollmentsToUpdate = await db.Enrollments
                        .Where(e => e.ActivityId == id && e.Price == oldPrice)
                        .ToListAsync();

                    foreach (var enrollment in enrollmentsToUpdate)
                    {
                        enrollment.Price = activity.Price;
                    }
                }

                await db.SaveChangesAsync();

                string? existingPosterPath = activity.PosterPath;

                if(activityDto.Poster != null)
                {
                    try
                    {
                        var compressedImage = await fileCompressor.CompressFileAsync(activityDto.Poster);
                        activity.PosterPath = await storageService.SaveFileAsync(compressedImage.Stream, compressedImage.ContentType, "posters");
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

                uint? oldLimit = activity.ParticipantLimit;
                activity.ParticipantLimit = activityDto.ParticipantLimit;

                if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit))
                {
                    await ProcessWaitingList(id, activity.ParticipantLimit, default);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                if(existingPosterPath != null)
                {
                    System.IO.File.Delete(existingPosterPath);
                    activity.PosterPath = null;
                    activity.PosterFileName = null;
                }

                return NoContent();
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while updating the activity.");
            }
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

            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            if (activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow && !PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

            var file = await storageService.GetFileAsync("posters", activity.PosterPath);
            if (file == null)
            {
                return NotFound("File is no longer present on the server.");
            }

            return File(file.Stream, file.ContentType);
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

            Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            if (activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow && !PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
            {
                return Forbid();
            }

            var file = await storageService.GetFileAsync("posters", activity.PosterPath);
            if (file == null)
            {
                return NotFound("File is no longer present on the server.");
            }

            return File(file.Stream, file.ContentType, activity.PosterFileName ?? "poster");
        }

        private async Task ProcessWaitingList(uint activityId, uint? newLimit, CancellationToken ct)
        {
            int currentParticipants = await db.Enrollments
                .CountAsync(e => e.ActivityId == activityId && !e.IsOnWaitingList, ct);

            int availableSpots = newLimit.HasValue 
                ? (int)newLimit.Value - currentParticipants 
                : int.MaxValue;

            if (availableSpots > 0)
            {
                var waitingListToPromote = await db.Enrollments
                    .Where(e => e.ActivityId == activityId && e.IsOnWaitingList)
                    .OrderBy(e => e.RegisteredOn)
                    .Take(availableSpots)
                    .ToListAsync(ct);

                foreach (var enrollment in waitingListToPromote)
                {
                    enrollment.IsOnWaitingList = false;
                }
            }
        }
    }
}
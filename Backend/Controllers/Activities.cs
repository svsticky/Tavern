using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Database;
using Backend.Models;
using Backend.Controllers.DTOs;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Activities(PostgresDbContext db) : ControllerBase
    {
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
        public async Task<ActionResult<Activity>> PostActivity(PostActivityDTO activity)
        {
            Activity newActivity = new()
            {
                Name = activity.Name, Description = activity.Description, DateTimeStart = activity.DateTimeStart, DateTimeEnd = activity.DateTimeEnd
            };

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

            db.Activities.Remove(activity);
            await db.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/activities/5/name
        /// <summary>
        /// Updates an activities name.
        /// </summary>
        /// <param name="id">The id of the activity to update.</param>
        /// <param name="newName">The new name of the activity.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/name")]
        public async Task<IActionResult> PatchActivityName(uint id, string newName)
        {
            Activity? activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            activity.Name = newName;

            await db.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/activities/5/description
        /// <summary>
        /// Updates an activities description.
        /// </summary>
        /// <param name="id">The id of the activity to update.</param>
        /// <param name="newDescription">The new description of the activity.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/description")]
        public async Task<IActionResult> PatchActivityDescription(uint id, string newDescription)
        {
            Activity? activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            activity.Description = newDescription;

            await db.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/activities/5/datetimestart
        /// <summary>
        /// Updates an activities start date and time.
        /// </summary>
        /// <param name="id">The id of the activity to update.</param>
        /// <param name="newDateTimeStart">The new start date and time of the activity.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/datetimestart")]
        public async Task<IActionResult> PatchActivityDateTimeStart(uint id, DateTime newDateTimeStart)
        {
            Activity? activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            activity.DateTimeStart = newDateTimeStart;

            await db.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/activities/5/datetimeend
        /// <summary>
        /// Updates an activities end date and time.
        /// </summary>
        /// <param name="id">The id of the activity to update.</param>
        /// <param name="newDateTimeEnd">The new end date and time of the activity.</param>
        /// <returns>No content.</returns>
        [HttpPatch("{id}/datetimeend")]
        public async Task<IActionResult> PatchActivityDateTimeEnd(uint id, DateTime newDateTimeEnd)
        {
            Activity? activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            activity.DateTimeEnd = newDateTimeEnd;

            await db.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/activities/5
        /// <summary>
        /// Updates an activity.
        /// </summary>
        /// <param name="id">The id of the activity to update.</param>
        /// <param name="activityDto">The new details of the activity.</param>
        /// <returns>No content.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutActivity(uint id, ActivityUpdateDTO activityDto)
        {
            Activity? activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            activity.Name = activityDto.Name;
            activity.Description = activityDto.Description;
            activity.DateTimeStart = activityDto.DateTimeStart;
            activity.DateTimeEnd = activityDto.DateTimeEnd;

            await db.SaveChangesAsync();

            return NoContent();
        }
    }
}

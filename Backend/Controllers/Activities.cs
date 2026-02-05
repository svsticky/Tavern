using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
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

            // Pas de patch toe op de entiteit
            patchDoc.ApplyTo(activity, ModelState);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await db.SaveChangesAsync(cancellationToken);

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

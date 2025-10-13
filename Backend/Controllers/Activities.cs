using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Database;
using Backend.Models;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Activities(PostgresDbContext db) : ControllerBase
    {
        // GET: api/activities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Activity>>> GetActivities()
        {
            return await db.Activities.ToListAsync();
        }

        // GET: api/activities/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Activity>> GetActivity(uint id)
        {
            Activity? activity = await db.Activities.FindAsync(id);

            return activity != null ? activity : NotFound();
        }

        // POST: api/activities
        [HttpPost]
        public async Task<ActionResult<Activity>> PostActivity(Activity activity) // TODO replace with DTO
        {
            Activity? currentActivity = await db.Activities.FindAsync(activity.Id);
            if (currentActivity != null) return BadRequest("Activity already exists with this Id.");

            db.Activities.Add(activity);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetActivity), new { id = activity.Id }, activity);
        }

        // DELETE: api/activities/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActivity(uint id)
        {
            Activity? activity = await db.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            db.Activities.Remove(activity);
            await db.SaveChangesAsync();

            return NoContent();
        }
    }
}

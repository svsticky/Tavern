using Microsoft.AspNetCore.Mvc;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Studies(PostgresDbContext db) : ControllerBase
    {
        // GET: api/studies
        /// <summary>
        /// Lists all studies in the database.
        /// </summary>
        /// <returns>Said list.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Study>>> GetStudies(CancellationToken cancellationToken)
        {
            return await db.Studies.ToListAsync(cancellationToken);
        }

        // GET: api/studies/5
        /// <summary>
        /// Fetches a single study.
        /// </summary>
        /// <param name="id">The id of the study to fetch.</param>
        /// <returns>The full study.</returns> // TODO: perhaps replace this with a DTO to prevent exposing unneeded fields?
        [HttpGet("{id}")]
        public async Task<ActionResult<Study>> GetStudy(uint id, CancellationToken cancellationToken)
        {
            Study? study = await db.Studies.FindAsync(id, cancellationToken);

            return study != null ? study : NotFound();
        }

        // POST: api/studies
        /// <summary>
        /// Creates a new study with a unique ID assigned by the database.
        /// </summary>
        /// <param name="study">The study to be added to the database.</param>
        /// <returns>Fully created study in body and api route of where to fetch it in the headers.</returns>
        [HttpPost]
        public async Task<ActionResult<Study>> PostStudy(PostStudyDTO study, CancellationToken cancellationToken)
        {
            var newEntry = db.Studies.Add(new Study
            {
                Title = study.Title,
                DurationYears = study.DurationYears,
                Type = study.Type
            });
            await db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetStudy), new { id = newEntry.Entity.Id }, newEntry.Entity);
        }

        // DELETE: api/studies/5
        /// <summary>
        /// Deletes a study.
        /// </summary>
        /// <param name="id">The id of the study to delete.</param>
        /// <returns>Nothing, really.</returns>
        /// <remarks>
        /// Deleting a study will also delete all enrollments and study enrollments associated with said
        /// study.
        /// </remarks>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudy(uint id, CancellationToken cancellationToken)
        {
            Study? study = await db.Studies.FindAsync(id, cancellationToken);
            if (study == null) return NotFound();

            db.Studies.Remove(study);
            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PUT: api/studies/5
        /// <summary>
        /// Updates a study's details.
        /// </summary>
        /// <param name="id">The id of the study to update.</param>
        /// <param name="studyDto">The new details of the study.</param>
        /// <returns>The updated study.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudy(uint id, StudyUpdateDTO studyDto, CancellationToken cancellationToken)
        {
            Study? study = await db.Studies.FindAsync(id, cancellationToken);
            if (study == null) return NotFound();

            study.Title = studyDto.Title;
            study.DurationYears = studyDto.DurationYears;
            study.Type = studyDto.Type;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}

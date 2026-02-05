using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Roles(PostgresDbContext db) : ControllerBase
    {
        // GET: api/roles
        /// <summary>
        /// Lists all roles in the database.
        /// </summary>
        /// <returns>Said list.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles(CancellationToken cancellationToken)
        {
            return await db.Roles.ToListAsync(cancellationToken);
        }

        // GET: api/roles/5
        /// <summary>
        /// Fetches a single role.
        /// </summary>
        /// <param name="id">The id of the role to fetch.</param>
        /// <returns>The full role.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Role>> GetRole(uint id, CancellationToken cancellationToken)
        {
            Role? role = await db.Roles.FindAsync(id, cancellationToken);

            return role != null ? role : NotFound();
        }

        // POST: api/roles
        /// <summary>
        /// Creates a new role with a unique ID assigned by the database.
        /// </summary>
        /// <param name="roleDto">The role to be added to the database.</param>
        /// <returns>Fully created role in body and api route of where to fetch it in the headers.</returns>
        [HttpPost]
        public async Task<ActionResult<Role>> PostRole(PostRoleDTO roleDto, CancellationToken cancellationToken)
        {
            var newEntry = db.Roles.Add(new Role
            {
                Name = roleDto.Name
            });
            await db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetRole), new { id = newEntry.Entity.Id }, newEntry.Entity);
        }

        // DELETE: api/roles/5
        /// <summary>
        /// Deletes a role.
        /// </summary>
        /// <param name="id">The id of the role to delete.</param>
        /// <returns>Nothing, really.</returns>
        /// <remarks>
        /// Deleting a role will also delete all enrollments and role enrollments associated with said
        /// role.
        /// </remarks>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(uint id, CancellationToken cancellationToken)
        {
            Role? role = await db.Roles.FindAsync(id, cancellationToken);
            if (role == null) return NotFound();

            db.Roles.Remove(role);
            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/roles/5
        /// <summary>
        /// Partially updates a role's details.
        /// </summary>
        /// <param name="id">The id of the role to update.</param>
        /// <param name="patchDoc">The patch document containing the changes.</param>
        /// <returns>No Content.</returns>
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchRole(uint id, [FromBody] JsonPatchDocument<Role> patchDoc, CancellationToken cancellationToken)
        {
            if (patchDoc == null)
                return BadRequest();

            Role? role = await db.Roles.FindAsync(new object[] { id }, cancellationToken);
            if (role == null)
                return NotFound();

            patchDoc.ApplyTo(role, ModelState);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PUT: api/roles/5
        /// <summary>
        /// Updates a role's details.
        /// </summary>
        /// <param name="id">The id of the role to update.</param>
        /// <param name="roleDto">The new details of the role.</param>
        /// <returns>No Content.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRole(uint id, RoleUpdateDTO roleDto, CancellationToken cancellationToken)
        {
            Role? role = await db.Roles.FindAsync(id, cancellationToken);
            if (role == null) return NotFound();

            role.Name = roleDto.Name;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}

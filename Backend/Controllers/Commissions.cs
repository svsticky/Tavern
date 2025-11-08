using Microsoft.AspNetCore.Mvc;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Commissions(PostgresDbContext db) : ControllerBase
    {
        // GET: api/commissions
        /// <summary>
        /// Lists all commissions in the database.
        /// </summary>
        /// <returns>Said list.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Commission>>> GetCommissions(CancellationToken cancellationToken)
        {
            var commissions = await db.Commissions
                .Include(c => c.CommissionMemberships)
                .ThenInclude(cm => cm.Member)
                .ToListAsync(cancellationToken);

            var result = commissions.Select(c => new CommissionResponseDTO
            {
                Id = c.Id,
                Name = c.Name,
                CommissionMemberships = c.CommissionMemberships.Select(cm => new CommissionMembershipResponseDTO
                {
                    Id = cm.Id,
                    CommissionId = cm.CommissionId,
                    CommissionName = cm.Commission.Name,
                    MemberId = cm.MemberId,
                    MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                    MembershipYear = cm.MembershipYear
                }).ToList()
            });

            return Ok(result);
        }

        // GET: api/commissions/5
        /// <summary>
        /// Fetches a single commission.
        /// </summary>
        /// <param name="id">The id of the commission to fetch.</param>
        /// <returns>The full commission.</returns> // TODO: perhaps replace this with a DTO to prevent exposing unneeded fields?
        [HttpGet("{id}")]
        public async Task<ActionResult<Commission>> GetCommission(uint id, CancellationToken cancellationToken)
        {
            var commission = await db.Commissions
                .Include(c => c.CommissionMemberships)
                .ThenInclude(cm => cm.Member)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (commission is null) return NotFound();

            var result = new CommissionResponseDTO
            {
                Id = commission.Id,
                Name = commission.Name,
                CommissionMemberships = commission.CommissionMemberships.Select(cm => new CommissionMembershipResponseDTO
                {
                    Id = cm.Id,
                    CommissionId = cm.CommissionId,
                    CommissionName = cm.Commission.Name,
                    MemberId = cm.MemberId,
                    MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                    MembershipYear = cm.MembershipYear
                }).ToList()
            };

            return Ok(result);
        }

        // POST: api/commissions
        /// <summary>
        /// Creates a new commission with a unique ID assigned by the database.
        /// </summary>
        /// <param name="commission">The commission to be added to the database.</param>
        /// <returns>Fully created commission in body and api route of where to fetch it in the headers.</returns>
        [HttpPost]
        public async Task<ActionResult<Commission>> PostCommission(string name, CancellationToken cancellationToken)
        {
            var newEntry = db.Commissions.Add(new Commission
            {
                Name = name
            });
            await db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetCommission), new { id = newEntry.Entity.Id }, newEntry.Entity);
        }

        // DELETE: api/commissions/5
        /// <summary>
        /// Deletes a commission.
        /// </summary>
        /// <param name="id">The id of the commission to delete.</param>
        /// <returns>Nothing, really.</returns>
        /// <remarks>
        /// Deleting a commission will also delete all enrollments and commission enrollments associated with said
        /// commission.
        /// </remarks>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommission(uint id, CancellationToken cancellationToken)
        {
            Commission? commission = await db.Commissions.FindAsync(id, cancellationToken);
            if (commission == null) return NotFound();

            db.Commissions.Remove(commission);
            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PUT: api/commissions/5
        /// <summary>
        /// Updates a commission's details.
        /// </summary>
        /// <param name="id">The id of the commission to update.</param>
        /// <param name="commissionDto">The new details of the commission.</param>
        /// <returns>The updated commission.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCommission(uint id, CommissionUpdateDTO commissionDto, CancellationToken cancellationToken)
        {
            Commission? commission = await db.Commissions.FindAsync(id, cancellationToken);
            if (commission == null) return NotFound();

            commission.Name = commissionDto.Name;

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}

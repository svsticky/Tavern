using Microsoft.AspNetCore.Mvc;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CommissionMemberships(PostgresDbContext db) : ControllerBase
{
    // GET: api/commissionMemberships
    /// <summary>
    /// Lists all commission memberships in the database.
    /// </summary>
    /// <returns>Said list.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommissionMembership>>> GetCommissionMemberships(CancellationToken cancellationToken)
    {
        var memberships = await db.CommissionMemberships
            .Include(cm => cm.Member)
            .Include(cm => cm.Commission)
            .ToListAsync(cancellationToken);

        var result = memberships.Select(cm => new CommissionMembershipResponseDTO
        {
            Id = cm.Id,
            MemberId = cm.MemberId,
            MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
            CommissionId = cm.CommissionId,
            CommissionName = cm.Commission.Name,
            MembershipYear = cm.MembershipYear
        });

        return Ok(result);
    }

    // GET: api/commissionMemberships/5
    /// <summary>
    /// Fetches a single commission membership.
    /// </summary>
    /// <param name="id">The id of the commission membership to fetch.</param>
    /// <returns>The full commission membership.</returns> 
    [HttpGet("{id}")]
    public async Task<ActionResult<CommissionMembership>> GetCommissionMembership(uint id, CancellationToken cancellationToken)
    {
        var cm = await db.CommissionMemberships
            .Include(e => e.Member)
            .Include(e => e.Commission)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (cm is null) return NotFound();

        var result = new CommissionMembershipResponseDTO
        {
            Id = cm.Id,
            MemberId = cm.MemberId,
            MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
            CommissionId = cm.CommissionId,
            CommissionName = cm.Commission.Name,
            MembershipYear = cm.MembershipYear
        };

        return Ok(result);
    }

    // POST: api/commissionMemberships
    /// <summary>
    /// Creates a new commission membership with a unique ID assigned by the database.
    /// </summary>
    /// <param name="membershipDto">The commission membership to be added to the database.</param>
    /// <returns>Fully created commission membership in body and api route of where to fetch it in the headers.</returns>
    [HttpPost]
    public async Task<ActionResult<CommissionMembership>> PostCommissionMembership(PostCommissionMembershipDTO membershipDto, CancellationToken cancellationToken)
    {
        Member? member = await db.Members.FindAsync(membershipDto.MemberId, cancellationToken);
        if (member is null)
            return BadRequest($"Member with ID {membershipDto.MemberId} does not exist.");

        Commission? commission = await db.Commissions.FindAsync(membershipDto.CommissionId, cancellationToken);
        if (commission is null)
            return BadRequest($"Commission with ID {membershipDto.CommissionId} does not exist.");

        var newMembership = new CommissionMembership
        {
            Member = member,
            Commission = commission,
            MembershipYear = membershipDto.MembershipYear
        };
        
        var newEntry = db.CommissionMemberships.Add(newMembership);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(
            nameof(GetCommissionMembership),
            new { id = newEntry.Entity.Id },
            new CommissionMembershipResponseDTO
            {
                Id = newEntry.Entity.Id,
                MemberId = newEntry.Entity.MemberId,
                CommissionId = newEntry.Entity.CommissionId,
                MembershipYear = newEntry.Entity.MembershipYear,
            }
        );
    }

    // DELETE: api/commissionmemberships/5
    /// <summary>
    /// Deletes a commission membership.
    /// </summary>
    /// <param name="id">The id of the commission membership to delete.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCommissionMembership(uint id, CancellationToken cancellationToken)
    {
        var membership = await db.CommissionMemberships.FindAsync(id, cancellationToken);
        if (membership is null)
            return NotFound();

        db.CommissionMemberships.Remove(membership);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

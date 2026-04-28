using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class MailinglistService : IMailinglistService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<MailinglistService> _logger;

    public MailinglistService(PostgresDbContext db, IPermissionService permissionService, ILogger<MailinglistService> logger)
    {
        _db = db;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<IEnumerable<Mailinglist>> GetMailinglists(CancellationToken ct)
    {
        return await _db.Mailinglists.ToListAsync(ct);
    }

    public async Task<Mailinglist?> GetMailinglist(int id, CancellationToken ct)
    {
        return await _db.Mailinglists.FindAsync(new [] { id }, ct);
    }

    public async Task<Mailinglist> CreateMailinglist(PostMailinglistDTO dto, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Creating mailinglist {Name} by user {UserId}.", dto.Name, userId);

        var maxBit = await _db.Mailinglists.MaxAsync(m => (uint?)m.BitValue, ct) ?? 0;

        uint nextBit = (maxBit == 0) ? 1 : maxBit << 1;

        var mailinglist = new Mailinglist
        {
            BitValue = nextBit,
            Name = dto.Name,
            ServiceId = dto.ServiceId
        };

        _db.Mailinglists.Add(mailinglist);
        await _db.SaveChangesAsync(ct);

        return mailinglist;
    }

    public async Task UpdateMailinglist(int id, PostMailinglistDTO dto, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        var existing = await GetMailinglistOrThrow(id, ct);

        _logger.LogInformation("Updating mailinglist {Id} by user {UserId}.", id, userId);

        existing.Name = dto.Name;
        existing.ServiceId = dto.ServiceId;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteMailinglist(int id, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Deleting mailinglist {Id} by user {UserId}.", id, userId);
    
        var mailinglist = await GetMailinglistOrThrow(id, ct);
        var bitToDelete = mailinglist.BitValue;

        await _db.Members
            .Where(m => (m.MailSubscriptions & bitToDelete) != 0)
            .ExecuteUpdateAsync(s => s.SetProperty(
                m => m.MailSubscriptions, 
                m => m.MailSubscriptions & ~bitToDelete), 
                ct);

        _db.Mailinglists.Remove(mailinglist);
        
        await _db.SaveChangesAsync(ct);
    }

    public async Task PatchMailinglist(int id, JsonPatchDocument<Mailinglist> patchDoc, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Patching mailinglist {Id} by user {UserId}.", id, userId);

        var mailinglist = await GetMailinglistOrThrow(id, ct);

        if (patchDoc == null) throw new ArgumentException("Patch document is null");
        
        if (patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase)
            || op.path.Equals("/BitValue", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Cannot modify Id or BitValue field.");

        patchDoc.ApplyTo(mailinglist);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Mailinglist> GetMailinglistOrThrow(int id, CancellationToken ct)
    {
        var mailinglist = await _db.Mailinglists.FindAsync(new object[] { id }, ct);
        if (mailinglist == null)
            throw new KeyNotFoundException($"Mailinglist with id '{id}' not found.");
        
        return mailinglist;
    }
}
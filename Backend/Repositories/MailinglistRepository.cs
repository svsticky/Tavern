using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

/// <summary>
/// Represents the service responsible for managing mailing lists within the application. The MailinglistRepository class implements the IMailinglistRepository interface, providing concrete implementations for operations such as retrieving all mailing lists, fetching a specific mailing list by ID, creating a new mailing list, updating an existing mailing list, deleting a mailing list, and partially updating a mailing list using a JSON Patch document. The service interacts with the database context to perform CRUD operations on the Mailinglist entities and includes authorization checks to ensure that only authorized users can perform certain actions on the mailing lists. Additionally, it incorporates logging to track significant events and actions related to mailing list management for monitoring and debugging purposes.
/// </summary>
public class MailinglistRepository : IMailinglistRepository
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<MailinglistRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the MailinglistRepository class with the specified database context, permission service, and logger. The constructor sets up the necessary dependencies for the service to function correctly, allowing it to interact with the database for managing mailing lists, perform authorization checks using the permission service, and log important events and actions related to mailing list management. This setup is essential for ensuring that the service can effectively handle mailing list operations while maintaining security and providing insights through logging.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="logger">The logger.</param>
    public MailinglistRepository(PostgresDbContext db, IPermissionService permissionService, ILogger<MailinglistRepository> logger)
    {
        _db = db;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Mailinglist>> GetMailinglists(CancellationToken ct)
    {
        return await _db.Mailinglists.ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Mailinglist?> GetMailinglist(int id, CancellationToken ct)
    {
        return await _db.Mailinglists.FindAsync(new object[] { id }, ct);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task UpdateMailinglist(int id, PostMailinglistDTO dto, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        var existing = await GetMailinglistOrThrow(id, ct);

        _logger.LogInformation("Updating mailinglist {Id} by user {UserId}.", id, userId);

        existing.Name = dto.Name;
        existing.ServiceId = dto.ServiceId;

        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
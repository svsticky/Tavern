using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Domain;

/// <summary>
/// Implements <see cref="IMailinglistCurationService"/>. Curation records only ever store a provider
/// list ID and a visibility - display names and existence are always resolved live against
/// <see cref="IMailSubscriptionService"/>, never cached locally.
/// </summary>
public class MailinglistCurationService : IMailinglistCurationService
{
    private readonly PostgresDbContext _db;
    private readonly IMailSubscriptionService _mailSubscriptionService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<MailinglistCurationService> _logger;

    /// <summary>
    /// Initializes a new instance of the MailinglistCurationService class.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="mailSubscriptionService">The mail subscription provider service.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="logger">The logger.</param>
    public MailinglistCurationService(
        PostgresDbContext db,
        IMailSubscriptionService mailSubscriptionService,
        IPermissionService permissionService,
        ILogger<MailinglistCurationService> logger)
    {
        _db = db;
        _mailSubscriptionService = mailSubscriptionService;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CuratedMailinglistDto>> GetCuratedMailinglists(Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var curated = await _db.CuratedMailinglists.ToListAsync(ct);
        var providerLists = (await _mailSubscriptionService.GetAvailableMailinglistsAsync(ct))
            .ToDictionary(l => l.Id, l => l.Name);

        return curated.Select(c => providerLists.TryGetValue(c.ProviderListId, out var name)
            ? new CuratedMailinglistDto(c.Id, c.ProviderListId, name, c.Visibility, Orphaned: false)
            : new CuratedMailinglistDto(c.Id, c.ProviderListId, null, c.Visibility, Orphaned: true));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MailinglistDto>> GetAddableProviderMailinglists(Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var curatedIds = await _db.CuratedMailinglists.Select(c => c.ProviderListId).ToListAsync(ct);
        var curatedIdSet = curatedIds.ToHashSet();

        var providerLists = await _mailSubscriptionService.GetAvailableMailinglistsAsync(ct);

        return providerLists.Where(l => !curatedIdSet.Contains(l.Id));
    }

    /// <inheritdoc />
    public async Task<CuratedMailinglistDto> AddMailinglist(string providerListId, MailinglistVisibility visibility, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        if (await _db.CuratedMailinglists.AnyAsync(c => c.ProviderListId == providerListId, ct))
            throw new ArgumentException($"Mailing list '{providerListId}' is already curated.");

        var providerLists = await _mailSubscriptionService.GetAvailableMailinglistsAsync(ct);
        var providerList = providerLists.FirstOrDefault(l => l.Id == providerListId)
            ?? throw new ArgumentException($"Mailing list '{providerListId}' does not exist at the mail subscription provider.");

        _logger.LogInformation("Curating mailing list {ProviderListId} with visibility {Visibility} by user {UserId}.", providerListId, visibility, userId);

        var curated = new CuratedMailinglist
        {
            ProviderListId = providerListId,
            Visibility = visibility
        };

        _db.CuratedMailinglists.Add(curated);
        await _db.SaveChangesAsync(ct);

        return new CuratedMailinglistDto(curated.Id, curated.ProviderListId, providerList.Name, curated.Visibility, Orphaned: false);
    }

    /// <inheritdoc />
    public async Task UpdateMailinglistVisibility(int id, MailinglistVisibility visibility, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Updating visibility of curated mailing list {Id} to {Visibility} by user {UserId}.", id, visibility, userId);

        var curated = await GetCuratedMailinglistOrThrow(id, ct);
        curated.Visibility = visibility;

        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeleteMailinglist(int id, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Un-curating mailing list {Id} by user {UserId}.", id, userId);

        var curated = await GetCuratedMailinglistOrThrow(id, ct);

        _db.CuratedMailinglists.Remove(curated);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> GetVisibleProviderListIds(bool includeYearlyRenewalOnly, CancellationToken ct)
    {
        var query = _db.CuratedMailinglists.AsQueryable();

        if (!includeYearlyRenewalOnly)
            query = query.Where(c => c.Visibility == MailinglistVisibility.General);

        return (await query.Select(c => c.ProviderListId).ToListAsync(ct)).ToHashSet();
    }

    private async Task<CuratedMailinglist> GetCuratedMailinglistOrThrow(int id, CancellationToken ct)
    {
        var curated = await _db.CuratedMailinglists.FindAsync(new object[] { id }, ct);
        if (curated == null)
            throw new KeyNotFoundException($"Curated mailing list with id '{id}' not found.");

        return curated;
    }
}

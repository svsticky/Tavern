using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.IO;

namespace Backend.Repositories;

/// <summary>
/// Implements external link management and icon operations.
/// </summary>
public class ExternalLinkRepository(
    PostgresDbContext db,
    IPermissionService permissionService,
    IFileCompressService fileCompressor,
    IStorageService storageService,
    IMemoryCache memoryCache,
    ILogger<ExternalLinkRepository> logger) : IExternalLinkRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<ExternalLinkResponseDTO>> GetExternalLinks(CancellationToken ct)
    {
        return await db.ExternalLinks
            .AsNoTracking()
            .OrderBy(l => l.SortOrder)
            .Select(l => ToDto(l))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<ExternalLinkResponseDTO?> GetExternalLink(int id, CancellationToken ct)
    {
        var link = await db.ExternalLinks.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        return link == null ? null : ToDto(link);
    }

    /// <inheritdoc />
    public async Task<ExternalLinkResponseDTO> CreateExternalLink(PostExternalLinkDTO dto, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Creating external link by user {UserId}.", userId);

        var link = new ExternalLink
        {
            TitleDutch = dto.TitleDutch,
            TitleEnglish = dto.TitleEnglish,
            DescriptionDutch = dto.DescriptionDutch,
            DescriptionEnglish = dto.DescriptionEnglish,
            Url = dto.Url,
            SortOrder = dto.SortOrder ?? await GetNextSortOrder(ct)
        };

        StateValidator.Validate(link);

        db.ExternalLinks.Add(link);
        await db.SaveChangesAsync(ct);

        return ToDto(link);
    }

    /// <inheritdoc />
    public async Task UpdateExternalLink(int id, ExternalLinkUpdateDTO dto, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Updating external link {LinkId} by user {UserId}.", id, userId);

        var link = await GetLinkOrThrow(id, ct);

        link.TitleDutch = dto.TitleDutch;
        link.TitleEnglish = dto.TitleEnglish;
        link.DescriptionDutch = dto.DescriptionDutch;
        link.DescriptionEnglish = dto.DescriptionEnglish;
        link.Url = dto.Url;
        link.SortOrder = dto.SortOrder;

        StateValidator.Validate(link);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeleteExternalLink(int id, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Deleting external link {LinkId} by user {UserId}.", id, userId);

        var link = await GetLinkOrThrow(id, ct);

        if (!string.IsNullOrEmpty(link.IconPath))
        {
            await storageService.DeleteFileAsync("external-link-icons", link.IconPath);
            memoryCache.Remove($"ext-link-icon-{link.IconPath}");
        }

        db.ExternalLinks.Remove(link);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<string?> UploadExternalLinkIcon(int id, Guid userId, IFormFile? icon)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Uploading external link icon for link {LinkId} by user {UserId}.", id, userId);

        var link = await GetLinkOrThrow(id, default);

        if (icon != null)
        {
            ExtensionValidator.ValidatePosterExtension(icon);
        }

        string? oldPath = link.IconPath;

        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            if (icon != null)
            {
                await SaveIcon(link, icon);
            }
            else
            {
                link.IconPath = null;
                link.IconFileName = null;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            if (!string.IsNullOrEmpty(oldPath))
            {
                await storageService.DeleteFileAsync("external-link-icons", oldPath);
                memoryCache.Remove($"ext-link-icon-{oldPath}");
            }

            return link.IconPath;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed uploading external link icon for link {LinkId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FileResultDto?> GetExternalLinkIconFile(string path)
    {
        string cacheKey = $"ext-link-icon-{path}";
        if (memoryCache.TryGetValue(cacheKey, out (byte[] bytes, string contentType) cached))
        {
            return new FileResultDto
            {
                Stream = new MemoryStream(cached.bytes),
                ContentType = cached.contentType
            };
        }

        var file = await storageService.GetFileAsync("external-link-icons", path);
        if (file == null) return null;

        using var memoryStream = new MemoryStream();
        await file.Stream.CopyToAsync(memoryStream);
        byte[] bytes = memoryStream.ToArray();

        memoryCache.Set(cacheKey, (bytes, file.ContentType), TimeSpan.FromHours(1));

        return new FileResultDto
        {
            Stream = new MemoryStream(bytes),
            ContentType = file.ContentType
        };
    }

    private async Task<ExternalLink> GetLinkOrThrow(int id, CancellationToken ct)
    {
        var link = await db.ExternalLinks.FindAsync(new object[] { id }, ct);
        return link ?? throw new KeyNotFoundException("External link not found.");
    }

    private async Task<int> GetNextSortOrder(CancellationToken ct)
    {
        var maxOrder = await db.ExternalLinks.MaxAsync(l => (int?)l.SortOrder, ct) ?? 0;
        return maxOrder + 1;
    }

    private async Task SaveIcon(ExternalLink link, IFormFile icon)
    {
        var compressedImage = await fileCompressor.CompressFileAsync(icon);
        string path = await storageService.SaveFileAsync(
            compressedImage.Stream,
            compressedImage.ContentType,
            "external-link-icons"
        );

        link.IconPath = path;
        link.IconFileName = icon.FileName;
    }

    private static ExternalLinkResponseDTO ToDto(ExternalLink link)
    {
        return new ExternalLinkResponseDTO
        {
            Id = link.Id,
            TitleDutch = link.TitleDutch,
            TitleEnglish = link.TitleEnglish,
            DescriptionDutch = link.DescriptionDutch,
            DescriptionEnglish = link.DescriptionEnglish,
            Url = link.Url,
            SortOrder = link.SortOrder,
            IconPath = link.IconPath
        };
    }
}

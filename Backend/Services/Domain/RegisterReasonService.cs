using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.Domain;

/// <summary>
/// Implements register reason management and icon operations.
/// </summary>
public class RegisterReasonService(
    PostgresDbContext db,
    IPermissionService permissionService,
    IFileCompressService fileCompressor,
    IStorageService storageService,
    IMemoryCache memoryCache,
    ILogger<RegisterReasonService> logger) : IRegisterReasonService
{
    /// <inheritdoc />
    public async Task<IEnumerable<RegisterReasonResponseDTO>> GetRegisterReasons(CancellationToken ct)
    {
        return await db.RegisterReasons
            .AsNoTracking()
            .OrderBy(r => r.SortOrder)
            .Select(r => ToDto(r))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<RegisterReasonResponseDTO?> GetRegisterReason(int id, CancellationToken ct)
    {
        var reason = await db.RegisterReasons.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        return reason == null ? null : ToDto(reason);
    }

    /// <inheritdoc />
    public async Task<RegisterReasonResponseDTO> CreateRegisterReason(PostRegisterReasonDTO dto, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Creating register reason by user {UserId}.", userId);

        var reason = new RegisterReason
        {
            TitleDutch = dto.TitleDutch,
            TitleEnglish = dto.TitleEnglish,
            DescriptionDutch = dto.DescriptionDutch,
            DescriptionEnglish = dto.DescriptionEnglish,
            SortOrder = dto.SortOrder ?? await GetNextSortOrder(ct)
        };

        StateValidator.Validate(reason);

        db.RegisterReasons.Add(reason);
        await db.SaveChangesAsync(ct);

        return ToDto(reason);
    }

    /// <inheritdoc />
    public async Task UpdateRegisterReason(int id, RegisterReasonUpdateDTO dto, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Updating register reason {ReasonId} by user {UserId}.", id, userId);

        var reason = await GetReasonOrThrow(id, ct);

        reason.TitleDutch = dto.TitleDutch;
        reason.TitleEnglish = dto.TitleEnglish;
        reason.DescriptionDutch = dto.DescriptionDutch;
        reason.DescriptionEnglish = dto.DescriptionEnglish;
        reason.SortOrder = dto.SortOrder;

        StateValidator.Validate(reason);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeleteRegisterReason(int id, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Deleting register reason {ReasonId} by user {UserId}.", id, userId);

        var reason = await GetReasonOrThrow(id, ct);

        if (!string.IsNullOrEmpty(reason.IconPath))
        {
            await storageService.DeleteFileAsync("register-reason-icons", reason.IconPath);
            memoryCache.Remove($"reason-icon-{reason.IconPath}");
        }

        db.RegisterReasons.Remove(reason);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<string?> UploadRegisterReasonIcon(int id, Guid userId, IFormFile? icon)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Uploading register reason icon for reason {ReasonId} by user {UserId}.", id, userId);

        var reason = await GetReasonOrThrow(id, default);

        if (icon != null)
        {
            ExtensionValidator.ValidatePosterExtension(icon);
        }

        string? oldPath = reason.IconPath;

        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            if (icon != null)
            {
                await SaveIcon(reason, icon);
            }
            else
            {
                reason.IconPath = null;
                reason.IconFileName = null;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            if (!string.IsNullOrEmpty(oldPath))
            {
                await storageService.DeleteFileAsync("register-reason-icons", oldPath);
                memoryCache.Remove($"reason-icon-{oldPath}");
            }

            return reason.IconPath;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed uploading register reason icon for reason {ReasonId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FileResultDto?> GetRegisterReasonIconFile(string path)
    {
        string cacheKey = $"reason-icon-{path}";
        if (memoryCache.TryGetValue(cacheKey, out (byte[] bytes, string contentType) cached))
        {
            return new FileResultDto
            {
                Stream = new MemoryStream(cached.bytes),
                ContentType = cached.contentType
            };
        }

        var file = await storageService.GetFileAsync("register-reason-icons", path);
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

    private async Task<RegisterReason> GetReasonOrThrow(int id, CancellationToken ct)
    {
        var reason = await db.RegisterReasons.FindAsync(new object[] { id }, ct);
        return reason ?? throw new KeyNotFoundException("Register reason not found.");
    }

    private async Task<int> GetNextSortOrder(CancellationToken ct)
    {
        var maxOrder = await db.RegisterReasons.MaxAsync(r => (int?)r.SortOrder, ct) ?? 0;
        return maxOrder + 1;
    }

    private async Task SaveIcon(RegisterReason reason, IFormFile icon)
    {
        var compressedImage = await fileCompressor.CompressFileAsync(icon);
        string path = await storageService.SaveFileAsync(
            compressedImage.Stream,
            compressedImage.ContentType,
            "register-reason-icons"
        );

        reason.IconPath = path;
        reason.IconFileName = icon.FileName;
    }

    private static RegisterReasonResponseDTO ToDto(RegisterReason reason)
    {
        return new RegisterReasonResponseDTO
        {
            Id = reason.Id,
            TitleDutch = reason.TitleDutch,
            TitleEnglish = reason.TitleEnglish,
            DescriptionDutch = reason.DescriptionDutch,
            DescriptionEnglish = reason.DescriptionEnglish,
            SortOrder = reason.SortOrder,
            IconPath = reason.IconPath
        };
    }
}

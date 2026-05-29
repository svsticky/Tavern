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
/// Implements register slide management and image operations.
/// </summary>
public class RegisterSlideRepository(
    PostgresDbContext db,
    IPermissionService permissionService,
    IFileCompressService fileCompressor,
    IStorageService storageService,
    IMemoryCache memoryCache,
    ILogger<RegisterSlideRepository> logger) : IRegisterSlideRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<RegisterSlideResponseDTO>> GetRegisterSlides(CancellationToken ct)
    {
        return await db.RegisterSlides
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => ToDto(s))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<RegisterSlideResponseDTO?> GetRegisterSlide(int id, CancellationToken ct)
    {
        var slide = await db.RegisterSlides.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return slide == null ? null : ToDto(slide);
    }

    /// <inheritdoc />
    public async Task<RegisterSlideResponseDTO> CreateRegisterSlide(PostRegisterSlideDTO dto, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Creating register slide by user {UserId}.", userId);

        ExtensionValidator.ValidatePosterExtension(dto.Image);

        var slide = new RegisterSlide
        {
            SortOrder = dto.SortOrder ?? await GetNextSortOrder(ct)
        };

        using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            await SaveImage(slide, dto.Image);

            StateValidator.Validate(slide);

            db.RegisterSlides.Add(slide);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ToDto(slide);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Failed creating register slide.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateRegisterSlide(int id, RegisterSlideUpdateDTO dto, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Updating register slide {SlideId} by user {UserId}.", id, userId);

        var slide = await GetSlideOrThrow(id, ct);

        slide.SortOrder = dto.SortOrder;

        StateValidator.Validate(slide);

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeleteRegisterSlide(int id, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Deleting register slide {SlideId} by user {UserId}.", id, userId);

        var slide = await GetSlideOrThrow(id, ct);

        if (!string.IsNullOrEmpty(slide.ImagePath))
        {
            await storageService.DeleteFileAsync("register-slides", slide.ImagePath);
            memoryCache.Remove($"slide-img-{slide.ImagePath}");
        }

        db.RegisterSlides.Remove(slide);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<string?> UploadRegisterSlideImage(int id, Guid userId, IFormFile? image)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);
        logger.LogInformation("Uploading register slide image for slide {SlideId} by user {UserId}.", id, userId);

        var slide = await GetSlideOrThrow(id, default);

        if (image != null)
        {
            ExtensionValidator.ValidatePosterExtension(image);
        }

        string? oldPath = slide.ImagePath;

        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            if (image != null)
            {
                await SaveImage(slide, image);
            }
            else
            {
                slide.ImagePath = null;
                slide.ImageFileName = null;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            if (!string.IsNullOrEmpty(oldPath))
            {
                await storageService.DeleteFileAsync("register-slides", oldPath);
                memoryCache.Remove($"slide-img-{oldPath}");
            }

            return slide.ImagePath;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Failed uploading register slide image for slide {SlideId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FileResultDto?> GetRegisterSlideImageFile(string path)
    {
        string cacheKey = $"slide-img-{path}";
        if (memoryCache.TryGetValue(cacheKey, out (byte[] bytes, string contentType) cached))
        {
            return new FileResultDto
            {
                Stream = new MemoryStream(cached.bytes),
                ContentType = cached.contentType
            };
        }

        var file = await storageService.GetFileAsync("register-slides", path);
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

    private async Task<RegisterSlide> GetSlideOrThrow(int id, CancellationToken ct)
    {
        var slide = await db.RegisterSlides.FindAsync(new object[] { id }, ct);
        return slide ?? throw new KeyNotFoundException("Register slide not found.");
    }

    private async Task<int> GetNextSortOrder(CancellationToken ct)
    {
        var maxOrder = await db.RegisterSlides.MaxAsync(s => (int?)s.SortOrder, ct) ?? 0;
        return maxOrder + 1;
    }

    private async Task SaveImage(RegisterSlide slide, IFormFile image)
    {
        var compressedImage = await fileCompressor.CompressFileAsync(image);
        string path = await storageService.SaveFileAsync(
            compressedImage.Stream,
            compressedImage.ContentType,
            "register-slides"
        );

        slide.ImagePath = path;
        slide.ImageFileName = image.FileName;
    }

    private static RegisterSlideResponseDTO ToDto(RegisterSlide slide)
    {
        return new RegisterSlideResponseDTO
        {
            Id = slide.Id,
            SortOrder = slide.SortOrder,
            ImagePath = slide.ImagePath
        };
    }
}

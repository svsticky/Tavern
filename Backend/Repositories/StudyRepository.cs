using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    /// <summary>
    /// Implements study management operations.
    /// </summary>
    public class StudyRepository(
            PostgresDbContext db,
            IPermissionService permissionService,
            ILogger<StudyRepository> logger
        ) : IStudyService
    {
        /// <inheritdoc />
        public async Task<List<Study>> GetStudies(CancellationToken ct)
        {
            return await db.Studies.ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<Study?> GetStudy(uint id, CancellationToken ct)
        {
            return await db.Studies.FindAsync(id, ct);
        }

        /// <inheritdoc />
        public async Task<Study> CreateStudy(PostStudyDTO dto, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
            logger.LogInformation("Creating study by user {UserId}.", userId);
            var study = BuildStudy(dto);

            StateValidator.Validate(study);

            db.Studies.Add(study);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Created study {StudyId}.", study.Id);

                return study;
            }

        /// <inheritdoc />
        public async Task DeleteStudy(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
            logger.LogInformation("Deleting study {StudyId} by user {UserId}.", id, userId);
            var study = await GetStudyOrThrow(id, ct);

            db.Studies.Remove(study);
            await db.SaveChangesAsync(ct);
        }

        /// <inheritdoc />
        public async Task PatchStudy(uint id, JsonPatchDocument<Study> patchDoc, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
            logger.LogInformation("Patching study {StudyId} by user {UserId}.", id, userId);

            if (patchDoc == null)
                throw new Exception("Patch document is null");

            if(patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Cannot modify Id field.");

            var study = await GetStudyOrThrow(id, ct);

            patchDoc.ApplyTo(study);

                StateValidator.Validate(study);

                await db.SaveChangesAsync(ct);
            }

        /// <inheritdoc />
        public async Task UpdateStudy(uint id, StudyUpdateDTO dto, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
            logger.LogInformation("Updating study {StudyId} by user {UserId}.", id, userId);
            var study = await GetStudyOrThrow(id, ct);
            ApplyStudyUpdate(study, dto);

            StateValidator.Validate(study);

            await db.SaveChangesAsync(ct);
        }

        private static Study BuildStudy(PostStudyDTO dto)
        {
            return new Study
            {
                Title = dto.Title,
                NominalDurationYears = dto.NominalDurationYears,
                Type = dto.Type
            };
        }

        private static void ApplyStudyUpdate(Study study, StudyUpdateDTO dto)
        {
            study.Title = dto.Title;
            study.NominalDurationYears = dto.NominalDurationYears;
            study.Type = dto.Type;
        }

        private async Task<Study> GetStudyOrThrow(uint id, CancellationToken ct)
        {
            var study = await db.Studies.FindAsync(new object[] { id }, ct);
            return study ?? throw new Exception("Study not found");
        }
    }
}

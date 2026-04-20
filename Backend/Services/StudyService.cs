using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class StudyService(
            PostgresDbContext db,
            IPermissionService permissionService
        ) : IStudyService
    {
        public async Task<List<Study>> GetStudies(CancellationToken ct)
        {
            return await db.Studies.ToListAsync(ct);
        }

        public async Task<Study?> GetStudy(uint id, CancellationToken ct)
        {
            return await db.Studies.FindAsync(id, ct);
        }

        public async Task<Study> CreateStudy(PostStudyDTO dto, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);
            var study = BuildStudy(dto);

            StateValidator.Validate(study);

            db.Studies.Add(study);
                await db.SaveChangesAsync(ct);

                return study;
            }

        public async Task DeleteStudy(uint id, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);
            var study = await GetStudyOrThrow(id, ct);

            db.Studies.Remove(study);
            await db.SaveChangesAsync(ct);
        }

        public async Task PatchStudy(uint id, JsonPatchDocument<Study> patchDoc, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            if (patchDoc == null)
                throw new Exception("Patch document is null");

            var study = await GetStudyOrThrow(id, ct);

            patchDoc.ApplyTo(study);

                StateValidator.Validate(study);

                await db.SaveChangesAsync(ct);
            }

        public async Task UpdateStudy(uint id, StudyUpdateDTO dto, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);
            var study = await GetStudyOrThrow(id, ct);
            ApplyStudyUpdate(study, dto);

            StateValidator.Validate(study);

            await db.SaveChangesAsync(ct);
        }

        private void EnsureBoardMember(Guid userId)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
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

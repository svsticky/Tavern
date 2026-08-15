using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.QueryExtensions;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Domain
{
    /// <summary>
    /// Implements study-enrollment management operations.
    /// </summary>
    public class StudyEnrollmentService(
        PostgresDbContext db,
        IPermissionService permissionService,
        ILogger<StudyEnrollmentService> logger
    ) : IStudyEnrollmentService
    {
        /// <inheritdoc />
        public async Task<List<StudyEnrollmentResponseDTO>> GetStudyEnrollments(GetStudyEnrollmentsDTO dto, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.StudyEnrollments
                .AsQueryable()
                .IncludeDetails()
                .Filter(dto)
                .Select(StudyEnrollmentResponseDTO.ToDto())
                .ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<StudyEnrollmentResponseDTO?> GetStudyEnrollment(uint id, Guid userId, CancellationToken ct)
        {
            var result = await db.StudyEnrollments
                .Where(se => se.Id == id)
                .Select(StudyEnrollmentResponseDTO.ToDto())
                .FirstOrDefaultAsync(ct);

            if (result == null)
                return null;

            if (result.MemberId != userId)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return result;
        }

        /// <inheritdoc />
        public async Task<StudyEnrollmentResponseDTO> CreateStudyEnrollment(PostStudyEnrollmentDTO dto, Guid userId, CancellationToken ct)
        {
            if (dto.MemberId != userId)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            logger.LogInformation("Creating study enrollment for member {MemberId} study {StudyId} by user {UserId}.", dto.MemberId, dto.StudyId, userId);

            var member = await GetMemberOrThrow(dto.MemberId, ct);
            var study = await GetStudyOrThrow(dto.StudyId, ct);
            var enrollment = BuildStudyEnrollment(dto, member, study);

            StateValidator.Validate(enrollment);

            db.StudyEnrollments.Add(enrollment);
            await db.SaveChangesAsync(ct);

            return await db.StudyEnrollments
                .Where(se => se.Id == enrollment.Id)
                .Select(StudyEnrollmentResponseDTO.ToDto())
                .FirstAsync(ct);
        }

        /// <inheritdoc />
        public async Task DeleteStudyEnrollment(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
            logger.LogInformation("Deleting study enrollment {EnrollmentId} by user {UserId}.", id, userId);

            var enrollment = await db.StudyEnrollments.FindAsync(id, ct);
            if (enrollment == null)
                throw new Exception("Enrollment not found");

            db.StudyEnrollments.Remove(enrollment);
            await db.SaveChangesAsync(ct);
        }

        /// <inheritdoc />
        public async Task PatchStudyEnrollment(uint id, JsonPatchDocument<StudyEnrollment> patchDoc, Guid userId, CancellationToken ct)
        {
            logger.LogInformation("Patching study enrollment {EnrollmentId} by user {UserId}.", id, userId);

            if (patchDoc == null)
                throw new ArgumentException("Patch document is null");

            if (patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase)
                || op.path.Equals("/memberId", StringComparison.OrdinalIgnoreCase)
                || op.path.Equals("/member", StringComparison.OrdinalIgnoreCase)
                || op.path.Equals("/studyId", StringComparison.OrdinalIgnoreCase)
                || op.path.Equals("/study", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Cannot modify Id, MemberId or StudyId fields.");

            var enrollment = await db.StudyEnrollments.Include(e => e.Study).FirstOrDefaultAsync(e => e.Id == id, ct);

            ArgumentNullException.ThrowIfNull(enrollment, nameof(enrollment));

            if (userId != enrollment.MemberId || DateTime.UtcNow < enrollment.EnrollmentDate.AddYears((int)enrollment.Study.NominalDurationYears))
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            using var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                var oldStatus = enrollment.Status;

                patchDoc.ApplyTo(enrollment);

                if (oldStatus != enrollment.Status)
                {
                    switch (enrollment.Status)
                    {
                        case StudyStatus.Enrolled:
                            enrollment.CompletionDate = null;
                            break;
                        case StudyStatus.DroppedOut:
                        case StudyStatus.Completed:
                            enrollment.CompletionDate = DateTime.UtcNow;
                            break;
                    }
                }

                StateValidator.Validate(enrollment);

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                logger.LogError(ex, "Failed patching study enrollment {EnrollmentId}.", id);
                throw;
            }
        }

        private async Task<Member> GetMemberOrThrow(Guid memberId, CancellationToken ct)
        {
            var member = await db.Members.FindAsync(new object[] { memberId }, ct);
            return member ?? throw new Exception($"Member with ID {memberId} does not exist.");
        }

        private async Task<Study> GetStudyOrThrow(uint studyId, CancellationToken ct)
        {
            var study = await db.Studies.FindAsync(new object[] { studyId }, ct);
            return study ?? throw new Exception($"Study with ID {studyId} does not exist.");
        }

        private static StudyEnrollment BuildStudyEnrollment(PostStudyEnrollmentDTO dto, Member member, Study study)
        {
            return new StudyEnrollment
            {
                Member = member,
                Study = study,
                EnrollmentDate = dto.EnrollmentDate,
                Status = dto.Status,
                CompletionDate = dto.Status switch
                {
                    StudyStatus.Enrolled => null,
                    StudyStatus.DroppedOut => DateTime.UtcNow,
                    StudyStatus.Completed => DateTime.UtcNow,
                    _ => throw new ArgumentException("Invalid study status")
                }
            };
        }
    }
}

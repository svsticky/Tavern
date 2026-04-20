using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Projections;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class StudyEnrollmentService(
        PostgresDbContext db,
        IPermissionService permissionService
    ) : IStudyEnrollmentService
    {
        public async Task<List<StudyEnrollmentResponseDTO>> GetStudyEnrollments(GetStudyEnrollmentsDTO dto, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            return await db.StudyEnrollments
                .AsQueryable()
                .IncludeDetails()
                .Filter(dto)
                .Select(StudyEnrollmentProjections.ToDto())
                .ToListAsync(ct);
        }

        public async Task<StudyEnrollmentResponseDTO?> GetStudyEnrollment(uint id, Guid userId, CancellationToken ct)
        {
            var result = await db.StudyEnrollments
                .Where(se => se.Id == id)
                .Select(StudyEnrollmentProjections.ToDto())
                .FirstOrDefaultAsync(ct);

            if (result == null)
                return null;

            if (!permissionService.IsBoardOrCandidateBoardMember(userId) && result.MemberId != userId)
                throw new UnauthorizedAccessException("Only board members can view study enrollments of others.");

            return result;
        }

        public async Task<StudyEnrollmentResponseDTO> CreateStudyEnrollment(PostStudyEnrollmentDTO dto, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            var member = await GetMemberOrThrow(dto.MemberId, ct);
            var study = await GetStudyOrThrow(dto.StudyId, ct);
            var enrollment = BuildStudyEnrollment(dto, member, study);

            StateValidator.Validate(enrollment);

            db.StudyEnrollments.Add(enrollment);
            await db.SaveChangesAsync(ct);

            return await db.StudyEnrollments
                .Where(se => se.Id == enrollment.Id)
                .Select(StudyEnrollmentProjections.ToDto())
                .FirstAsync(ct);
        }

        public async Task DeleteStudyEnrollment(uint id, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);

            var enrollment = await db.StudyEnrollments.FindAsync(id, ct);
            if (enrollment == null)
                throw new Exception("Enrollment not found");

            db.StudyEnrollments.Remove(enrollment);
            await db.SaveChangesAsync(ct);
        }

        public async Task PatchStudy(uint id, JsonPatchDocument<StudyEnrollment> patchDoc, Guid userId, CancellationToken ct)
        {
            permissionService.EnsureBoardOrCandidateBoardMember(userId);
            
            var enrollment = await db.StudyEnrollments.FindAsync(id, ct);

            ArgumentNullException.ThrowIfNull(enrollment, nameof(enrollment));

            var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                var oldStatus = enrollment.Status;

                patchDoc.ApplyTo(enrollment);

                if(oldStatus != enrollment.Status)
                {
                    switch(enrollment.Status)
                    {
                        case StudyStatus.DroppedOut:
                        case StudyStatus.Enrolled:
                            enrollment.CompletionDate = null;
                            break;
                        case StudyStatus.Completed:
                            enrollment.CompletionDate = DateTime.UtcNow;
                            break;
                    }
                }

                StateValidator.Validate(enrollment);

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
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
                Status = dto.Status
            };
        }
    }
}

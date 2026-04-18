using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
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
            EnsureBoardMember(userId);

            var query = db.StudyEnrollments
                .Include(se => se.Member)
                .Include(se => se.Study)
                .AsQueryable();

            if (dto.MemberId != null)
            {
                query = query.Where(se => se.MemberId == dto.MemberId);
            }

            return await query
                .Select(se => new StudyEnrollmentResponseDTO
                {
                    Id = se.Id,
                    MemberId = se.MemberId,
                    MemberName = $"{se.Member.FirstName} {se.Member.LastName}",
                    StudyId = se.StudyId,
                    StudyTitle = se.Study.Title,
                    EnrollmentDate = se.EnrollmentDate,
                    CompletionDate = se.CompletionDate,
                    Status = se.Status,
                })
                .ToListAsync(ct);
        }

        public async Task<StudyEnrollmentResponseDTO?> GetStudyEnrollment(uint id, Guid userId, CancellationToken ct)
        {
            var result = await db.StudyEnrollments
                .Where(se => se.Id == id)
                .Select(se => new StudyEnrollmentResponseDTO
                {
                    Id = se.Id,
                    MemberId = se.MemberId,
                    MemberName = $"{se.Member.FirstName} {se.Member.LastName}",
                    StudyId = se.StudyId,
                    StudyTitle = se.Study.Title,
                    EnrollmentDate = se.EnrollmentDate,
                    CompletionDate = se.CompletionDate,
                    Status = se.Status
                })
                .FirstOrDefaultAsync(ct);

            if (result == null)
                return null;

            if (!permissionService.IsBoardOrCandidateBoardMember(userId) && result.MemberId != userId)
                throw new UnauthorizedAccessException("Only board members can view study enrollments of others.");

            return result;
        }

        public async Task<StudyEnrollmentResponseDTO> CreateStudyEnrollment(PostStudyEnrollmentDTO dto, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            var member = await db.Members.FindAsync(dto.MemberId, ct);
            if (member == null)
                throw new Exception($"Member with ID {dto.MemberId} does not exist.");

            var study = await db.Studies.FindAsync(dto.StudyId, ct);
            if (study == null)
                throw new Exception($"Study with ID {dto.StudyId} does not exist.");

            var enrollment = new StudyEnrollment
            {
                Member = member,
                Study = study,
                EnrollmentDate = dto.EnrollmentDate,
                Status = dto.Status
            };

            StateValidator.Validate(enrollment);

            db.StudyEnrollments.Add(enrollment);
            await db.SaveChangesAsync(ct);

            return new StudyEnrollmentResponseDTO
            {
                Id = enrollment.Id,
                MemberId = enrollment.MemberId,
                StudyTitle = study.Title,
                StudyId = enrollment.StudyId,
                EnrollmentDate = enrollment.EnrollmentDate,
                CompletionDate = enrollment.CompletionDate,
                Status = enrollment.Status
            };
        }

        public async Task DeleteStudyEnrollment(uint id, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            var enrollment = await db.StudyEnrollments.FindAsync(id, ct);
            if (enrollment == null)
                throw new Exception("Enrollment not found");

            db.StudyEnrollments.Remove(enrollment);
            await db.SaveChangesAsync(ct);
        }

        public async Task PatchStudy(uint id, JsonPatchDocument<StudyEnrollment> patchDoc, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);
            
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

        private void EnsureBoardMember(Guid userId)
        {
            if (!permissionService.IsBoardOrCandidateBoardMember(userId))
            {
                throw new UnauthorizedAccessException("Only board members can perform this action.");
            }
        }
    }
}
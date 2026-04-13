using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class StudyEnrollmentService(
        PostgresDbContext db,
        IPermissionService permissionService
    ) : IStudyEnrollmentService
    {
        public async Task<List<StudyEnrollmentResponseDTO>> GetStudyEnrollments(Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            return await db.StudyEnrollments
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

            if (!IsBoardMember(userId) && result.MemberId != userId)
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

            StateValidateUtils.Validate(enrollment);

            db.StudyEnrollments.Add(enrollment);
            await db.SaveChangesAsync(ct);

            return new StudyEnrollmentResponseDTO
            {
                Id = enrollment.Id,
                MemberId = enrollment.MemberId,
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

        public async Task UpdateStatus(uint id, StudyStatus newStatus, Guid userId, CancellationToken ct)
        {
            EnsureBoardMember(userId);

            var enrollment = await db.StudyEnrollments.FindAsync(id, ct);
            if (enrollment == null)
                throw new Exception("Enrollment not found");

            StateValidateUtils.Validate(enrollment);

            enrollment.Status = newStatus;
            await db.SaveChangesAsync(ct);
        }

        private void EnsureBoardMember(Guid userId)
        {
            if (!IsBoardMember(userId))
            {
                throw new UnauthorizedAccessException("Only board members can perform this action.");
            }
        }

        private bool IsBoardMember(Guid userId)
        {
            return permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board);
        }
    }
}
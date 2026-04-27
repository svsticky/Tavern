using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Services;

public class PaymentValidationService(
    PostgresDbContext db,
    ILogger<PaymentValidationService> logger) : IPaymentValidationService
{
    public bool HasPaidMembershipPayment(Guid memberId)
    {
         var member = db.Members
            .Include(m => m.StudyEnrollments)
            .ThenInclude(se => se.Study)
            .FirstOrDefault(m => m.Id == memberId);

        if (member == null)
        {
            logger.LogWarning("Membership payment check failed: member {MemberId} not found.", memberId);
            throw new Exception($"Member with id {memberId} not found.");
        }

        bool isMaster = member.StudyEnrollments.Any(e => e.Study.Type == StudyType.Master);
        
        if (isMaster) return true;

        return db.MembershipPayments.Any(p => p.MemberId == member.Id && p.PaidAt != null);
    }

    public IEnumerable<EnrollmentBalance> GetUnpaidEnrollmentsForMember(Guid memberId)
    {
        return db.Enrollments
            .Where(e => e.MemberId == memberId)
            .Include(e => e.Member)
            .Include(e => e.Activity)
            .Select(e => new 
            {
                Enrollment = e,
                PaidSum = db.EnrollmentPayments
                    .Where(p => p.PaidAt != null && p.ActivityId == e.ActivityId && p.MemberId == e.MemberId)
                    .Sum(p => (decimal?)p.Price) ?? 0
            })
            .Where(x => x.PaidSum < x.Enrollment.Price && x.Enrollment.Activity.IsOpenForPayment && !x.Enrollment.IsOnWaitingList)
            .AsEnumerable() 
            .Select(x => new EnrollmentBalance{ Enrollment = x.Enrollment, Balance = x.Enrollment.Price - x.PaidSum });
    }

    public decimal GetUnpaidAmountForEnrollment(Enrollment enrollment)
    {
        var paidSum = db.EnrollmentPayments
            .Include(p => p.Activity)
            .Where(p => p.PaidAt != null && p.ActivityId == enrollment.ActivityId && p.MemberId == enrollment.MemberId && p.Activity != null && p.Activity.IsOpenForPayment && !enrollment.IsOnWaitingList)
            .Sum(p => (decimal?)p.Price) ?? 0;

        return enrollment.Price - paidSum;
    }

    public IEnumerable<EnrollmentBalance> GetAllUnpaidEnrollments()
    {
        return db.Enrollments
            .Include(e => e.Member)
            .Include(e => e.Activity)
            .Select(e => new 
            {
                Enrollment = e,
                PaidSum = db.EnrollmentPayments
                    .Where(p => p.PaidAt != null && p.ActivityId == e.ActivityId && p.MemberId == e.MemberId)
                    .Sum(p => (decimal?)p.Price) ?? 0
            })
            .Where(x => x.PaidSum < x.Enrollment.Price && x.Enrollment.Activity.IsOpenForPayment && !x.Enrollment.IsOnWaitingList)
            .AsEnumerable()
            .Select(x => new EnrollmentBalance{ Enrollment = x.Enrollment, Balance = x.Enrollment.Price - x.PaidSum });
    }

    public IEnumerable<EnrollmentBalance> GetAllOverpaidEnrollments()
    {
        return db.Enrollments
            .Include(e => e.Activity)
            .Include(e => e.Member)
            .Select(e => new 
            {
                Enrollment = e,
                PaidSum = db.EnrollmentPayments
                    .Where(p => p.PaidAt != null && p.ActivityId == e.ActivityId && p.MemberId == e.MemberId)
                    .Sum(p => (decimal?)p.Price) ?? 0
            })
            .Where(x => x.PaidSum > x.Enrollment.Price && x.Enrollment.Activity.IsOpenForPayment && !x.Enrollment.IsOnWaitingList)
            .AsEnumerable()
            .Select(x => new EnrollmentBalance{ Enrollment = x.Enrollment, Balance = x.PaidSum - x.Enrollment.Price });
    }

    public bool MemberHasPaidAllActivities(Member member)
    {
        return !GetUnpaidEnrollmentsForMember(member.Id).Any();
    }
}

using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Utils;

public static class PaymentUtils
{
    public static bool HasPaidMembershipPayment(Member member, PostgresDbContext db)
    {
        bool isMaster = member.StudyEnrollments.Any(e => e.Study.Type == StudyType.Master);
        
        if (isMaster) return true;

        return db.MembershipPayments.Any(p => p.MemberId == member.Id && p.PaidAt != null);
    }

    public static IEnumerable<EnrollmentBalance> GetUnpaidEnrollmentsForMember(Member member, PostgresDbContext db)
    {
        return db.Enrollments
            .Where(e => e.MemberId == member.Id) // All enrollments for the member
            .Select(e => new 
            {
                Enrollment = e,
                PaidSum = db.EnrollmentPayments
                    .Where(p => p.PaidAt != null && p.ActivityId == e.ActivityId && p.MemberId == e.MemberId)
                    .Sum(p => (decimal?)p.Price) ?? 0
            }) // List of member enrollments with their paid sums
            .Where(x => x.PaidSum < x.Enrollment.Price) // Filter to only those enrollments where the paid sum is less than the enrollment price
            .Include(e=> e.Enrollment.Member) // Include the Member navigation property to avoid lazy loading issues
            .Include(e => e.Enrollment.Activity) // Include the Activity navigation property to avoid lazy loading issues
            .Select(x => new EnrollmentBalance(x.Enrollment, x.Enrollment.Price - x.PaidSum));
    }

    public static decimal GetUnpaidAmountForEnrollment(Enrollment enrollment, PostgresDbContext db)
    {
        var paidSum = db.EnrollmentPayments
            .Where(p => p.PaidAt != null && p.ActivityId == enrollment.ActivityId && p.MemberId == enrollment.MemberId)
            .Sum(p => (decimal?)p.Price) ?? 0;

        return enrollment.Price - paidSum;
    }

    public static IEnumerable<EnrollmentBalance> GetAllUnpaidEnrollments(PostgresDbContext db)
    {
        return db.Enrollments
            .Select(e => new 
            {
                Enrollment = e,
                PaidSum = db.EnrollmentPayments
                    .Where(p => p.PaidAt != null && p.ActivityId == e.ActivityId && p.MemberId == e.MemberId)
                    .Sum(p => (decimal?)p.Price) ?? 0
            }) // List of all enrollments with their paid sums
            .Where(x => x.PaidSum < x.Enrollment.Price) // Filter to only those enrollments where the paid sum is less than the enrollment price
            .Include(e=> e.Enrollment.Member) // Include the Member navigation property to avoid lazy loading issues
            .Include(e => e.Enrollment.Activity) // Include the Activity navigation property to avoid lazy loading issues
            .Select(x => new EnrollmentBalance(x.Enrollment, x.Enrollment.Price - x.PaidSum));
    }

    public static IEnumerable<EnrollmentBalance> GetAllOverpaidEnrollments(PostgresDbContext db)
    {
        return db.Enrollments
            .Select(e => new 
            {
                Enrollment = e,
                PaidSum = db.EnrollmentPayments
                    .Where(p => p.PaidAt != null && p.ActivityId == e.ActivityId && p.MemberId == e.MemberId)
                    .Sum(p => (decimal?)p.Price) ?? 0
            }) // List of all enrollments with their paid sums
            .Where(x => x.PaidSum > x.Enrollment.Price) // Filter to only those enrollments where the paid sum is greater than the enrollment price
            .Include(e=> e.Enrollment.Member) // Include the Member navigation property to avoid lazy loading issues
            .Include(e => e.Enrollment.Activity) // Include the Activity navigation property to avoid lazy loading issues
            .Select(x => new EnrollmentBalance(x.Enrollment, x.PaidSum - x.Enrollment.Price));
    }

    public static bool MemberHasPaidAllActivities(Member member, PostgresDbContext db)
    {
        return !GetUnpaidEnrollmentsForMember(member, db).Any();
    }
}
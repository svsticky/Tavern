using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Domain;

/// <summary>
/// Implements payment validation and balance calculations.
/// </summary>
public class PaymentValidationService(
    PostgresDbContext db,
    ILogger<PaymentValidationService> logger) : IPaymentValidationService
{
    /// <inheritdoc />
    public bool HasPaidMembershipPaymentBeforeExpirationTime(Guid memberId)
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

        if (db.Settings.Find("MastersShouldPayMembership")?.Value != "1" && member.StudyEnrollments.Any(e => e.Study.Type == StudyType.Master && (e.Status == StudyStatus.Enrolled)))
        {
            return true;
        }

        if (db.Settings.Find("GratieShouldPayMembership")?.Value != "1" && member.Gratie)
        {
            return true;
        }

        if (db.Settings.Find("ErelidShouldPayMembership")?.Value != "1" && member.EreLid)
        {
            return true;
        }

        if (db.Settings.Find("LidVanVerdiensteShouldPayMembership")?.Value != "1" && member.LidVanVerdienste)
        {
            return true;
        }

        string? paymentExpirationTime = db.Settings.FirstOrDefault(s => s.Name == "MembershipPaymentExpirationTime")?.Value;
        if (paymentExpirationTime != null && int.TryParse(paymentExpirationTime, out int expirationYears))
        {
            var studyEnrollment = member.StudyEnrollments
                .OrderBy(e => e.EnrollmentDate)
                .FirstOrDefault();

            if (studyEnrollment == null)
            {
                // If member has no study enrollment, fallback to rolling expiration window (or simple PaidAt threshold)
                DateTime expirationThreshold = DateTime.UtcNow.AddYears(-expirationYears);
                return db.MembershipPayments.Any(p => p.MemberId == memberId && p.PaidAt != null && p.PaidAt >= expirationThreshold);
            }

            if (studyEnrollment.EnrollmentDate.AddYears(expirationYears) > DateTime.UtcNow)
            {
                // Allow the first payment to be made up to 6 months before the first study enrollment date
                return db.MembershipPayments.Any(p => p.MemberId == memberId && p.PaidAt != null && p.PaidAt >= studyEnrollment.EnrollmentDate.AddMonths(-6));
            }

            // If member has a study enrollment:
            // Calculate next recurrence date of study start date on or after DateTime.UtcNow.
            // A grace period of 2 months is added to allow paying near the end of the membership cycle for the upcoming period.
            var now = DateTime.UtcNow;
            var startDate = studyEnrollment.EnrollmentDate.DateTime;

            // Build candidate date in current calendar year
            if (startDate.Month == 2 && startDate.Day == 29 && !DateTime.IsLeapYear(now.Year))
            {
                // Handle leap year case: if the study start date is Feb 29, use Feb 28 in non-leap years
                startDate = new DateTime(now.Year, 2, 28, startDate.Hour, startDate.Minute, startDate.Second, DateTimeKind.Utc);
            }

            var nextOccurrence = new DateTime(now.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, startDate.Second, DateTimeKind.Utc);
            if (now >= nextOccurrence)
            {
                nextOccurrence = nextOccurrence.AddYears(1);
            }

            // Expiration threshold is expirationYears prior to the next recurrence date
            DateTime expirationThresholdDate = nextOccurrence.AddYears(-expirationYears).AddMonths(-2); // Add 2 months grace period to allow paying near the end of the membership cycle for the upcoming period.

            return db.MembershipPayments.Any(p => p.MemberId == memberId && p.PaidAt != null && p.PaidAt >= expirationThresholdDate);
        }
        else
        {
            return db.MembershipPayments.Any(p => p.MemberId == memberId && p.PaidAt != null);
        }
    }

    /// <inheritdoc />
    public bool HasEverPaidMembershipPayment(Guid memberId)
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

        if (db.Settings.Find("MastersShouldPayMembership")?.Value != "1" && member.StudyEnrollments.Any(e => e.Study.Type == StudyType.Master && (e.Status == StudyStatus.Enrolled)))
        {
            return true;
        }

        if (db.Settings.Find("GratieShouldPayMembership")?.Value != "1" && member.Gratie)
        {
            return true;
        }

        if (db.Settings.Find("ErelidShouldPayMembership")?.Value != "1" && member.EreLid)
        {
            return true;
        }

        if (db.Settings.Find("LidVanVerdiensteShouldPayMembership")?.Value != "1" && member.LidVanVerdienste)
        {
            return true;
        }

        return db.MembershipPayments.Any(p => p.MemberId == memberId && p.PaidAt != null);
    }

    /// <inheritdoc />
    public bool HasEverPaidBegunstigerFee(Guid memberId)
    {
        return db.BegunstigerPayments.Any(p => p.MemberId == memberId && p.PaidAt != null);
    }

    /// <inheritdoc />
    public bool HasDoneOrDoingStudy(Guid memberId)
    {
        return db.StudyEnrollments.Any(e => e.MemberId == memberId);
    }

    /// <inheritdoc />
    public bool HasPaidBegunstigerFeeSinceLastBoardChange(Guid memberId)
    {
        var lastBoardRotationAtValue = db.Settings.Find("LastBoardRotationAt")?.Value;
        var threshold = DateTimeOffset.TryParse(lastBoardRotationAtValue, out var parsed) ? parsed : DateTimeOffset.MinValue;

        return db.BegunstigerPayments.Any(p => p.MemberId == memberId && p.PaidAt != null && p.PaidAt >= threshold);
    }

    /// <inheritdoc />
    public IEnumerable<EnrollmentBalance> GetUnpaidEnrollmentsForMember(Guid memberId, bool includeNotOpenForPayment = false)
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
            .Where(x => x.PaidSum < x.Enrollment.Price && (includeNotOpenForPayment || x.Enrollment.Activity.IsOpenForPayment) && !x.Enrollment.IsOnWaitingList)
            .AsEnumerable()
            .Select(x => new EnrollmentBalance { Enrollment = x.Enrollment, Balance = x.Enrollment.Price - x.PaidSum });
    }

    /// <inheritdoc />
    public decimal GetUnpaidAmountForEnrollment(Enrollment enrollment, bool includeNotOpenForPayment = false)
    {
        if (enrollment.IsOnWaitingList) return 0;

        var paidSum = db.EnrollmentPayments
            .Include(p => p.Activity)
            .Where(p => p.PaidAt != null && p.ActivityId == enrollment.ActivityId && p.MemberId == enrollment.MemberId && p.Activity != null && (includeNotOpenForPayment || p.Activity.IsOpenForPayment))
            .Sum(p => (decimal?)p.Price) ?? 0;

        return enrollment.Price - paidSum;
    }

    /// <inheritdoc />
    public IEnumerable<EnrollmentBalance> GetAllUnpaidEnrollments(bool includeNotOpenForPayment = false)
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
            .Where(x => x.PaidSum < x.Enrollment.Price && (includeNotOpenForPayment || x.Enrollment.Activity.IsOpenForPayment) && !x.Enrollment.IsOnWaitingList)
            .AsEnumerable()
            .Select(x => new EnrollmentBalance { Enrollment = x.Enrollment, Balance = x.Enrollment.Price - x.PaidSum });
    }

    /// <inheritdoc />
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
            .Select(x => new EnrollmentBalance { Enrollment = x.Enrollment, Balance = x.PaidSum - x.Enrollment.Price });
    }

    /// <inheritdoc />
    public bool MemberHasPaidAllActivities(Member member)
    {
        return !GetUnpaidEnrollmentsForMember(member.Id, true).Any();
    }
}

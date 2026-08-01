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

        if (member.Begunstiger) return true;

        var settingNames = new[]
        {
            "MastersShouldPayMembership",
            "GratieShouldPayMembership",
            "ErelidShouldPayMembership",
            "LidVanVerdiensteShouldPayMembership",
            "MembershipPaymentExpirationTime",
        };
        var settings = db.Settings
            .Where(s => settingNames.Contains(s.Name))
            .ToDictionary(s => s.Name, s => s.Value);

        settings.TryGetValue("MastersShouldPayMembership", out var mastersShouldPay);
        if (mastersShouldPay != "1" && member.StudyEnrollments.Any(e => e.Study.Type == StudyType.Master && e.Status == StudyStatus.Enrolled))
            return true;

        settings.TryGetValue("GratieShouldPayMembership", out var gratieShouldPay);
        if (gratieShouldPay != "1" && member.Gratie)
            return true;

        settings.TryGetValue("ErelidShouldPayMembership", out var erelidShouldPay);
        if (erelidShouldPay != "1" && member.EreLid)
            return true;

        settings.TryGetValue("LidVanVerdiensteShouldPayMembership", out var lidVanVerdienteShouldPay);
        if (lidVanVerdienteShouldPay != "1" && member.LidVanVerdienste)
            return true;

        settings.TryGetValue("MembershipPaymentExpirationTime", out var paymentExpirationTime);
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

            // If member has a study enrollment:
            // Calculate next recurrence date of study start date on or after DateTime.UtcNow.
            // A grace period of 2 months is added to allow paying near the end of the membership cycle for the upcoming period.
            var now = DateTime.UtcNow;
            var startDate = studyEnrollment.EnrollmentDate.DateTime;

            // Build candidate date in current calendar year
            var nextOccurrence = new DateTime(now.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, startDate.Second, DateTimeKind.Utc);
            if (now >= nextOccurrence)
            {
                nextOccurrence = nextOccurrence.AddYears(1);
            }

            // Expiration threshold is expirationYears prior to the next recurrence date
            DateTime expirationThresholdDate = nextOccurrence.AddYears(-expirationYears).AddMonths(-2); // Add 2 months grace period to allow paying near the end of the membership cycle for the upcoming period.

            return db.MembershipPayments.Any(p => p.MemberId == memberId && p.PaidAt != null && p.PaidAt >= expirationThresholdDate);
        }

        return db.MembershipPayments.Any(p => p.MemberId == memberId && p.PaidAt != null);
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

        if (member.Begunstiger) return true;

        bool isMaster = member.StudyEnrollments.Any(e => e.Study.Type == StudyType.Master);
        if (isMaster) return true;

        return db.MembershipPayments.Any(p => p.MemberId == memberId && p.PaidAt != null);
    }

    /// <inheritdoc />
    public IEnumerable<EnrollmentBalance> GetUnpaidEnrollmentsForMember(Guid memberId)
    {
        var enrollments = db.Enrollments
            .Include(e => e.Member)
            .Include(e => e.Activity)
            .Where(e => e.MemberId == memberId && e.Activity.IsOpenForPayment && !e.IsOnWaitingList)
            .AsNoTracking()
            .ToList();

        if (enrollments.Count == 0)
            return Enumerable.Empty<EnrollmentBalance>();

        var activityIds = enrollments.Select(e => e.ActivityId).ToHashSet();
        var paidSums = db.EnrollmentPayments
            .Where(p => p.PaidAt != null && p.MemberId == memberId && p.ActivityId.HasValue && activityIds.Contains(p.ActivityId!.Value))
            .GroupBy(p => p.ActivityId!.Value)
            .Select(g => new { ActivityId = g.Key, Total = g.Sum(p => p.Price) })
            .ToDictionary(x => x.ActivityId, x => (decimal)x.Total);

        return enrollments
            .Select(e => {
                paidSums.TryGetValue(e.ActivityId, out var paid);
                return new EnrollmentBalance { Enrollment = e, Balance = e.Price - paid };
            })
            .Where(x => x.Balance > 0);
    }

    /// <inheritdoc />
    public decimal GetUnpaidAmountForEnrollment(Enrollment enrollment)
    {
        if (!enrollment.Activity.IsOpenForPayment || enrollment.IsOnWaitingList)
            return 0;

        var paidSum = db.EnrollmentPayments
            .Where(p => p.PaidAt != null && p.ActivityId == enrollment.ActivityId && p.MemberId == enrollment.MemberId)
            .Sum(p => (decimal?)p.Price) ?? 0;

        return enrollment.Price - paidSum;
    }

    /// <inheritdoc />
    public IEnumerable<EnrollmentBalance> GetAllUnpaidEnrollments()
    {
        var enrollments = db.Enrollments
            .Include(e => e.Member)
            .Include(e => e.Activity)
            .Where(e => e.Activity.IsOpenForPayment && !e.IsOnWaitingList)
            .AsNoTracking()
            .ToList();

        if (enrollments.Count == 0)
            return Enumerable.Empty<EnrollmentBalance>();

        var activityIds = enrollments.Select(e => e.ActivityId).ToHashSet();
        var paidSums = db.EnrollmentPayments
            .Where(p => p.PaidAt != null && p.ActivityId.HasValue && p.MemberId.HasValue && activityIds.Contains(p.ActivityId!.Value))
            .GroupBy(p => new { ActivityId = p.ActivityId!.Value, MemberId = p.MemberId!.Value })
            .Select(g => new { g.Key.ActivityId, g.Key.MemberId, Total = g.Sum(p => p.Price) })
            .ToDictionary(x => (x.ActivityId, x.MemberId), x => (decimal)x.Total);

        return enrollments
            .Select(e => {
                paidSums.TryGetValue((e.ActivityId, e.MemberId), out var paid);
                return new EnrollmentBalance { Enrollment = e, Balance = e.Price - paid };
            })
            .Where(x => x.Balance > 0);
    }

    /// <inheritdoc />
    public IEnumerable<EnrollmentBalance> GetAllOverpaidEnrollments()
    {
        var enrollments = db.Enrollments
            .Include(e => e.Member)
            .Include(e => e.Activity)
            .Where(e => e.Activity.IsOpenForPayment && !e.IsOnWaitingList)
            .AsNoTracking()
            .ToList();

        if (enrollments.Count == 0)
            return Enumerable.Empty<EnrollmentBalance>();

        var activityIds = enrollments.Select(e => e.ActivityId).ToHashSet();
        var paidSums = db.EnrollmentPayments
            .Where(p => p.PaidAt != null && p.ActivityId.HasValue && p.MemberId.HasValue && activityIds.Contains(p.ActivityId!.Value))
            .GroupBy(p => new { ActivityId = p.ActivityId!.Value, MemberId = p.MemberId!.Value })
            .Select(g => new { g.Key.ActivityId, g.Key.MemberId, Total = g.Sum(p => p.Price) })
            .ToDictionary(x => (x.ActivityId, x.MemberId), x => (decimal)x.Total);

        return enrollments
            .Select(e => {
                paidSums.TryGetValue((e.ActivityId, e.MemberId), out var paid);
                return new EnrollmentBalance { Enrollment = e, Balance = paid - e.Price };
            })
            .Where(x => x.Balance > 0);
    }

    /// <inheritdoc />
    public bool MemberHasPaidAllActivities(Member member)
    {
        return !GetUnpaidEnrollmentsForMember(member.Id).Any();
    }
}

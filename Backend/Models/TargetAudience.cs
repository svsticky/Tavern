using Backend.Models.Domain;
using Backend.Utils.DateTime;

namespace Backend.Models;

/// <summary>
/// Represents the target audience for an activity, allowing for the specification of which groups of members are eligible to participate in a given activity. The TargetAudience enum defines various audience options, such as FirstYears, SecondYears, ThirdYearsAndAbove, and Masters, which can be combined using bitwise operations to create more specific audience groups. This entity is used to manage and enforce eligibility criteria for activities based on the academic standing of members, ensuring that activities are appropriately targeted and accessible to the intended audience within the system.
/// </summary>
[Flags]
public enum TargetAudience : uint
{
    None = 0,
    FirstYears = 1 << 0,          // 1
    SecondYears = 1 << 1,         // 2
    ThirdYearsAndAbove = 1 << 2,  // 4
    Masters = 1 << 3,             // 8
    Gratie = 1 << 4,              // 16
    ActiveMembers = 1 << 5,        // 32
    All = FirstYears | SecondYears | ThirdYearsAndAbove | Masters | Gratie | ActiveMembers // 63
}

/// <summary>
/// Provides helper methods for working with the TargetAudience enum, including functionality to determine if a member belongs to a specific target audience based on their study enrollments. The TargetAudienceHelper class includes the IsMemberInTargetAudience method, which checks if a given member falls within the specified target audience by evaluating their study enrollments and academic standing. This helper class is essential for enforcing eligibility criteria for activities and ensuring that members are appropriately categorized based on their academic progress within the system.
/// </summary>
public static class TargetAudienceHelper
{
    /// <summary>
    /// Determines if a member belongs to a specific target audience based on their study enrollments and academic standing. This method evaluates the member's study enrollments to check if they meet the criteria for being classified as a first-year, second-year, third-year or above, or master's student, based on the enrollment dates and study types. The method returns true if the member belongs to any of the specified target audience groups, allowing for effective enforcement of eligibility criteria for activities within the system.
    /// </summary>
    /// <param name="member">The member to evaluate.</param>
    /// <param name="targetAudience">The target audience to check against.</param>
    /// <returns>True if the member belongs to the specified target audience, otherwise false.</returns>
    public static bool IsMemberInTargetAudience(Member member, TargetAudience targetAudience)
    {
        if (targetAudience.HasFlag(TargetAudience.FirstYears) && member.StudyEnrollments.Any(se => 
            (se.CompletionDate == null || se.CompletionDate > DateTimeOffset.UtcNow)
            && se.Study.Type == StudyType.Bachelor 
            && se.EnrollmentDate >= DateTimeOffset.UtcNow.AddYears(-1)))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.SecondYears) && member.StudyEnrollments.Any(se =>
            (se.CompletionDate == null || se.CompletionDate > DateTimeOffset.UtcNow)
            && se.Study.Type == StudyType.Bachelor
            && se.EnrollmentDate >= DateTimeOffset.UtcNow.AddYears(-2) 
            && se.EnrollmentDate < DateTimeOffset.UtcNow.AddYears(-1)))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.ThirdYearsAndAbove) && member.StudyEnrollments.Any(se => 
            (se.CompletionDate == null || se.CompletionDate > DateTimeOffset.UtcNow)
            && se.Study.Type == StudyType.Bachelor
            && se.EnrollmentDate < DateTimeOffset.UtcNow.AddYears(-2)))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.Masters) && member.StudyEnrollments.Any(se => 
            (se.CompletionDate == null || se.CompletionDate > DateTimeOffset.UtcNow)
            && se.Study.Type == StudyType.Master))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.Gratie) && member.Gratie)
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.ActiveMembers) && member.GroupMemberships?.Any(gm => gm.MembershipYear == FinancialYearUtils.GetCurrentFinancialYear()) == true)
        {
            return true;
        }

        return false;
    }
}
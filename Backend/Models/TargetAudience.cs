using Backend.Models.Domain;
using Backend.Utils.DateTime;

namespace Backend.Models;

/// <summary>
/// Represents the target audience for an activity, allowing for the specification of which groups of members are eligible to participate in a given activity. The TargetAudience enum defines various audience options, such as FirstYears, SecondYears, ThirdYearsAndAbove, and Masters, which can be combined using bitwise operations to create more specific audience groups. This entity is used to manage and enforce eligibility criteria for activities based on the academic standing of members, ensuring that activities are appropriately targeted and accessible to the intended audience within the system.
/// </summary>
[Flags]
public enum TargetAudience : uint
{
    /// <summary>
    /// Represents no specific target audience, indicating that the activity is not limited to any particular group of members. This value can be used when an activity is open to all members regardless of their academic standing or other criteria, allowing for maximum inclusivity and accessibility within the system.
    /// </summary>
    None = 0,

    /// <summary>
    /// Represents first-year students as the target audience for an activity, indicating that the activity is specifically designed for or limited to members who are in their first year of study. This value can be used to ensure that certain activities are tailored to the needs and interests of first-year students, providing them with relevant opportunities and experiences within the system.
    /// </summary>
    FirstYears = 1 << 0,          // 1

    /// <summary>
    /// Represents second-year students as the target audience for an activity, indicating that the activity is specifically designed for or limited to members who are in their second year of study. This value can be used to ensure that certain activities are tailored to the needs and interests of second-year students, providing them with relevant opportunities and experiences within the system.
    /// </summary>
    SecondYears = 1 << 1,         // 2

    /// <summary>
    /// Represents third-year students and above as the target audience for an activity, indicating that the activity is specifically designed for or limited to members who are in their third year of study or higher. This value can be used to ensure that certain activities are tailored to the needs and interests of more advanced students, providing them with relevant opportunities and experiences within the system while excluding those who are in their first or second year.
    /// </summary>
    ThirdYearsAndAbove = 1 << 2,  // 4

    /// <summary>
    /// Represents master's students as the target audience for an activity, indicating that the activity is specifically designed for or limited to members who are pursuing a master's degree. This value can be used to ensure that certain activities are tailored to the needs and interests of master's students, providing them with relevant opportunities and experiences within the system while excluding those who are in bachelor's programs or other academic tracks.
    /// </summary>
    Masters = 1 << 3,             // 8

    /// <summary>
    /// Represents members with a "gratie" status as the target audience for an activity, indicating that the activity is specifically designed for or limited to members who have been granted a "gratie" status, which may be a special designation within the system based on certain criteria. This value can be used to ensure that certain activities are tailored to the needs and interests of members with this status, providing them with relevant opportunities and experiences while excluding those who do not meet the criteria for "gratie" status.
     ///
    /// </summary>
    Gratie = 1 << 4,              // 16

    /// <summary>
    /// Represents active members as the target audience for an activity, indicating that the activity is specifically designed for or limited to members who are currently active within the system, which may be determined based on their group memberships or other criteria. This value can be used to ensure that certain activities are tailored to the needs and interests of active members, providing them with relevant opportunities and experiences while excluding those who are not currently active within the system.
    /// </summary>
    ActiveMembers = 1 << 5,        // 32

    /// <summary>
    /// Represents all possible target audiences for an activity, indicating that the activity is designed for or open to all groups of members regardless of their academic standing or other criteria. This value can be used when an activity is intended to be inclusive and accessible to all members within the system, allowing for maximum participation and engagement across different member groups.
    /// </summary>
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
            se.Status == StudyStatus.Enrolled
            && se.Study.Type == StudyType.Bachelor 
            && se.EnrollmentDate >= DateTimeOffset.UtcNow.AddYears(-1)))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.SecondYears) && member.StudyEnrollments.Any(se =>
            se.Status == StudyStatus.Enrolled
            && se.Study.Type == StudyType.Bachelor
            && se.EnrollmentDate >= DateTimeOffset.UtcNow.AddYears(-2) 
            && se.EnrollmentDate < DateTimeOffset.UtcNow.AddYears(-1)))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.ThirdYearsAndAbove) && member.StudyEnrollments.Any(se => 
            se.Status == StudyStatus.Enrolled
            && se.Study.Type == StudyType.Bachelor
            && se.EnrollmentDate < DateTimeOffset.UtcNow.AddYears(-2)))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.Masters) && member.StudyEnrollments.Any(se => 
            se.Status == StudyStatus.Enrolled
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
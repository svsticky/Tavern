using Backend.Models.Domain;

namespace Backend.Models;

[Flags]
public enum TargetAudience : uint
{
    None = 0,
    FirstYears = 1 << 0,          // 1
    SecondYears = 1 << 1,         // 2
    ThirdYearsAndAbove = 1 << 2,  // 4
    Masters = 1 << 3,             // 8
    All = FirstYears | SecondYears | ThirdYearsAndAbove | Masters
}

public static class TargetAudienceHelper
{
    public static bool IsMemberInTargetAudience(Member member, TargetAudience targetAudience)
    {
        if (targetAudience == TargetAudience.None)
        {
            return false;
        }

        if  (targetAudience == TargetAudience.All)
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.FirstYears) && member.StudyEnrollments.Any(se => se.CompletionDate == null 
            && se.Study.Type == StudyType.Bachelor 
            && se.EnrollmentDate >= DateTimeOffset.UtcNow.AddYears(-1)))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.SecondYears) && member.StudyEnrollments.Any(se => se.CompletionDate == null 
            && se.Study.Type == StudyType.Bachelor
            && se.EnrollmentDate >= DateTimeOffset.UtcNow.AddYears(-2) 
            && se.EnrollmentDate < DateTimeOffset.UtcNow.AddYears(-1)))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.ThirdYearsAndAbove) && member.StudyEnrollments.Any(se => se.CompletionDate == null 
            && se.Study.Type == StudyType.Bachelor
            && se.EnrollmentDate < DateTimeOffset.UtcNow.AddYears(-2)))
        {
            return true;
        }

        if (targetAudience.HasFlag(TargetAudience.Masters) && member.StudyEnrollments.Any(se => se.CompletionDate == null 
            && se.Study.Type == StudyType.Master))
        {
            return true;
        }

        return false;
    }
}
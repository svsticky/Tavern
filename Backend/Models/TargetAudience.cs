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
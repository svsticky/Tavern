using Backend.Models;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using Xunit;

namespace Backend.Tests.Models;

public class ModelsTests
{
    [Fact]
    public void Setting_GettersAndSetters_WorkCorrectly()
    {
        var setting = new Setting
        {
            Name = "TestName",
            Value = "TestValue"
        };

        Assert.Equal("TestName", setting.Name);
        Assert.Equal("TestValue", setting.Value);
    }

    [Fact]
    public void TargetAudienceHelper_IsMemberInTargetAudience_None_ReturnsFalse()
    {
        var member = new Member { FirstName = "A", LastName = "B", Email = "a@b.com", StudentNumber = "s1", PhoneNumber = "1", Street = "S", HouseNumber = "1", PostalCode = "P", City = "C" };
        member.StudyEnrollments = new List<StudyEnrollment>();

        var result = TargetAudienceHelper.IsMemberInTargetAudience(member, TargetAudience.None);

        Assert.False(result);
    }

    [Fact]
    public void TargetAudienceHelper_FirstYears_Check()
    {
        var member = new Member { FirstName = "A", LastName = "B", Email = "a@b.com", StudentNumber = "s1", PhoneNumber = "1", Street = "S", HouseNumber = "1", PostalCode = "P", City = "C" };
        var study = new Study { Id = 1, Title = "CS", Type = StudyType.Bachelor };
        member.StudyEnrollments = new List<StudyEnrollment>
        {
            new StudyEnrollment
            {
                Study = study,
                Status = StudyStatus.Enrolled,
                EnrollmentDate = DateTimeOffset.UtcNow.AddMonths(-6)
            }
        };

        var resultTrue = TargetAudienceHelper.IsMemberInTargetAudience(member, TargetAudience.FirstYears);
        var resultFalse = TargetAudienceHelper.IsMemberInTargetAudience(member, TargetAudience.SecondYears);

        Assert.True(resultTrue);
        Assert.False(resultFalse);
    }

    [Fact]
    public void TargetAudienceHelper_SecondYears_Check()
    {
        var member = new Member { FirstName = "A", LastName = "B", Email = "a@b.com", StudentNumber = "s1", PhoneNumber = "1", Street = "S", HouseNumber = "1", PostalCode = "P", City = "C" };
        var study = new Study { Id = 1, Title = "CS", Type = StudyType.Bachelor };
        member.StudyEnrollments = new List<StudyEnrollment>
        {
            new StudyEnrollment
            {
                Study = study,
                Status = StudyStatus.Enrolled,
                EnrollmentDate = DateTimeOffset.UtcNow.AddMonths(-18)
            }
        };

        var resultTrue = TargetAudienceHelper.IsMemberInTargetAudience(member, TargetAudience.SecondYears);

        Assert.True(resultTrue);
    }

    [Fact]
    public void TargetAudienceHelper_ThirdYearsAndAbove_Check()
    {
        var member = new Member { FirstName = "A", LastName = "B", Email = "a@b.com", StudentNumber = "s1", PhoneNumber = "1", Street = "S", HouseNumber = "1", PostalCode = "P", City = "C" };
        var study = new Study { Id = 1, Title = "CS", Type = StudyType.Bachelor };
        member.StudyEnrollments = new List<StudyEnrollment>
        {
            new StudyEnrollment
            {
                Study = study,
                Status = StudyStatus.Enrolled,
                EnrollmentDate = DateTimeOffset.UtcNow.AddYears(-3)
            }
        };

        var resultTrue = TargetAudienceHelper.IsMemberInTargetAudience(member, TargetAudience.ThirdYearsAndAbove);

        Assert.True(resultTrue);
    }

    [Fact]
    public void TargetAudienceHelper_Masters_Check()
    {
        var member = new Member { FirstName = "A", LastName = "B", Email = "a@b.com", StudentNumber = "s1", PhoneNumber = "1", Street = "S", HouseNumber = "1", PostalCode = "P", City = "C" };
        var study = new Study { Id = 1, Title = "CS", Type = StudyType.Master };
        member.StudyEnrollments = new List<StudyEnrollment>
        {
            new StudyEnrollment
            {
                Study = study,
                Status = StudyStatus.Enrolled,
                EnrollmentDate = DateTimeOffset.UtcNow
            }
        };

        var resultTrue = TargetAudienceHelper.IsMemberInTargetAudience(member, TargetAudience.Masters);

        Assert.True(resultTrue);
    }

    [Fact]
    public void TargetAudienceHelper_Gratie_Check()
    {
        var member = new Member { FirstName = "A", LastName = "B", Email = "a@b.com", StudentNumber = "s1", PhoneNumber = "1", Street = "S", HouseNumber = "1", PostalCode = "P", City = "C", Gratie = true };
        member.StudyEnrollments = new List<StudyEnrollment>();

        var resultTrue = TargetAudienceHelper.IsMemberInTargetAudience(member, TargetAudience.Gratie);

        Assert.True(resultTrue);
    }

    [Fact]
    public void TargetAudienceHelper_ActiveMembers_Check()
    {
        var member = new Member { FirstName = "A", LastName = "B", Email = "a@b.com", StudentNumber = "s1", PhoneNumber = "1", Street = "S", HouseNumber = "1", PostalCode = "P", City = "C" };
        member.StudyEnrollments = new List<StudyEnrollment>();
        member.GroupMemberships = new List<GroupMembership>
        {
            new GroupMembership
            {
                GroupId = 1u,
                MembershipYear = YearUtils.GetCurrentFinancialYear()
            }
        };

        var resultTrue = TargetAudienceHelper.IsMemberInTargetAudience(member, TargetAudience.ActiveMembers);

        Assert.True(resultTrue);
    }
}

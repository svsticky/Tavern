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

    [Fact]
    public void Activity_GetDescription_ReturnsCorrectLanguageDescription()
    {
        var activity = new Activity
        {
            DutchDescription = "Nederlandse omschrijving",
            EnglishDescription = "English description",
            PaymentDeadline = DateTimeOffset.UtcNow
        };

        Assert.Equal("Nederlandse omschrijving", activity.GetDescription(Language.NL));
        Assert.Equal("English description", activity.GetDescription(Language.EN));
        Assert.Equal("English description", activity.GetDescription((Language)999));
    }

    [Fact]
    public void Activity_Properties_GetAndSetCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var organizerGroup = new Group { Id = 5, Name = "Test Group" };
        var specQuestions = new List<SpecificationQuestion>();
        var enrollments = new List<Enrollment>();

        var activity = new Activity
        {
            Id = 1,
            Name = "Party",
            Price = 12.50m,
            PosterFileName = "poster.png",
            PosterPath = "/path/to/poster.png",
            DutchDescription = "Feest",
            EnglishDescription = "Party",
            DateTimeStart = now,
            DateTimeEnd = now.AddHours(4),
            UnenrollmentDeadline = now.AddDays(-1),
            EnrollmentDeadline = now.AddDays(-1),
            EnrollOpenDate = now.AddDays(-7),
            Location = "Tavern",
            ParticipantLimit = 50,
            OrganizerId = 5,
            Organizer = organizerGroup,
            SpecificationQuestions = specQuestions,
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = true,
            AllowedAudience = TargetAudience.FirstYears,
            IsOpenForPayment = true,
            VatRate = 21,
            Enrollments = enrollments,
            GLAccountId = "1000",
            CostCenterId = "2000",
            CostUnitId = "3000",
            PaymentDeadline = now.AddDays(2)
        };

        Assert.Equal(1u, activity.Id);
        Assert.Equal("Party", activity.Name);
        Assert.Equal(12.50m, activity.Price);
        Assert.Equal("poster.png", activity.PosterFileName);
        Assert.Equal("/path/to/poster.png", activity.PosterPath);
        Assert.Equal("Feest", activity.DutchDescription);
        Assert.Equal("Party", activity.EnglishDescription);
        Assert.Equal(now, activity.DateTimeStart);
        Assert.Equal(now.AddHours(4), activity.DateTimeEnd);
        Assert.Equal(now.AddDays(-1), activity.UnenrollmentDeadline);
        Assert.Equal(now.AddDays(-1), activity.EnrollmentDeadline);
        Assert.Equal(now.AddDays(-7), activity.EnrollOpenDate);
        Assert.Equal("Tavern", activity.Location);
        Assert.Equal(50u, activity.ParticipantLimit);
        Assert.Equal(5u, activity.OrganizerId);
        Assert.Same(organizerGroup, activity.Organizer);
        Assert.Same(specQuestions, activity.SpecificationQuestions);
        Assert.True(activity.ShowInKoala);
        Assert.True(activity.ShowOnWebsite);
        Assert.True(activity.IsEnrollable);
        Assert.True(activity.AreParticipantsVisible);
        Assert.False(activity.IsAdultOnly);
        Assert.True(activity.IsWeeklyDrinks);
        Assert.Equal(TargetAudience.FirstYears, activity.AllowedAudience);
        Assert.True(activity.IsOpenForPayment);
        Assert.Equal(21u, activity.VatRate);
        Assert.Same(enrollments, activity.Enrollments);
        Assert.Equal("1000", activity.GLAccountId);
        Assert.Equal("2000", activity.CostCenterId);
        Assert.Equal("3000", activity.CostUnitId);
        Assert.Equal(now.AddDays(2), activity.PaymentDeadline);
    }
}

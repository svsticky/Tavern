using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Backend.QueryExtensions;

namespace Backend.Tests.QueryExtensions;

public class MemberQueryExtensionsTests
{
    private Member CreateMember(
        string firstName, 
        string lastName, 
        string email, 
        string studentNumber,
        string phoneNumber,
        bool gratie = false,
        bool lidVanVerdienste = false,
        bool ereLid = false,
        bool begunstiger = false,
        bool suspended = false)
    {
        return new Member
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            StudentNumber = studentNumber,
            PhoneNumber = phoneNumber,
            Street = "Main Street",
            HouseNumber = "42",
            PostalCode = "1234AB",
            City = "Enschede",
            Gratie = gratie,
            LidVanVerdienste = lidVanVerdienste,
            EreLid = ereLid,
            Begunstiger = begunstiger,
            Suspended = suspended,
            StudyEnrollments = new List<StudyEnrollment>()
        };
    }

    [Fact]
    public void Filter_WithSearchOnName_ReturnsMatching()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111");
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222");
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { Search = "Doe" };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public void Filter_WithSearchOnEmail_ReturnsMatching()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111");
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222");
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { Search = "jane@" };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("Jane", result[0].FirstName);
    }

    [Fact]
    public void Filter_WithSearchOnStudentNumber_ReturnsMatching()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111");
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222");
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { Search = "222" };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("Jane", result[0].FirstName);
    }

    [Fact]
    public void Filter_WithSearchOnPhoneNumber_ReturnsMatching()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111");
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222");
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { Search = "+31622" };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("Jane", result[0].FirstName);
    }

    [Fact]
    public void Filter_ByStudyId_FiltersCorrectly()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111");
        m1.StudyEnrollments.Add(new StudyEnrollment { StudyId = 10 });
        
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222");
        m2.StudyEnrollments.Add(new StudyEnrollment { StudyId = 20 });
        
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { StudyId = 10 };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public void Filter_ByGratie_FiltersCorrectly()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111", gratie: true);
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222", gratie: false);
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { Gratie = true };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public void Filter_ByLidVanVerdienste_FiltersCorrectly()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111", lidVanVerdienste: true);
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222", lidVanVerdienste: false);
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { LidVanVerdienste = true };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public void Filter_ByEreLid_FiltersCorrectly()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111", ereLid: true);
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222", ereLid: false);
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { EreLid = true };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public void Filter_ByBegunstiger_FiltersCorrectly()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111", begunstiger: true);
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222", begunstiger: false);
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { Begunstiger = true };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public void Filter_BySuspended_FiltersCorrectly()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111", suspended: true);
        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222", suspended: false);
        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { Suspended = true };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public void Filter_ByInactiveTrue_FiltersCorrectly()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111");
        m1.StudyEnrollments.Add(new StudyEnrollment { Status = StudyStatus.Completed });

        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222");
        m2.StudyEnrollments.Add(new StudyEnrollment { Status = StudyStatus.Enrolled });

        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { Inactive = true };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public void Filter_ByInactiveFalse_FiltersCorrectly()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111");
        m1.StudyEnrollments.Add(new StudyEnrollment { Status = StudyStatus.Completed });

        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222");
        m2.StudyEnrollments.Add(new StudyEnrollment { Status = StudyStatus.Enrolled });

        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { Inactive = false };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("Jane", result[0].FirstName);
    }

    [Fact]
    public void Filter_ByStudyType_FiltersCorrectly()
    {
        var m1 = CreateMember("John", "Doe", "john@example.com", "s1111111", "+31611111111");
        m1.StudyEnrollments.Add(new StudyEnrollment { Study = new Study { Type = StudyType.Bachelor } });

        var m2 = CreateMember("Jane", "Smith", "jane@example.com", "s2222222", "+31622222222");
        m2.StudyEnrollments.Add(new StudyEnrollment { Study = new Study { Type = StudyType.Master } });

        var list = new List<Member> { m1, m2 }.AsQueryable();

        var dto = new GetMembersDto { StudyType = StudyType.Bachelor };
        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
    }

    [Fact]
    public void ApplyPaging_CorrectlyPaginates()
    {
        var members = new List<Member>();
        for (int i = 0; i < 10; i++)
        {
            members.Add(CreateMember($"FN{i}", "LN", "e@e.com", $"s{i}", "123"));
        }
        var query = members.AsQueryable();

        var dto = new GetMembersDto { Page = 2, PageSize = 3 };
        var result = query.ApplyPaging(dto).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("FN3", result[0].FirstName);
        Assert.Equal("FN4", result[1].FirstName);
        Assert.Equal("FN5", result[2].FirstName);
    }
}

using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Backend.QueryExtensions;

namespace Backend.Tests.QueryExtensions;

public class StudyEnrollmentQueryExtensionsTests
{
    [Fact]
    public void Filter_WithMemberId_FiltersByMemberId()
    {
        var memberId1 = Guid.NewGuid();
        var memberId2 = Guid.NewGuid();
        var list = new List<StudyEnrollment>
        {
            new() { MemberId = memberId1, StudyId = 1 },
            new() { MemberId = memberId1, StudyId = 2 },
            new() { MemberId = memberId2, StudyId = 1 }
        }.AsQueryable();

        var dto = new GetStudyEnrollmentsDTO { MemberId = memberId1 };

        var result = list.Filter(dto).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, se => Assert.Equal(memberId1, se.MemberId));
    }

    [Fact]
    public void Filter_WithNullMemberId_ReturnsAll()
    {
        var memberId1 = Guid.NewGuid();
        var memberId2 = Guid.NewGuid();
        var list = new List<StudyEnrollment>
        {
            new() { MemberId = memberId1, StudyId = 1 },
            new() { MemberId = memberId1, StudyId = 2 },
            new() { MemberId = memberId2, StudyId = 1 }
        }.AsQueryable();

        var dto = new GetStudyEnrollmentsDTO { MemberId = null };

        var result = list.Filter(dto).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void IncludeDetails_ReturnsQueryable()
    {
        var list = new List<StudyEnrollment>
        {
            new() { MemberId = Guid.NewGuid(), StudyId = 1 }
        }.AsQueryable();

        var result = list.IncludeDetails();

        Assert.NotNull(result);
        Assert.Single(result);
    }
}

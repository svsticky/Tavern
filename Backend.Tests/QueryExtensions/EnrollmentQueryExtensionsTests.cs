using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Backend.QueryExtensions;

namespace Backend.Tests.QueryExtensions;

public class EnrollmentQueryExtensionsTests
{
    [Fact]
    public void Filter_WithMemberId_FiltersByMemberId()
    {
        var memberId1 = Guid.NewGuid();
        var memberId2 = Guid.NewGuid();
        var list = new List<Enrollment>
        {
            new() { ActivityId = 1, MemberId = memberId1 },
            new() { ActivityId = 2, MemberId = memberId1 },
            new() { ActivityId = 1, MemberId = memberId2 }
        }.AsQueryable();

        var dto = new GetEnrollmentsDTO { FromMemberId = memberId1 };

        var result = list.Filter(dto).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(memberId1, e.MemberId));
    }

    [Fact]
    public void Filter_WithNullMemberId_ReturnsAll()
    {
        var memberId1 = Guid.NewGuid();
        var memberId2 = Guid.NewGuid();
        var list = new List<Enrollment>
        {
            new() { ActivityId = 1, MemberId = memberId1 },
            new() { ActivityId = 2, MemberId = memberId1 },
            new() { ActivityId = 1, MemberId = memberId2 }
        }.AsQueryable();

        var dto = new GetEnrollmentsDTO { FromMemberId = null };

        var result = list.Filter(dto).ToList();

        Assert.Equal(3, result.Count);
    }
}

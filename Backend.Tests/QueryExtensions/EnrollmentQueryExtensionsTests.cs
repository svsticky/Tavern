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
        var activity = new Activity { Id = 1, ShowInKoala = true, PaymentDeadline = DateTimeOffset.UtcNow };
        var list = new List<Enrollment>
        {
            new() { ActivityId = 1, MemberId = memberId1, Activity = activity },
            new() { ActivityId = 2, MemberId = memberId1, Activity = activity },
            new() { ActivityId = 1, MemberId = memberId2, Activity = activity }
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
        var activity = new Activity { Id = 1, ShowInKoala = true, PaymentDeadline = DateTimeOffset.UtcNow };
        var list = new List<Enrollment>
        {
            new() { ActivityId = 1, MemberId = memberId1, Activity = activity },
            new() { ActivityId = 2, MemberId = memberId1, Activity = activity },
            new() { ActivityId = 1, MemberId = memberId2, Activity = activity }
        }.AsQueryable();

        var dto = new GetEnrollmentsDTO { FromMemberId = null };

        var result = list.Filter(dto).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_ActivityNotShownInKoala_ExcludesEnrollment()
    {
        var memberId = Guid.NewGuid();
        var visibleActivity = new Activity { Id = 1, ShowInKoala = true, PaymentDeadline = DateTimeOffset.UtcNow };
        var draftActivity = new Activity { Id = 2, ShowInKoala = false, PaymentDeadline = DateTimeOffset.UtcNow };
        var list = new List<Enrollment>
        {
            new() { ActivityId = 1, MemberId = memberId, Activity = visibleActivity },
            new() { ActivityId = 2, MemberId = memberId, Activity = draftActivity }
        }.AsQueryable();

        var dto = new GetEnrollmentsDTO { FromMemberId = memberId };

        var result = list.Filter(dto).ToList();

        Assert.Single(result);
        Assert.Equal(1u, result[0].ActivityId);
    }
}

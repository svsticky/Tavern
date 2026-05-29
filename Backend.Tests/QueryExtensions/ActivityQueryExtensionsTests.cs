using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Backend.QueryExtensions;

namespace Backend.Tests.QueryExtensions;

public class ActivityQueryExtensionsTests
{
    private List<Activity> GetTestActivities()
    {
        return new List<Activity>
        {
            new()
            {
                Id = 1,
                Name = "Past Activity",
                ShowInKoala = true,
                ShowOnWebsite = true,
                DateTimeStart = DateTimeOffset.UtcNow.AddDays(-10),
                DateTimeEnd = DateTimeOffset.UtcNow.AddDays(-9),
                OrganizerId = 1,
                PaymentDeadline = DateTimeOffset.UtcNow.AddDays(-8)
            },
            new()
            {
                Id = 2,
                Name = "Future Activity",
                ShowInKoala = true,
                ShowOnWebsite = true,
                DateTimeStart = DateTimeOffset.UtcNow.AddDays(5),
                DateTimeEnd = DateTimeOffset.UtcNow.AddDays(6),
                OrganizerId = 1,
                PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7),
                IsOpenForPayment = true
            },
            new()
            {
                Id = 3,
                Name = "Hidden Activity",
                ShowInKoala = false,
                ShowOnWebsite = false,
                DateTimeStart = DateTimeOffset.UtcNow.AddDays(2),
                DateTimeEnd = DateTimeOffset.UtcNow.AddDays(3),
                OrganizerId = 2,
                PaymentDeadline = DateTimeOffset.UtcNow.AddDays(4),
                EnrollOpenDate = null
            }
        };
    }

    [Fact]
    public void Filter_AsBoardMember_IncludesAll()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_AsRegularMember_HidesHiddenActivitiesIfNotInOrganizerGroup()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true };

        var result = query.Filter(dto, isBoard: false, userGroupIds: new uint[] { 1 }).ToList();

        // 1 & 2 are ShowInKoala=true. 3 is hidden and organizer group is 2 (user is in 1), so it's hidden.
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, a => a.Id == 3);
    }

    [Fact]
    public void Filter_AsRegularMember_ShowsHiddenActivityIfInOrganizerGroup()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true };

        var result = query.Filter(dto, isBoard: false, userGroupIds: new uint[] { 2 }).ToList();

        // 1 & 2 are shown. 3 is shown because user is in group 2.
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_ExcludePast_FiltersCorrectly()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = false, IncludeFuture = true };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, a => a.Id == 1); // Past activity is filtered out
    }

    [Fact]
    public void Filter_ExcludeFuture_FiltersCorrectly()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true, IncludeFuture = false };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }).ToList();

        Assert.Single(result);
        Assert.Equal(1u, result[0].Id); // Only past activity is kept
    }

    [Fact]
    public void Filter_ByYear_FiltersCorrectly()
    {
        var query = GetTestActivities().AsQueryable();
        var currentYear = (uint)DateTimeOffset.UtcNow.Year;
        var dto = new GetActivitiesDTO { IncludePast = true, Year = currentYear };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }).ToList();

        Assert.Equal(3, result.Count); // All are in the current year
    }

    [Fact]
    public void Filter_ByOpenForPayment_FiltersCorrectly()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { OpenForPayment = true };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }).ToList();

        Assert.Single(result);
        Assert.Equal(2u, result[0].Id); // Future Activity is open for payment
    }
}

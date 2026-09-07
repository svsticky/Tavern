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
                ShowInKoala = false,
                ShowOnWebsite = false,
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

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_AsRegularMember_HidesHiddenActivitiesIfNotInOrganizerGroup()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true };

        var result = query.Filter(dto, isBoard: false, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        // 1 is hidden (wrong group). 2 is visible (ShowInKoala=true). 3 is hidden (wrong group).
        Assert.Single(result);
        Assert.Equal(2u, result[0].Id);
    }

    [Fact]
    public void Filter_AsRegularMember_ShowsHiddenActivityIfInOrganizerGroup()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true };

        var result = query.Filter(dto, isBoard: false, userGroupIds: new uint[] { 2 }, isLoggedIn: true).ToList();

        // 2 & 3 are shown. 1 is past (so hidden).
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Id == 2);
        Assert.Contains(result, a => a.Id == 3);
    }

    [Fact]
    public void Filter_ExcludePast_FiltersCorrectly()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = false, IncludeFuture = true };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, a => a.Id == 1); // Past activity is filtered out
    }

    [Fact]
    public void Filter_ExcludeFuture_FiltersCorrectly()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true, IncludeFuture = false };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        Assert.Single(result);
        Assert.Equal(1u, result[0].Id); // Only past activity is kept
    }

    [Fact]
    public void Filter_ByYear_FiltersCorrectly()
    {
        var query = GetTestActivities().AsQueryable();
        var currentYear = Backend.Utils.DateTime.YearUtils.GetYearForDate(System.DateTime.UtcNow, Backend.Utils.DateTime.YearUtils.CommitteeCreationDate);
        var dto = new GetActivitiesDTO { IncludePast = true, Year = currentYear };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        Assert.Equal(3, result.Count); // All are in the current year
    }

    [Fact]
    public void Filter_ByOpenForPayment_FiltersCorrectly()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { OpenForPayment = true };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        Assert.Single(result);
        Assert.Equal(2u, result[0].Id); // Future Activity is open for payment
    }

    [Fact]
    public void Filter_AsGuest_OnlyShowsFutureAndShowOnWebsite()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO();

        var result = query.Filter(dto, isBoard: false, userGroupIds: new uint[] { }, isLoggedIn: false).ToList();

        // 1 is past (so hidden).
        // 2 is future and ShowOnWebsite=true (so visible).
        // 3 is future but ShowOnWebsite=false (so hidden).
        Assert.Single(result);
        Assert.Equal(2u, result[0].Id);
    }

    [Fact]
    public void Filter_ByUserId_OnlyShowsActivitiesTheUserIsEnrolledIn()
    {
        var memberId = Guid.NewGuid();
        var activities = GetTestActivities();
        activities[0].Enrollments = new List<Enrollment>();
        activities[1].Enrollments = new List<Enrollment>
        {
            new() { ActivityId = 2, MemberId = memberId, Price = 0, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false }
        };
        activities[2].Enrollments = new List<Enrollment>
        {
            // On the waiting list - shouldn't count as an enrollment for this filter
            new() { ActivityId = 3, MemberId = memberId, Price = 0, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = true }
        };

        var dto = new GetActivitiesDTO { IncludePast = true, UserId = memberId };
        var result = activities.AsQueryable().Filter(dto, isBoard: true, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        Assert.Single(result);
        Assert.Equal(2u, result[0].Id);
    }

    [Fact]
    public void ApplyPaging_NoPageOrPageSize_ReturnsAllUnpaginated()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO();

        var result = query.ApplyPaging(dto).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ApplyPaging_WithPageAndPageSize_ReturnsCorrectSlice()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { Page = 1, PageSize = 2 };

        var result = query.ApplyPaging(dto).ToList();

        // Ordered descending by DateTimeStart: Activity 2 (+5d), Activity 3 (+2d), Activity 1 (-10d)
        Assert.Equal(2, result.Count);
        Assert.Equal(2u, result[0].Id);
        Assert.Equal(3u, result[1].Id);
    }

    [Fact]
    public void ApplyPaging_SecondPage_SkipsFirstPage()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { Page = 2, PageSize = 2 };

        var result = query.ApplyPaging(dto).ToList();

        Assert.Single(result);
        Assert.Equal(1u, result[0].Id);
    }

    [Fact]
    public void ApplyPaging_PageWithoutPageSize_DefaultsToFiftyPerPage()
    {
        var query = GetTestActivities().AsQueryable();
        var dto = new GetActivitiesDTO { Page = 1 };

        var result = query.ApplyPaging(dto).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_ExcludesArchivedActivitiesByDefault()
    {
        var activities = GetTestActivities();
        activities[1].IsArchived = true;
        var query = activities.AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, a => a.Id == 2);
    }

    [Fact]
    public void Filter_WhenIsArchivedTrue_ReturnsOnlyArchivedActivities()
    {
        var activities = GetTestActivities();
        activities[1].IsArchived = true;
        var query = activities.AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true, IsArchived = true };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        Assert.Single(result);
        Assert.Equal(2u, result[0].Id);
    }

    [Fact]
    public void Filter_WhenIsArchivedFalse_ReturnsOnlyNonArchivedActivities()
    {
        var activities = GetTestActivities();
        activities[1].IsArchived = true;
        var query = activities.AsQueryable();
        var dto = new GetActivitiesDTO { IncludePast = true, IsArchived = false };

        var result = query.Filter(dto, isBoard: true, userGroupIds: new uint[] { }, isLoggedIn: true).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, a => a.Id == 2);
    }

    [Fact]
    public void Filter_WhenNotLoggedIn_ExcludesArchivedActivities()
    {
        var activities = GetTestActivities();
        activities[1].IsArchived = true;
        var query = activities.AsQueryable();
        var dto = new GetActivitiesDTO();

        var result = query.Filter(dto, isBoard: false, userGroupIds: new uint[] { }, isLoggedIn: false).ToList();

        Assert.Empty(result);
    }
}


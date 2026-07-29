using Backend.Database;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Utils.DateTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Backend.Tests.Services;

public class PermissionServiceTests : IDisposable
{
    private readonly PostgresDbContext _db;
    private readonly ILogger<PermissionService> _loggerMock;
    private readonly PermissionService _service;

    public PermissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _db = new PostgresDbContext(options);
        _db.Database.EnsureCreated();

        _loggerMock = Substitute.For<ILogger<PermissionService>>();
        _service = new PermissionService(_db, _loggerMock);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    private Member CreateMember(Guid? id = null)
    {
        return new Member
        {
            Id = id ?? Guid.NewGuid(),
            Suspended = false,
            DateOfBirth = new DateTime(2000, 1, 1),
            StudentNumber = "s" + Guid.NewGuid().ToString().Substring(0, 7),
            FirstName = "John",
            LastName = "Doe",
            Email = Guid.NewGuid().ToString() + "@example.com",
            PhoneNumber = "+31612345678",
            Street = "Main Street",
            HouseNumber = "42",
            PostalCode = "1234AB",
            City = "Enschede",
            GroupMemberships = new List<GroupMembership>()
        };
    }

    [Fact]
    public void IsInGroup_MemberInGroup_ReturnsTrue()
    {
        var memberId = Guid.NewGuid();
        uint groupId = 10;
        uint year = 2026;

        _db.GroupMemberships.Add(new GroupMembership
        {
            MemberId = memberId,
            GroupId = groupId,
            MembershipYear = year
        });
        _db.SaveChanges();

        var result = _service.IsInGroup(memberId, groupId, year);

        Assert.True(result);
    }

    [Fact]
    public void IsInGroup_MemberNotInGroup_ReturnsFalse()
    {
        var memberId = Guid.NewGuid();
        var otherMemberId = Guid.NewGuid();
        uint groupId = 10;
        uint year = 2026;

        _db.GroupMemberships.Add(new GroupMembership
        {
            MemberId = otherMemberId,
            GroupId = groupId,
            MembershipYear = year
        });
        _db.SaveChanges();

        var result = _service.IsInGroup(memberId, groupId, year);

        Assert.False(result);
    }

    [Fact]
    public void IsInGroup_MemberModel_InGroup_ReturnsTrue()
    {
        var member = CreateMember();
        uint groupId = 10;
        uint year = 2026;

        member.GroupMemberships.Add(new GroupMembership
        {
            GroupId = groupId,
            MembershipYear = year
        });

        var result = _service.IsInGroup(member, groupId, year);

        Assert.True(result);
    }

    [Fact]
    public void IsInGroupInCurrentYear_Guid_ReturnsTrue()
    {
        var memberId = Guid.NewGuid();
        uint groupId = 10;
        uint currentYear = YearUtils.GetCurrentFinancialYear();

        _db.GroupMemberships.Add(new GroupMembership
        {
            MemberId = memberId,
            GroupId = groupId,
            MembershipYear = currentYear
        });
        _db.SaveChanges();

        var result = _service.IsInGroupInCurrentYear(memberId, groupId);

        Assert.True(result);
    }

    [Fact]
    public void IsInGroupInCurrentYear_MemberModel_ReturnsTrue()
    {
        uint groupId = 10;
        uint currentYear = YearUtils.GetCurrentFinancialYear();
        var member = CreateMember();
        member.GroupMemberships.Add(new GroupMembership { GroupId = groupId, MembershipYear = currentYear });

        var result = _service.IsInGroupInCurrentYear(member, groupId);

        Assert.True(result);
    }

    [Fact]
    public void IsInRole_MemberInRole_ReturnsTrue()
    {
        var memberId = Guid.NewGuid();
        uint roleId = 5;
        uint year = 2026;

        _db.GroupMemberships.Add(new GroupMembership
        {
            MemberId = memberId,
            MembershipYear = year,
            RoleAlias = new RoleAlias { RoleId = roleId, Name = "RoleName" }
        });
        _db.SaveChanges();

        var result = _service.IsInRole(memberId, roleId, year);

        Assert.True(result);
    }

    [Fact]
    public void IsInRole_WithGroupId_MemberInRole_ReturnsTrue()
    {
        var memberId = Guid.NewGuid();
        uint roleId = 5;
        uint groupId = 10;
        uint year = 2026;

        _db.GroupMemberships.Add(new GroupMembership
        {
            MemberId = memberId,
            GroupId = groupId,
            MembershipYear = year,
            RoleAlias = new RoleAlias { RoleId = roleId, Name = "RoleName" }
        });
        _db.SaveChanges();

        var result = _service.IsInRole(memberId, roleId, year, groupId);

        Assert.True(result);
    }

    [Fact]
    public void IsInRole_MemberModel_InRole_ReturnsTrue()
    {
        uint roleId = 5;
        uint year = 2026;
        var member = CreateMember();
        member.GroupMemberships.Add(new GroupMembership
        {
            MembershipYear = year,
            RoleAlias = new RoleAlias { RoleId = roleId, Name = "RoleName" }
        });

        var result = _service.IsInRole(member, roleId, year);

        Assert.True(result);
    }

    [Fact]
    public void IsInRole_MemberModel_WithGroupId_InRole_ReturnsTrue()
    {
        uint roleId = 5;
        uint groupId = 10;
        uint year = 2026;
        var member = CreateMember();
        member.GroupMemberships.Add(new GroupMembership
        {
            GroupId = groupId,
            MembershipYear = year,
            RoleAlias = new RoleAlias { RoleId = roleId, Name = "RoleName" }
        });

        var result = _service.IsInRole(member, roleId, year, groupId);

        Assert.True(result);
    }

    [Fact]
    public void IsInRoleInCurrentYear_Guid_ReturnsTrue()
    {
        var memberId = Guid.NewGuid();
        uint roleId = 5;
        uint currentYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);

        _db.GroupMemberships.Add(new GroupMembership
        {
            MemberId = memberId,
            MembershipYear = currentYear,
            RoleAlias = new RoleAlias { RoleId = roleId, Name = "RoleName" }
        });
        _db.SaveChanges();

        var result = _service.IsInRoleInCurrentYear(memberId, roleId);

        Assert.True(result);
    }

    [Fact]
    public void IsInRoleInCurrentYear_MemberModel_ReturnsTrue()
    {
        uint roleId = 5;
        uint currentYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var member = CreateMember();
        member.GroupMemberships.Add(new GroupMembership
        {
            MembershipYear = currentYear,
            RoleAlias = new RoleAlias { RoleId = roleId, Name = "RoleName" }
        });

        var result = _service.IsInRoleInCurrentYear(member, roleId);

        Assert.True(result);
    }

    [Fact]
    public void EnsureBoardMember_NotBoard_ThrowsUnauthorizedAccessException()
    {
        var memberId = Guid.NewGuid();
        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = "2" });
        _db.SaveChanges();

        Assert.Throws<UnauthorizedAccessException>(() => _service.EnsureBoardMember(memberId));
    }

    [Fact]
    public void EnsureBoardMember_IsBoard_DoesNotThrow()
    {
        var memberId = Guid.NewGuid();
        uint boardGroupId = 2;
        uint currentYear = YearUtils.GetBoardYear(_db);

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = "2" });
        _db.GroupMemberships.Add(new GroupMembership
        {
            MemberId = memberId,
            GroupId = boardGroupId,
            MembershipYear = currentYear
        });
        _db.SaveChanges();

        var exception = Record.Exception(() => _service.EnsureBoardMember(memberId));
        
        Assert.Null(exception);
    }

    [Fact]
    public void IsBoardOrCandidateBoardMember_CandidateBoard_ReturnsTrue()
    {
        var memberId = Guid.NewGuid();
        uint candidateBoardGroupId = 3;
        uint currentYear = YearUtils.GetBoardYear(_db);

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = "2" });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = "3" });
        _db.GroupMemberships.Add(new GroupMembership
        {
            MemberId = memberId,
            GroupId = candidateBoardGroupId,
            MembershipYear = currentYear
        });
        _db.SaveChanges();

        var result = _service.IsBoardOrCandidateBoardMember(memberId);

        Assert.True(result);
    }

    [Fact]
    public void EnsureBoardOrCandidateBoardMember_NotAuthorized_ThrowsUnauthorizedAccessException()
    {
        var memberId = Guid.NewGuid();
        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = "2" });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = "3" });
        _db.SaveChanges();

        Assert.Throws<UnauthorizedAccessException>(() => _service.EnsureBoardOrCandidateBoardMember(memberId));
    }

    [Fact]
    public void EnsureBoardOrCandidateBoardMember_IsAuthorized_DoesNotThrow()
    {
        var memberId = Guid.NewGuid();
        uint boardGroupId = 2;
        uint currentYear = YearUtils.GetBoardYear(_db);

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = "2" });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = "3" });
        _db.GroupMemberships.Add(new GroupMembership
        {
            MemberId = memberId,
            GroupId = boardGroupId,
            MembershipYear = currentYear
        });
        _db.SaveChanges();

        var exception = Record.Exception(() => _service.EnsureBoardOrCandidateBoardMember(memberId));
        
        Assert.Null(exception);
    }
}

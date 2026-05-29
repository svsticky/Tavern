using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Backend.Projections;
using Xunit;

namespace Backend.Tests.Projections;

public class AnnouncementProjectionsTests
{
    [Fact]
    public void ToDto_WhenUserIsBoard_ReturnsCreatedById()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var announcement = new Announcement
        {
            Id = 1,
            Title = "Welcome",
            Content = "Hello World",
            CreatedById = creatorId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = new Member
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com",
                StudentNumber = "s1234567",
                PhoneNumber = "+31612345678",
                Street = "Main Street",
                HouseNumber = "12",
                PostalCode = "7500AA",
                City = "Enschede"
            }
        };

        // Act
        var projectionFunc = AnnouncementProjections.ToDto(userId, isBoard: true).Compile();
        var dto = projectionFunc(announcement);

        // Assert
        Assert.Equal(1u, dto.Id);
        Assert.Equal("Welcome", dto.Title);
        Assert.Equal("Hello World", dto.Content);
        Assert.Equal(creatorId, dto.CreatedById);
        Assert.Equal("Jane Doe", dto.CreatedByName);
    }

    [Fact]
    public void ToDto_WhenUserIsNotBoardButIsCreator_ReturnsCreatedById()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var announcement = new Announcement
        {
            Id = 2,
            Title = "Welcome",
            Content = "Hello World",
            CreatedById = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null!
        };

        // Act
        var projectionFunc = AnnouncementProjections.ToDto(userId, isBoard: false).Compile();
        var dto = projectionFunc(announcement);

        // Assert
        Assert.Equal(userId, dto.CreatedById);
        Assert.Equal("Unknown", dto.CreatedByName);
    }

    [Fact]
    public void ToDto_WhenUserIsNotBoardAndNotCreator_ReturnsNullForCreatedById()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var announcement = new Announcement
        {
            Id = 3,
            Title = "Welcome",
            Content = "Hello World",
            CreatedById = creatorId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = null!
        };

        // Act
        var projectionFunc = AnnouncementProjections.ToDto(userId, isBoard: false).Compile();
        var dto = projectionFunc(announcement);

        // Assert
        Assert.Null(dto.CreatedById);
        Assert.Equal("Unknown", dto.CreatedByName);
    }
}

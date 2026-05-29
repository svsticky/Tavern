using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers;
using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Controllers;

public class AnnouncementsControllerTests
{
    private readonly IAnnouncementRepository _repositoryMock;
    private readonly AnnouncementsController _controller;
    private readonly Guid _userId;

    public AnnouncementsControllerTests()
    {
        _repositoryMock = Substitute.For<IAnnouncementRepository>();
        _controller = new AnnouncementsController(_repositoryMock);
        _userId = Guid.NewGuid();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("UserId", _userId.ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAnnouncements_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<GetAnnouncementResponseDTO>
        {
            new GetAnnouncementResponseDTO { Id = 1, Title = "Info", Content = "Info Content", CreatedByName = "John Doe", CreatedAt = DateTimeOffset.UtcNow }
        };
        _repositoryMock.GetAnnouncements(_userId, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetAnnouncements(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<GetAnnouncementResponseDTO>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetAnnouncements_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _repositoryMock.GetAnnouncements(_userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetAnnouncements(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetAnnouncements_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.GetAnnouncements(_userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetAnnouncements(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task GetAnnouncement_Found_ReturnsOk()
    {
        // Arrange
        var ann = new GetAnnouncementResponseDTO { Id = 2, Title = "Test", Content = "Test Content", CreatedByName = "John Doe", CreatedAt = DateTimeOffset.UtcNow };
        _repositoryMock.GetAnnouncement(2, _userId, Arg.Any<CancellationToken>()).Returns(ann);

        // Act
        var result = await _controller.GetAnnouncement(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<GetAnnouncementResponseDTO>(okResult.Value);
        Assert.Equal("Test", returned.Title);
    }

    [Fact]
    public async Task GetAnnouncement_NotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock.GetAnnouncement(3, _userId, Arg.Any<CancellationToken>()).Returns((GetAnnouncementResponseDTO?)null);

        // Act
        var result = await _controller.GetAnnouncement(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAnnouncement_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _repositoryMock.GetAnnouncement(3, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetAnnouncement(3, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetAnnouncement_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.GetAnnouncement(3, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetAnnouncement(3, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PostAnnouncement_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { Title = "Post", Content = "Content" };
        var created = new Announcement { Id = 10, Title = "Post", Content = "Content", CreatedAt = DateTimeOffset.UtcNow, CreatedById = _userId };
        _repositoryMock.CreateAnnouncement(_userId, dto, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostAnnouncement(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetAnnouncement), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostAnnouncement_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { Title = "Post", Content = "Content" };
        _repositoryMock.CreateAnnouncement(_userId, dto, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PostAnnouncement(dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task PostAnnouncement_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { Title = "Post", Content = "Content" };
        _repositoryMock.CreateAnnouncement(_userId, dto, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PostAnnouncement(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task DeleteAnnouncement_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteAnnouncement(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _repositoryMock.Received(1).DeleteAnnouncement(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAnnouncement_NotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock.DeleteAnnouncement(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.DeleteAnnouncement(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAnnouncement_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _repositoryMock.DeleteAnnouncement(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteAnnouncement(1, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteAnnouncement_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.DeleteAnnouncement(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.DeleteAnnouncement(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PatchAnnouncement_NullPatchDoc_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.PatchAnnouncement(1, null!, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task PatchAnnouncement_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>();

        // Act
        var result = await _controller.PatchAnnouncement(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _repositoryMock.Received(1).PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAnnouncement_NotFound_ReturnsNotFound()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>();
        _repositoryMock.PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.PatchAnnouncement(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PatchAnnouncement_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>();
        _repositoryMock.PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new ArgumentException());

        // Act
        var result = await _controller.PatchAnnouncement(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task PatchAnnouncement_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>();
        _repositoryMock.PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PatchAnnouncement(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PatchAnnouncement_Exception_ReturnsBadRequest()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>();
        _repositoryMock.PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PatchAnnouncement(1, patchDoc, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PutAnnouncement_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new UpdateAnnouncementDTO { Title = "Updated", Content = "Updated Content" };

        // Act
        var result = await _controller.PutAnnouncement(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _repositoryMock.Received(1).UpdateAnnouncement(1, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutAnnouncement_NotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateAnnouncementDTO { Title = "Updated", Content = "Updated Content" };
        _repositoryMock.UpdateAnnouncement(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.PutAnnouncement(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutAnnouncement_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new UpdateAnnouncementDTO { Title = "Updated", Content = "Updated Content" };
        _repositoryMock.UpdateAnnouncement(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PutAnnouncement(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PutAnnouncement_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UpdateAnnouncementDTO { Title = "Updated", Content = "Updated Content" };
        _repositoryMock.UpdateAnnouncement(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PutAnnouncement(1, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }
}

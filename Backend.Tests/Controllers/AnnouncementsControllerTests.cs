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
    private readonly IAnnouncementService _serviceMock;
    private readonly AnnouncementsController _controller;
    private readonly Guid _userId;

    public AnnouncementsControllerTests()
    {
        _serviceMock = Substitute.For<IAnnouncementService>();
        _controller = new AnnouncementsController(_serviceMock);
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
            new GetAnnouncementResponseDTO { Id = 1, TitleDutch = "Info NL", TitleEnglish = "Info EN", ContentDutch = "Inhoud NL", ContentEnglish = "Content EN", CreatedByName = "John Doe", CreatedAt = DateTimeOffset.UtcNow }
        };
        _serviceMock.GetAnnouncements(_userId, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetAnnouncements(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<GetAnnouncementResponseDTO>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetAnnouncements_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetAnnouncements(_userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetAnnouncements(CancellationToken.None));
    }

    [Fact]
    public async Task GetAnnouncements_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetAnnouncements(_userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetAnnouncements(CancellationToken.None));
    }

    [Fact]
    public async Task GetAnnouncement_Found_ReturnsOk()
    {
        // Arrange
        var ann = new GetAnnouncementResponseDTO { Id = 2, TitleDutch = "Test NL", TitleEnglish = "Test EN", ContentDutch = "Inhoud NL", ContentEnglish = "Content EN", CreatedByName = "John Doe", CreatedAt = DateTimeOffset.UtcNow };
        _serviceMock.GetAnnouncement(2, _userId, Arg.Any<CancellationToken>()).Returns(ann);

        // Act
        var result = await _controller.GetAnnouncement(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<GetAnnouncementResponseDTO>(okResult.Value);
        Assert.Equal("Test NL", returned.TitleDutch);
    }

    [Fact]
    public async Task GetAnnouncement_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetAnnouncement(3, _userId, Arg.Any<CancellationToken>()).Returns((GetAnnouncementResponseDTO?)null);

        // Act
        var result = await _controller.GetAnnouncement(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAnnouncement_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetAnnouncement(3, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetAnnouncement(3, CancellationToken.None));
    }

    [Fact]
    public async Task GetAnnouncement_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetAnnouncement(3, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetAnnouncement(3, CancellationToken.None));
    }

    [Fact]
    public async Task PostAnnouncement_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { TitleDutch = "Post NL", TitleEnglish = "Post EN", ContentDutch = "Inhoud NL", ContentEnglish = "Content EN" };
        var created = new Announcement { Id = 10, TitleDutch = "Post NL", TitleEnglish = "Post EN", ContentDutch = "Inhoud NL", ContentEnglish = "Content EN", CreatedAt = DateTimeOffset.UtcNow, CreatedById = _userId };
        _serviceMock.CreateAnnouncement(_userId, dto, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostAnnouncement(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetAnnouncement), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostAnnouncement_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { TitleDutch = "Post NL", TitleEnglish = "Post EN", ContentDutch = "Inhoud NL", ContentEnglish = "Content EN" };
        _serviceMock.CreateAnnouncement(_userId, dto, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostAnnouncement(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PostAnnouncement_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { TitleDutch = "Post NL", TitleEnglish = "Post EN", ContentDutch = "Inhoud NL", ContentEnglish = "Content EN" };
        _serviceMock.CreateAnnouncement(_userId, dto, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostAnnouncement(dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAnnouncement_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteAnnouncement(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteAnnouncement(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAnnouncement_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteAnnouncement(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteAnnouncement(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAnnouncement_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteAnnouncement(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteAnnouncement(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAnnouncement_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteAnnouncement(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteAnnouncement(1, CancellationToken.None));
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
        await _serviceMock.Received(1).PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAnnouncement_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>();
        _serviceMock.PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PatchAnnouncement(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchAnnouncement_ArgumentException_ThrowsArgumentException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>();
        _serviceMock.PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new ArgumentException());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _controller.PatchAnnouncement(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchAnnouncement_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>();
        _serviceMock.PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PatchAnnouncement(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchAnnouncement_Exception_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>();
        _serviceMock.PatchAnnouncement(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchAnnouncement(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PutAnnouncement_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new UpdateAnnouncementDTO { TitleDutch = "Updated NL", TitleEnglish = "Updated EN", ContentDutch = "Updated Content NL", ContentEnglish = "Updated Content EN" };

        // Act
        var result = await _controller.PutAnnouncement(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateAnnouncement(1, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutAnnouncement_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new UpdateAnnouncementDTO { TitleDutch = "Updated NL", TitleEnglish = "Updated EN", ContentDutch = "Updated Content NL", ContentEnglish = "Updated Content EN" };
        _serviceMock.UpdateAnnouncement(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutAnnouncement(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutAnnouncement_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new UpdateAnnouncementDTO { TitleDutch = "Updated NL", TitleEnglish = "Updated EN", ContentDutch = "Updated Content NL", ContentEnglish = "Updated Content EN" };
        _serviceMock.UpdateAnnouncement(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutAnnouncement(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutAnnouncement_Exception_ThrowsException()
    {
        // Arrange
        var dto = new UpdateAnnouncementDTO { TitleDutch = "Updated NL", TitleEnglish = "Updated EN", ContentDutch = "Updated Content NL", ContentEnglish = "Updated Content EN" };
        _serviceMock.UpdateAnnouncement(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutAnnouncement(1, dto, CancellationToken.None));
    }
}

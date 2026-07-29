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

public class MailinglistsControllerTests
{
    private readonly IMailinglistService _serviceMock;
    private readonly Mailinglists _controller;
    private readonly Guid _userId;

    public MailinglistsControllerTests()
    {
        _serviceMock = Substitute.For<IMailinglistService>();
        _controller = new Mailinglists(_serviceMock);
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
    public async Task GetMailinglists_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<Mailinglist> { new Mailinglist { Id = 1, Name = "Newsletter", BitValue = 1 } };
        _serviceMock.GetMailinglists(Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetMailinglists(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<Mailinglist>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetMailinglists_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetMailinglists(Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetMailinglists(CancellationToken.None));
    }

    [Fact]
    public async Task GetMailinglists_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetMailinglists(Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetMailinglists(CancellationToken.None));
    }

    [Fact]
    public async Task GetMailinglist_Found_ReturnsOk()
    {
        // Arrange
        var mailinglist = new Mailinglist { Id = 2, Name = "Announcements", BitValue = 2 };
        _serviceMock.GetMailinglist(2, Arg.Any<CancellationToken>()).Returns(mailinglist);

        // Act
        var result = await _controller.GetMailinglist(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<Mailinglist>(okResult.Value);
        Assert.Equal("Announcements", returned.Name);
    }

    [Fact]
    public async Task GetMailinglist_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetMailinglist(3, Arg.Any<CancellationToken>()).Returns((Mailinglist?)null);

        // Act
        var result = await _controller.GetMailinglist(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetMailinglist_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetMailinglist(3, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetMailinglist(3, CancellationToken.None));
    }

    [Fact]
    public async Task GetMailinglist_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetMailinglist(3, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetMailinglist(3, CancellationToken.None));
    }

    [Fact]
    public async Task PostMailinglist_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "NewList", ServiceId = "service_1" };
        var created = new Mailinglist { Id = 10, Name = "NewList", BitValue = 4 };
        _serviceMock.CreateMailinglist(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostMailinglist(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetMailinglist), createdResult.ActionName);
        Assert.Equal(10, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostMailinglist_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "NewList", ServiceId = "service_1" };
        _serviceMock.CreateMailinglist(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostMailinglist(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PostMailinglist_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "NewList", ServiceId = "service_1" };
        _serviceMock.CreateMailinglist(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostMailinglist(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutMailinglist_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "Updated", ServiceId = "service_1" };

        // Act
        var result = await _controller.PutMailinglist(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateMailinglist(1, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutMailinglist_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "Updated", ServiceId = "service_1" };
        _serviceMock.UpdateMailinglist(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutMailinglist(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutMailinglist_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "Updated", ServiceId = "service_1" };
        _serviceMock.UpdateMailinglist(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutMailinglist(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutMailinglist_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostMailinglistDTO { Name = "Updated", ServiceId = "service_1" };
        _serviceMock.UpdateMailinglist(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutMailinglist(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PatchMailinglist_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Mailinglist>();

        // Act
        var result = await _controller.PatchMailinglist(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).PatchMailinglist(1, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchMailinglist_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Mailinglist>();
        _serviceMock.PatchMailinglist(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PatchMailinglist(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchMailinglist_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Mailinglist>();
        _serviceMock.PatchMailinglist(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PatchMailinglist(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchMailinglist_Exception_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Mailinglist>();
        _serviceMock.PatchMailinglist(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchMailinglist(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMailinglist_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteMailinglist(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteMailinglist(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMailinglist_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteMailinglist(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteMailinglist(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMailinglist_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteMailinglist(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteMailinglist(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMailinglist_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteMailinglist(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteMailinglist(1, CancellationToken.None));
    }
}

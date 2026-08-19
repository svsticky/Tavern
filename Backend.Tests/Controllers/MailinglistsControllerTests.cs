using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers;
using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Controllers;

public class MailinglistsControllerTests
{
    private readonly IMailSubscriptionService _mailSubscriptionServiceMock;
    private readonly IMailinglistCurationService _curationServiceMock;
    private readonly Mailinglists _controller;
    private readonly Guid _userId;

    public MailinglistsControllerTests()
    {
        _mailSubscriptionServiceMock = Substitute.For<IMailSubscriptionService>();
        _curationServiceMock = Substitute.For<IMailinglistCurationService>();
        _controller = new Mailinglists(_mailSubscriptionServiceMock, _curationServiceMock);
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
    public async Task GetMailinglists_Success_ReturnsOnlyGeneralVisibleLists()
    {
        // Arrange
        var providerLists = new List<MailinglistDto> { new("id_news", "Newsletter"), new("id_alumni", "Alumni") };
        _mailSubscriptionServiceMock.GetAvailableMailinglistsAsync(Arg.Any<CancellationToken>()).Returns(providerLists);
        _curationServiceMock.GetVisibleProviderListIds(false, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "id_news" });

        // Act
        var result = await _controller.GetMailinglists(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<MailinglistDto>>(okResult.Value);
        var single = Assert.Single(returnedList);
        Assert.Equal("id_news", single.Id);
    }

    [Fact]
    public async Task GetMailinglists_Exception_ThrowsException()
    {
        // Arrange
        _curationServiceMock.GetVisibleProviderListIds(false, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        _mailSubscriptionServiceMock.GetAvailableMailinglistsAsync(Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetMailinglists(CancellationToken.None));
    }

    [Fact]
    public async Task GetAddableMailinglists_Success_ReturnsOk()
    {
        // Arrange
        var addable = new List<MailinglistDto> { new("id_new", "New list") };
        _curationServiceMock.GetAddableProviderMailinglists(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(addable);

        // Act
        var result = await _controller.GetAddableMailinglists(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<MailinglistDto>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetAddableMailinglists_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _curationServiceMock.GetAddableProviderMailinglists(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetAddableMailinglists(CancellationToken.None));
    }

    [Fact]
    public async Task GetCuratedMailinglists_Success_ReturnsOk()
    {
        // Arrange
        var curated = new List<CuratedMailinglistDto> { new(1, "id_news", "Newsletter", MailinglistVisibility.General, false) };
        _curationServiceMock.GetCuratedMailinglists(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(curated);

        // Act
        var result = await _controller.GetCuratedMailinglists(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<CuratedMailinglistDto>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetCuratedMailinglists_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _curationServiceMock.GetCuratedMailinglists(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetCuratedMailinglists(CancellationToken.None));
    }

    [Fact]
    public async Task PostMailinglist_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostCuratedMailinglistDTO { ProviderListId = "id_news", Visibility = MailinglistVisibility.General };
        var created = new CuratedMailinglistDto(1, "id_news", "Newsletter", MailinglistVisibility.General, false);
        _curationServiceMock.AddMailinglist(dto.ProviderListId, dto.Visibility, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostMailinglist(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostMailinglist_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostCuratedMailinglistDTO { ProviderListId = "id_news", Visibility = MailinglistVisibility.General };
        _curationServiceMock.AddMailinglist(dto.ProviderListId, dto.Visibility, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostMailinglist(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PatchMailinglist_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new PatchCuratedMailinglistDTO { Visibility = MailinglistVisibility.YearlyRenewalOnly };

        // Act
        var result = await _controller.PatchMailinglist(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _curationServiceMock.Received(1).UpdateMailinglistVisibility(1, MailinglistVisibility.YearlyRenewalOnly, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchMailinglist_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new PatchCuratedMailinglistDTO { Visibility = MailinglistVisibility.General };
        _curationServiceMock.UpdateMailinglistVisibility(1, dto.Visibility, _userId, Arg.Any<CancellationToken>())
            .Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PatchMailinglist(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMailinglist_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteMailinglist(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _curationServiceMock.Received(1).DeleteMailinglist(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMailinglist_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _curationServiceMock.DeleteMailinglist(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteMailinglist(1, CancellationToken.None));
    }
}

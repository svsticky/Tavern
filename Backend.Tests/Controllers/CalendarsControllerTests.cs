using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers;
using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Controllers;

public class CalendarsControllerTests
{
    private readonly ICalendarService _serviceMock;
    private readonly CalendarsController _controller;
    private readonly Guid _userId;

    public CalendarsControllerTests()
    {
        _serviceMock = Substitute.For<ICalendarService>();
        _controller = new CalendarsController(_serviceMock);
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
    public async Task GetCalendar_KnownCalendarId_ReturnsCalendarContent()
    {
        var calendarId = Guid.NewGuid();
        const string ics = "BEGIN:VCALENDAR\r\nEND:VCALENDAR";
        _serviceMock.GetPersonalCalendar(calendarId, Arg.Any<CancellationToken>()).Returns(ics);

        var result = await _controller.GetCalendar(calendarId, CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(ics, content.Content);
        Assert.Equal("text/calendar; charset=utf-8", content.ContentType);
    }

    [Fact]
    public async Task GetCalendar_ServedInline_HasNoContentDispositionHeader()
    {
        // The feed is a live subscription URL rather than a downloadable file, so it must not be
        // presented to the client as an attachment.
        var calendarId = Guid.NewGuid();
        _serviceMock.GetPersonalCalendar(calendarId, Arg.Any<CancellationToken>())
            .Returns("BEGIN:VCALENDAR\r\nEND:VCALENDAR");

        await _controller.GetCalendar(calendarId, CancellationToken.None);

        Assert.False(_controller.Response.Headers.ContainsKey("Content-Disposition"));
    }

    [Fact]
    public async Task GetCalendar_UnknownCalendarId_ReturnsNotFound()
    {
        var calendarId = Guid.NewGuid();
        _serviceMock.GetPersonalCalendar(calendarId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await _controller.GetCalendar(calendarId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetOwnFeedUrl_ReturnsUrlForTheAuthenticatedMember()
    {
        var dto = new CalendarFeedUrlDTO { Url = "https://api.tavern.svsticky.nl/calendars/abc" };
        _serviceMock.GetOwnFeedUrl(_userId, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _controller.GetOwnFeedUrl(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
        await _serviceMock.Received(1).GetOwnFeedUrl(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateOwnFeedUrl_ReturnsTheNewUrlForTheAuthenticatedMember()
    {
        var dto = new CalendarFeedUrlDTO { Url = "https://api.tavern.svsticky.nl/calendars/def" };
        _serviceMock.RotateOwnCalendarId(_userId, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _controller.RotateOwnFeedUrl(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
        await _serviceMock.Received(1).RotateOwnCalendarId(_userId, Arg.Any<CancellationToken>());
    }
}

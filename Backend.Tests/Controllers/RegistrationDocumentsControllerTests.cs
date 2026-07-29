using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers;
using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Controllers;

public class RegistrationDocumentsControllerTests
{
    private readonly IRegistrationDocumentService _serviceMock;
    private readonly RegistrationDocumentsController _controller;
    private readonly Guid _userId;

    public RegistrationDocumentsControllerTests()
    {
        _serviceMock = Substitute.For<IRegistrationDocumentService>();
        _controller = new RegistrationDocumentsController(_serviceMock);
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
    public async Task GetRegistrationDocuments_Success_ReturnsOk()
    {
        var list = new List<RegistrationDocumentResponseDTO>
        {
            new RegistrationDocumentResponseDTO
            {
                Id = 1,
                NameDutch = "Privacy",
                NameEnglish = "Privacy",
                Url = "https://example.com",
                SortOrder = 1
            }
        };
        _serviceMock.GetRegistrationDocuments(Arg.Any<CancellationToken>()).Returns(list);

        var result = await _controller.GetRegistrationDocuments(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<RegistrationDocumentResponseDTO>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetRegistrationDocument_Found_ReturnsOk()
    {
        var doc = new RegistrationDocumentResponseDTO
        {
            Id = 1,
            NameDutch = "Privacy",
            NameEnglish = "Privacy",
            Url = "https://example.com",
            SortOrder = 1
        };
        _serviceMock.GetRegistrationDocument(1, Arg.Any<CancellationToken>()).Returns(doc);

        var result = await _controller.GetRegistrationDocument(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<RegistrationDocumentResponseDTO>(okResult.Value);
        Assert.Equal(1, returned.Id);
    }

    [Fact]
    public async Task GetRegistrationDocument_NotFound_ReturnsNotFound()
    {
        _serviceMock.GetRegistrationDocument(1, Arg.Any<CancellationToken>()).Returns((RegistrationDocumentResponseDTO?)null);

        var result = await _controller.GetRegistrationDocument(1, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PostRegistrationDocument_Success_ReturnsCreated()
    {
        var dto = new PostRegistrationDocumentDTO
        {
            NameDutch = "Privacy",
            NameEnglish = "Privacy",
            Url = "https://example.com",
            SortOrder = 1
        };
        var created = new RegistrationDocumentResponseDTO
        {
            Id = 10,
            NameDutch = "Privacy",
            NameEnglish = "Privacy",
            Url = "https://example.com",
            SortOrder = 1
        };
        _serviceMock.CreateRegistrationDocument(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        var result = await _controller.PostRegistrationDocument(dto, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(10, ((RegistrationDocumentResponseDTO)createdResult.Value!).Id);
    }

    [Fact]
    public async Task PutRegistrationDocument_Success_ReturnsNoContent()
    {
        var dto = new RegistrationDocumentUpdateDTO
        {
            NameDutch = "Privacy NL",
            NameEnglish = "Privacy EN",
            Url = "https://example.com",
            SortOrder = 1
        };

        var result = await _controller.PutRegistrationDocument(1, dto, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateRegistrationDocument(1, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRegistrationDocument_Success_ReturnsNoContent()
    {
        var result = await _controller.DeleteRegistrationDocument(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteRegistrationDocument(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRegistrationDocuments_Exception_ThrowsException()
    {
        _serviceMock.GetRegistrationDocuments(Arg.Any<CancellationToken>()).Throws(new Exception("Database error"));

        await Assert.ThrowsAsync<Exception>(() => _controller.GetRegistrationDocuments(CancellationToken.None));
    }

    [Fact]
    public async Task GetRegistrationDocument_Exception_ThrowsException()
    {
        _serviceMock.GetRegistrationDocument(1, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        await Assert.ThrowsAsync<Exception>(() => _controller.GetRegistrationDocument(1, CancellationToken.None));
    }

    [Fact]
    public async Task PostRegistrationDocument_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        var dto = new PostRegistrationDocumentDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _serviceMock.CreateRegistrationDocument(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostRegistrationDocument(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PostRegistrationDocument_GenericException_ThrowsException()
    {
        var dto = new PostRegistrationDocumentDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _serviceMock.CreateRegistrationDocument(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Fail"));

        await Assert.ThrowsAsync<Exception>(() => _controller.PostRegistrationDocument(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutRegistrationDocument_NotFound_ThrowsKeyNotFoundException()
    {
        var dto = new RegistrationDocumentUpdateDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _serviceMock.UpdateRegistrationDocument(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutRegistrationDocument(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutRegistrationDocument_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        var dto = new RegistrationDocumentUpdateDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _serviceMock.UpdateRegistrationDocument(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutRegistrationDocument(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutRegistrationDocument_GenericException_ThrowsException()
    {
        var dto = new RegistrationDocumentUpdateDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _serviceMock.UpdateRegistrationDocument(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        await Assert.ThrowsAsync<Exception>(() => _controller.PutRegistrationDocument(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRegistrationDocument_NotFound_ThrowsKeyNotFoundException()
    {
        _serviceMock.DeleteRegistrationDocument(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteRegistrationDocument(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRegistrationDocument_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        _serviceMock.DeleteRegistrationDocument(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteRegistrationDocument(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRegistrationDocument_GenericException_ThrowsException()
    {
        _serviceMock.DeleteRegistrationDocument(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteRegistrationDocument(1, CancellationToken.None));
    }

    [Fact]
    public async Task GetUserId_Unauthenticated_ThrowsUnauthorizedAccessException()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var dto = new PostRegistrationDocumentDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostRegistrationDocument(dto, CancellationToken.None));
    }
}

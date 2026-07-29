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
    private readonly IRegistrationDocumentRepository _repositoryMock;
    private readonly RegistrationDocumentsController _controller;
    private readonly Guid _userId;

    public RegistrationDocumentsControllerTests()
    {
        _repositoryMock = Substitute.For<IRegistrationDocumentRepository>();
        _controller = new RegistrationDocumentsController(_repositoryMock);
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
        _repositoryMock.GetRegistrationDocuments(Arg.Any<CancellationToken>()).Returns(list);

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
        _repositoryMock.GetRegistrationDocument(1, Arg.Any<CancellationToken>()).Returns(doc);

        var result = await _controller.GetRegistrationDocument(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<RegistrationDocumentResponseDTO>(okResult.Value);
        Assert.Equal(1, returned.Id);
    }

    [Fact]
    public async Task GetRegistrationDocument_NotFound_ReturnsNotFound()
    {
        _repositoryMock.GetRegistrationDocument(1, Arg.Any<CancellationToken>()).Returns((RegistrationDocumentResponseDTO?)null);

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
        _repositoryMock.CreateRegistrationDocument(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

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
        await _repositoryMock.Received(1).UpdateRegistrationDocument(1, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRegistrationDocument_Success_ReturnsNoContent()
    {
        var result = await _controller.DeleteRegistrationDocument(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        await _repositoryMock.Received(1).DeleteRegistrationDocument(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRegistrationDocuments_Exception_ReturnsBadRequest()
    {
        _repositoryMock.GetRegistrationDocuments(Arg.Any<CancellationToken>()).Throws(new Exception("Database error"));

        var result = await _controller.GetRegistrationDocuments(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var err = Assert.IsType<ErrorResponseDto>(badRequest.Value);
        Assert.Equal("Database error", err.Message);
    }

    [Fact]
    public async Task GetRegistrationDocument_Exception_ReturnsBadRequest()
    {
        _repositoryMock.GetRegistrationDocument(1, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        var result = await _controller.GetRegistrationDocument(1, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var err = Assert.IsType<ErrorResponseDto>(badRequest.Value);
        Assert.Equal("Error", err.Message);
    }

    [Fact]
    public async Task PostRegistrationDocument_Unauthorized_ReturnsForbid()
    {
        var dto = new PostRegistrationDocumentDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _repositoryMock.CreateRegistrationDocument(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        var result = await _controller.PostRegistrationDocument(dto, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task PostRegistrationDocument_GenericException_ReturnsBadRequest()
    {
        var dto = new PostRegistrationDocumentDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _repositoryMock.CreateRegistrationDocument(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Fail"));

        var result = await _controller.PostRegistrationDocument(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PutRegistrationDocument_NotFound_ReturnsNotFound()
    {
        var dto = new RegistrationDocumentUpdateDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _repositoryMock.UpdateRegistrationDocument(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        var result = await _controller.PutRegistrationDocument(1, dto, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutRegistrationDocument_Unauthorized_ReturnsForbid()
    {
        var dto = new RegistrationDocumentUpdateDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _repositoryMock.UpdateRegistrationDocument(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        var result = await _controller.PutRegistrationDocument(1, dto, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PutRegistrationDocument_GenericException_ReturnsBadRequest()
    {
        var dto = new RegistrationDocumentUpdateDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        _repositoryMock.UpdateRegistrationDocument(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        var result = await _controller.PutRegistrationDocument(1, dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRegistrationDocument_NotFound_ReturnsNotFound()
    {
        _repositoryMock.DeleteRegistrationDocument(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        var result = await _controller.DeleteRegistrationDocument(1, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteRegistrationDocument_Unauthorized_ReturnsForbid()
    {
        _repositoryMock.DeleteRegistrationDocument(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        var result = await _controller.DeleteRegistrationDocument(1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteRegistrationDocument_GenericException_ReturnsBadRequest()
    {
        _repositoryMock.DeleteRegistrationDocument(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        var result = await _controller.DeleteRegistrationDocument(1, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetUserId_Unauthenticated_ThrowsUnauthorizedAccessException()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var dto = new PostRegistrationDocumentDTO { NameDutch = "A", NameEnglish = "B", Url = "http://a.b", SortOrder = 1 };
        var result = await _controller.PostRegistrationDocument(dto, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }
}

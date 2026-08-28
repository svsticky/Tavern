using System;
using System.Collections.Generic;
using System.IO;
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

public class ExternalLinksControllerTests
{
    private readonly IExternalLinkService _serviceMock;
    private readonly ExternalLinksController _controller;
    private readonly Guid _userId;

    public ExternalLinksControllerTests()
    {
        _serviceMock = Substitute.For<IExternalLinkService>();
        _controller = new ExternalLinksController(_serviceMock);
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
    public async Task GetExternalLinks_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<ExternalLinkResponseDTO>
        {
            new ExternalLinkResponseDTO
            {
                Id = 1,
                TitleDutch = "NL Title",
                TitleEnglish = "EN Title",
                DescriptionDutch = "NL Desc",
                DescriptionEnglish = "EN Desc",
                Url = "https://google.com",
                SortOrder = 1
            }
        };
        _serviceMock.GetExternalLinks(Arg.Any<CancellationToken>())
            .Returns(list);

        // Act
        var result = await _controller.GetExternalLinks(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<ExternalLinkResponseDTO>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetExternalLinks_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.GetExternalLinks(Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetExternalLinks(CancellationToken.None));
    }

    [Fact]
    public async Task GetExternalLink_Success_ReturnsOk()
    {
        // Arrange
        var link = new ExternalLinkResponseDTO
        {
            Id = 5,
            TitleDutch = "NL Title",
            TitleEnglish = "EN Title",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://yahoo.com",
            SortOrder = 2
        };
        _serviceMock.GetExternalLink(5, Arg.Any<CancellationToken>())
            .Returns(link);

        // Act
        var result = await _controller.GetExternalLink(5, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLink = Assert.IsType<ExternalLinkResponseDTO>(okResult.Value);
        Assert.Equal(5, returnedLink.Id);
    }

    [Fact]
    public async Task GetExternalLink_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetExternalLink(99, Arg.Any<CancellationToken>())
            .Returns((ExternalLinkResponseDTO?)null);

        // Act
        var result = await _controller.GetExternalLink(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetExternalLink_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.GetExternalLink(5, Arg.Any<CancellationToken>())
            .Throws(new Exception("Error retrieving"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetExternalLink(5, CancellationToken.None));
    }

    [Fact]
    public async Task PostExternalLink_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostExternalLinkDTO
        {
            TitleDutch = "NL Title",
            TitleEnglish = "EN Title",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://bing.com",
            SortOrder = 3
        };
        var created = new ExternalLinkResponseDTO
        {
            Id = 10,
            TitleDutch = "NL Title",
            TitleEnglish = "EN Title",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://bing.com",
            SortOrder = 3
        };
        _serviceMock.CreateExternalLink(dto, _userId, Arg.Any<CancellationToken>())
            .Returns(created);

        // Act
        var result = await _controller.PostExternalLink(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetExternalLink", createdResult.ActionName);
        Assert.Equal(10, createdResult.RouteValues?["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostExternalLink_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostExternalLinkDTO
        {
            TitleDutch = "NL Title",
            TitleEnglish = "EN Title",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://bing.com",
            SortOrder = 3
        };
        _serviceMock.CreateExternalLink(dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostExternalLink(dto, CancellationToken.None));
}

    [Fact]
    public async Task PostExternalLink_Error_ThrowsException()
    {
        // Arrange
        var dto = new PostExternalLinkDTO
        {
            TitleDutch = "NL Title",
            TitleEnglish = "EN Title",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://bing.com",
            SortOrder = 3
        };
        _serviceMock.CreateExternalLink(dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Fail"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostExternalLink(dto, CancellationToken.None));
}

    [Fact]
    public async Task PutExternalLink_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new ExternalLinkUpdateDTO
        {
            TitleDutch = "NL Title",
            TitleEnglish = "EN Title",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://bing.com",
            SortOrder = 4
        };
        _serviceMock.UpdateExternalLink(1, dto, _userId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PutExternalLink(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task PutExternalLink_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new ExternalLinkUpdateDTO
        {
            TitleDutch = "NL Title",
            TitleEnglish = "EN Title",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://bing.com",
            SortOrder = 4
        };
        _serviceMock.UpdateExternalLink(1, dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutExternalLink(1, dto, CancellationToken.None));
}

    [Fact]
    public async Task PutExternalLink_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new ExternalLinkUpdateDTO
        {
            TitleDutch = "NL Title",
            TitleEnglish = "EN Title",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://bing.com",
            SortOrder = 4
        };
        _serviceMock.UpdateExternalLink(1, dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutExternalLink(1, dto, CancellationToken.None));
}

    [Fact]
    public async Task PutExternalLink_Error_ThrowsException()
    {
        // Arrange
        var dto = new ExternalLinkUpdateDTO
        {
            TitleDutch = "NL Title",
            TitleEnglish = "EN Title",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://bing.com",
            SortOrder = 4
        };
        _serviceMock.UpdateExternalLink(1, dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Put error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutExternalLink(1, dto, CancellationToken.None));
}

    [Fact]
    public async Task DeleteExternalLink_Success_ReturnsNoContent()
    {
        // Arrange
        _serviceMock.DeleteExternalLink(1, _userId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteExternalLink(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteExternalLink_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteExternalLink(1, _userId, Arg.Any<CancellationToken>())
            .Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteExternalLink(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteExternalLink_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteExternalLink(1, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteExternalLink(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteExternalLink_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteExternalLink(1, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Delete error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteExternalLink(1, CancellationToken.None));
    }

    [Fact]
    public async Task UploadIcon_Success_ReturnsOk()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadExternalLinkIcon(1, _userId, fileMock)
            .Returns(Task.FromResult<string?>("icons/icon.png"));

        // Act
        var result = await _controller.UploadIcon(1, fileMock);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UploadPictureResponse>(okResult.Value);
        Assert.Equal("icons/icon.png", response.Path);
    }

    [Fact]
    public async Task UploadIcon_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadExternalLinkIcon(1, _userId, fileMock)
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.UploadIcon(1, fileMock));
    }

    [Fact]
    public async Task UploadIcon_Error_ThrowsException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadExternalLinkIcon(1, _userId, fileMock)
            .Throws(new Exception("Upload error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.UploadIcon(1, fileMock));
    }

    [Fact]
    public async Task GetIcon_NotFoundLinkOrIconPath_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetExternalLink(1, Arg.Any<CancellationToken>())
            .Returns((ExternalLinkResponseDTO?)null);

        // Act
        var result = await _controller.GetIcon(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetIcon_NotFoundIconFile_ReturnsNotFound()
    {
        // Arrange
        var link = new ExternalLinkResponseDTO
        {
            Id = 1,
            TitleDutch = "NL",
            TitleEnglish = "EN",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://google.com",
            SortOrder = 1,
            IconPath = "icons/path.png"
        };
        _serviceMock.GetExternalLink(1, Arg.Any<CancellationToken>())
            .Returns(link);
        _serviceMock.GetExternalLinkIconFile("icons/path.png")
            .Returns(Task.FromResult<FileResultDto?>(null));

        // Act
        var result = await _controller.GetIcon(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetIcon_Success_ReturnsFile()
    {
        // Arrange
        var link = new ExternalLinkResponseDTO
        {
            Id = 1,
            TitleDutch = "NL",
            TitleEnglish = "EN",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            Url = "https://google.com",
            SortOrder = 1,
            IconPath = "icons/path.png"
        };
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileResult = new FileResultDto { Stream = fileStream, ContentType = "image/png" };

        _serviceMock.GetExternalLink(1, Arg.Any<CancellationToken>())
            .Returns(link);
        _serviceMock.GetExternalLinkIconFile("icons/path.png")
            .Returns(fileResult);

        // Act
        var result = await _controller.GetIcon(1, CancellationToken.None);

        // Assert
        var fileResultVal = Assert.IsType<FileStreamResult>(result.Result);
        Assert.Equal("image/png", fileResultVal.ContentType);
        Assert.Equal(fileStream, fileResultVal.FileStream);
    }

    [Fact]
    public async Task GetIcon_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.GetExternalLink(1, Arg.Any<CancellationToken>())
            .Throws(new Exception("Read error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetIcon(1, CancellationToken.None));
    }
}

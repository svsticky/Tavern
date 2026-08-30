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

public class RegisterReasonsControllerTests
{
    private readonly IRegisterReasonService _serviceMock;
    private readonly RegisterReasonsController _controller;
    private readonly Guid _userId;

    public RegisterReasonsControllerTests()
    {
        _serviceMock = Substitute.For<IRegisterReasonService>();
        _controller = new RegisterReasonsController(_serviceMock);
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
    public async Task GetRegisterReasons_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<RegisterReasonResponseDTO>
        {
            new RegisterReasonResponseDTO
            {
                Id = 1,
                TitleDutch = "Reden",
                TitleEnglish = "Reason",
                DescriptionDutch = "NL Desc",
                DescriptionEnglish = "EN Desc",
                SortOrder = 1
            }
        };
        _serviceMock.GetRegisterReasons(Arg.Any<CancellationToken>())
            .Returns(list);

        // Act
        var result = await _controller.GetRegisterReasons(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<RegisterReasonResponseDTO>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetRegisterReasons_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.GetRegisterReasons(Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetRegisterReasons(CancellationToken.None));
    }

    [Fact]
    public async Task GetRegisterReason_Success_ReturnsOk()
    {
        // Arrange
        var reason = new RegisterReasonResponseDTO
        {
            Id = 5,
            TitleDutch = "Reden 5",
            TitleEnglish = "Reason 5",
            DescriptionDutch = "NL Desc 5",
            DescriptionEnglish = "EN Desc 5",
            SortOrder = 5
        };
        _serviceMock.GetRegisterReason(5, Arg.Any<CancellationToken>())
            .Returns(reason);

        // Act
        var result = await _controller.GetRegisterReason(5, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedReason = Assert.IsType<RegisterReasonResponseDTO>(okResult.Value);
        Assert.Equal(5, returnedReason.Id);
    }

    [Fact]
    public async Task GetRegisterReason_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetRegisterReason(99, Arg.Any<CancellationToken>())
            .Returns((RegisterReasonResponseDTO?)null);

        // Act
        var result = await _controller.GetRegisterReason(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRegisterReason_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.GetRegisterReason(5, Arg.Any<CancellationToken>())
            .Throws(new Exception("Error retrieving"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetRegisterReason(5, CancellationToken.None));
    }

    [Fact]
    public async Task PostRegisterReason_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostRegisterReasonDTO
        {
            TitleDutch = "Reden",
            TitleEnglish = "Reason",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 1
        };
        var created = new RegisterReasonResponseDTO
        {
            Id = 10,
            TitleDutch = "Reden",
            TitleEnglish = "Reason",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 1
        };
        _serviceMock.CreateRegisterReason(dto, _userId, Arg.Any<CancellationToken>())
            .Returns(created);

        // Act
        var result = await _controller.PostRegisterReason(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetRegisterReason", createdResult.ActionName);
        Assert.Equal(10, createdResult.RouteValues?["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostRegisterReason_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostRegisterReasonDTO
        {
            TitleDutch = "Reden",
            TitleEnglish = "Reason",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 1
        };
        _serviceMock.CreateRegisterReason(dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostRegisterReason(dto, CancellationToken.None));
}

    [Fact]
    public async Task PostRegisterReason_Error_ThrowsException()
    {
        // Arrange
        var dto = new PostRegisterReasonDTO
        {
            TitleDutch = "Reden",
            TitleEnglish = "Reason",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 1
        };
        _serviceMock.CreateRegisterReason(dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Fail"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostRegisterReason(dto, CancellationToken.None));
}

    [Fact]
    public async Task PutRegisterReason_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new RegisterReasonUpdateDTO
        {
            TitleDutch = "Reden",
            TitleEnglish = "Reason",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 2
        };
        _serviceMock.UpdateRegisterReason(1, dto, _userId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PutRegisterReason(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task PutRegisterReason_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new RegisterReasonUpdateDTO
        {
            TitleDutch = "Reden",
            TitleEnglish = "Reason",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 2
        };
        _serviceMock.UpdateRegisterReason(1, dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutRegisterReason(1, dto, CancellationToken.None));
}

    [Fact]
    public async Task PutRegisterReason_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new RegisterReasonUpdateDTO
        {
            TitleDutch = "Reden",
            TitleEnglish = "Reason",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 2
        };
        _serviceMock.UpdateRegisterReason(1, dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutRegisterReason(1, dto, CancellationToken.None));
}

    [Fact]
    public async Task PutRegisterReason_Error_ThrowsException()
    {
        // Arrange
        var dto = new RegisterReasonUpdateDTO
        {
            TitleDutch = "Reden",
            TitleEnglish = "Reason",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 2
        };
        _serviceMock.UpdateRegisterReason(1, dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Put error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutRegisterReason(1, dto, CancellationToken.None));
}

    [Fact]
    public async Task DeleteRegisterReason_Success_ReturnsNoContent()
    {
        // Arrange
        _serviceMock.DeleteRegisterReason(1, _userId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteRegisterReason(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteRegisterReason_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteRegisterReason(1, _userId, Arg.Any<CancellationToken>())
            .Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteRegisterReason(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRegisterReason_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteRegisterReason(1, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteRegisterReason(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRegisterReason_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteRegisterReason(1, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Delete error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteRegisterReason(1, CancellationToken.None));
    }

    [Fact]
    public async Task UploadIcon_Success_ReturnsOk()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadRegisterReasonIcon(1, _userId, fileMock)
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
        _serviceMock.UploadRegisterReasonIcon(1, _userId, fileMock)
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.UploadIcon(1, fileMock));
    }

    [Fact]
    public async Task UploadIcon_Error_ThrowsException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadRegisterReasonIcon(1, _userId, fileMock)
            .Throws(new Exception("Upload error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.UploadIcon(1, fileMock));
    }

    [Fact]
    public async Task GetIcon_NotFoundLinkOrIconPath_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetRegisterReason(1, Arg.Any<CancellationToken>())
            .Returns((RegisterReasonResponseDTO?)null);

        // Act
        var result = await _controller.GetIcon(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetIcon_NotFoundIconFile_ReturnsNotFound()
    {
        // Arrange
        var reason = new RegisterReasonResponseDTO
        {
            Id = 1,
            TitleDutch = "NL",
            TitleEnglish = "EN",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 1,
            IconPath = "icons/path.png"
        };
        _serviceMock.GetRegisterReason(1, Arg.Any<CancellationToken>())
            .Returns(reason);
        _serviceMock.GetRegisterReasonIconFile("icons/path.png")
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
        var reason = new RegisterReasonResponseDTO
        {
            Id = 1,
            TitleDutch = "NL",
            TitleEnglish = "EN",
            DescriptionDutch = "NL Desc",
            DescriptionEnglish = "EN Desc",
            SortOrder = 1,
            IconPath = "icons/path.png"
        };
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileResult = new FileResultDto { Stream = fileStream, ContentType = "image/png" };

        _serviceMock.GetRegisterReason(1, Arg.Any<CancellationToken>())
            .Returns(reason);
        _serviceMock.GetRegisterReasonIconFile("icons/path.png")
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
        _serviceMock.GetRegisterReason(1, Arg.Any<CancellationToken>())
            .Throws(new Exception("Read error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetIcon(1, CancellationToken.None));
    }
}

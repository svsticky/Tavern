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

public class RegisterSlidesControllerTests
{
    private readonly IRegisterSlideService _serviceMock;
    private readonly RegisterSlidesController _controller;
    private readonly Guid _userId;

    public RegisterSlidesControllerTests()
    {
        _serviceMock = Substitute.For<IRegisterSlideService>();
        _controller = new RegisterSlidesController(_serviceMock);
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
    public async Task GetRegisterSlides_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<RegisterSlideResponseDTO>
        {
            new RegisterSlideResponseDTO
            {
                Id = 1,
                SortOrder = 1,
                ImagePath = "slides/1.png"
            }
        };
        _serviceMock.GetRegisterSlides(Arg.Any<CancellationToken>())
            .Returns(list);

        // Act
        var result = await _controller.GetRegisterSlides(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<RegisterSlideResponseDTO>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetRegisterSlides_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.GetRegisterSlides(Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetRegisterSlides(CancellationToken.None));
    }

    [Fact]
    public async Task GetRegisterSlide_Success_ReturnsOk()
    {
        // Arrange
        var slide = new RegisterSlideResponseDTO
        {
            Id = 5,
            SortOrder = 2,
            ImagePath = "slides/2.png"
        };
        _serviceMock.GetRegisterSlide(5, Arg.Any<CancellationToken>())
            .Returns(slide);

        // Act
        var result = await _controller.GetRegisterSlide(5, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSlide = Assert.IsType<RegisterSlideResponseDTO>(okResult.Value);
        Assert.Equal(5, returnedSlide.Id);
    }

    [Fact]
    public async Task GetRegisterSlide_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetRegisterSlide(99, Arg.Any<CancellationToken>())
            .Returns((RegisterSlideResponseDTO?)null);

        // Act
        var result = await _controller.GetRegisterSlide(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRegisterSlide_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.GetRegisterSlide(5, Arg.Any<CancellationToken>())
            .Throws(new Exception("Error retrieving"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetRegisterSlide(5, CancellationToken.None));
    }

    [Fact]
    public async Task PostRegisterSlide_Success_ReturnsCreated()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        var dto = new PostRegisterSlideDTO
        {
            Image = fileMock,
            SortOrder = 1
        };
        var created = new RegisterSlideResponseDTO
        {
            Id = 10,
            SortOrder = 1,
            ImagePath = "slides/10.png"
        };
        _serviceMock.CreateRegisterSlide(dto, _userId, Arg.Any<CancellationToken>())
            .Returns(created);

        // Act
        var result = await _controller.PostRegisterSlide(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("GetRegisterSlide", createdResult.ActionName);
        Assert.Equal(10, createdResult.RouteValues?["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostRegisterSlide_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        var dto = new PostRegisterSlideDTO
        {
            Image = fileMock,
            SortOrder = 1
        };
        _serviceMock.CreateRegisterSlide(dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostRegisterSlide(dto, CancellationToken.None));
}

    [Fact]
    public async Task PostRegisterSlide_Error_ThrowsException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        var dto = new PostRegisterSlideDTO
        {
            Image = fileMock,
            SortOrder = 1
        };
        _serviceMock.CreateRegisterSlide(dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Fail"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostRegisterSlide(dto, CancellationToken.None));
}

    [Fact]
    public async Task PutRegisterSlide_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new RegisterSlideUpdateDTO
        {
            SortOrder = 2
        };
        _serviceMock.UpdateRegisterSlide(1, dto, _userId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PutRegisterSlide(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task PutRegisterSlide_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new RegisterSlideUpdateDTO
        {
            SortOrder = 2
        };
        _serviceMock.UpdateRegisterSlide(1, dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutRegisterSlide(1, dto, CancellationToken.None));
}

    [Fact]
    public async Task PutRegisterSlide_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new RegisterSlideUpdateDTO
        {
            SortOrder = 2
        };
        _serviceMock.UpdateRegisterSlide(1, dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutRegisterSlide(1, dto, CancellationToken.None));
}

    [Fact]
    public async Task PutRegisterSlide_Error_ThrowsException()
    {
        // Arrange
        var dto = new RegisterSlideUpdateDTO
        {
            SortOrder = 2
        };
        _serviceMock.UpdateRegisterSlide(1, dto, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Put error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutRegisterSlide(1, dto, CancellationToken.None));
}

    [Fact]
    public async Task DeleteRegisterSlide_Success_ReturnsNoContent()
    {
        // Arrange
        _serviceMock.DeleteRegisterSlide(1, _userId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteRegisterSlide(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteRegisterSlide_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteRegisterSlide(1, _userId, Arg.Any<CancellationToken>())
            .Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteRegisterSlide(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRegisterSlide_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteRegisterSlide(1, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteRegisterSlide(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRegisterSlide_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteRegisterSlide(1, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Delete error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteRegisterSlide(1, CancellationToken.None));
    }

    [Fact]
    public async Task UploadImage_Success_ReturnsOk()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadRegisterSlideImage(1, _userId, fileMock)
            .Returns(Task.FromResult<string?>("slides/slide.png"));

        // Act
        var result = await _controller.UploadImage(1, fileMock);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UploadPictureResponse>(okResult.Value);
        Assert.Equal("slides/slide.png", response.Path);
    }

    [Fact]
    public async Task UploadImage_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadRegisterSlideImage(1, _userId, fileMock)
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.UploadImage(1, fileMock));
    }

    [Fact]
    public async Task UploadImage_Error_ThrowsException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadRegisterSlideImage(1, _userId, fileMock)
            .Throws(new Exception("Upload error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.UploadImage(1, fileMock));
    }

    [Fact]
    public async Task GetImage_NotFoundLinkOrImagePath_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetRegisterSlide(1, Arg.Any<CancellationToken>())
            .Returns((RegisterSlideResponseDTO?)null);

        // Act
        var result = await _controller.GetImage(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetImage_NotFoundImageFile_ReturnsNotFound()
    {
        // Arrange
        var slide = new RegisterSlideResponseDTO
        {
            Id = 1,
            SortOrder = 1,
            ImagePath = "slides/path.png"
        };
        _serviceMock.GetRegisterSlide(1, Arg.Any<CancellationToken>())
            .Returns(slide);
        _serviceMock.GetRegisterSlideImageFile("slides/path.png")
            .Returns(Task.FromResult<FileResultDto?>(null));

        // Act
        var result = await _controller.GetImage(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetImage_Success_ReturnsFile()
    {
        // Arrange
        var slide = new RegisterSlideResponseDTO
        {
            Id = 1,
            SortOrder = 1,
            ImagePath = "slides/path.png"
        };
        var fileStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileResult = new FileResultDto { Stream = fileStream, ContentType = "image/png" };

        _serviceMock.GetRegisterSlide(1, Arg.Any<CancellationToken>())
            .Returns(slide);
        _serviceMock.GetRegisterSlideImageFile("slides/path.png")
            .Returns(fileResult);

        // Act
        var result = await _controller.GetImage(1, CancellationToken.None);

        // Assert
        var fileResultVal = Assert.IsType<FileStreamResult>(result.Result);
        Assert.Equal("image/png", fileResultVal.ContentType);
        Assert.Equal(fileStream, fileResultVal.FileStream);
    }

    [Fact]
    public async Task GetImage_Error_ThrowsException()
    {
        // Arrange
        _serviceMock.GetRegisterSlide(1, Arg.Any<CancellationToken>())
            .Throws(new Exception("Read error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetImage(1, CancellationToken.None));
    }
}

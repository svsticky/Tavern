using System;
using System.IO;
using System.Security.Claims;
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

public class ProfilePictureControllerTests
{
    private readonly IProfilePictureService _serviceMock;
    private readonly ProfilePictureController _controller;
    private readonly Guid _userId;

    public ProfilePictureControllerTests()
    {
        _serviceMock = Substitute.For<IProfilePictureService>();
        _controller = new ProfilePictureController(_serviceMock);
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
    public async Task GetProfilePictureByPath_Success_ReturnsFileStream()
    {
        // Arrange
        var path = "uploads/avatar.png";
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var contentType = "image/png";
        _serviceMock.GetProfilePictureByPath(path)
            .Returns(Task.FromResult<(Stream Stream, string ContentType)?>((stream, contentType)));

        // Act
        var result = await _controller.GetProfilePictureByPath(path);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result.Result);
        Assert.Equal(contentType, fileResult.ContentType);
        Assert.Equal(stream, fileResult.FileStream);
    }

    [Fact]
    public async Task GetProfilePictureByPath_NotFound_ReturnsNotFound()
    {
        // Arrange
        var path = "missing.png";
        _serviceMock.GetProfilePictureByPath(path)
            .Returns(Task.FromResult<(Stream Stream, string ContentType)?>(null));

        // Act
        var result = await _controller.GetProfilePictureByPath(path);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetProfilePictureByPath_Error_ThrowsException()
    {
        // Arrange
        var path = "error.png";
        _serviceMock.GetProfilePictureByPath(path)
            .Throws(new Exception("Read error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetProfilePictureByPath(path));
    }

    [Fact]
    public async Task UploadProfilePicture_Success_ReturnsOk()
    {
        // Arrange
        var targetMemberId = Guid.NewGuid();
        var mockFile = Substitute.For<IFormFile>();
        var generatedPath = "uploads/new_avatar.webp";

        _serviceMock.UploadProfilePicture(targetMemberId, _userId, mockFile)
            .Returns(Task.FromResult<string?>(generatedPath));

        // Act
        var result = await _controller.UploadProfilePicture(targetMemberId, mockFile);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UploadPictureResponse>(okResult.Value);
        Assert.Equal(generatedPath, response.Path);
    }

    [Fact]
    public async Task UploadProfilePicture_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var targetMemberId = Guid.NewGuid();
        var mockFile = Substitute.For<IFormFile>();

        _serviceMock.UploadProfilePicture(targetMemberId, _userId, mockFile)
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.UploadProfilePicture(targetMemberId, mockFile));
    }

    [Fact]
    public async Task UploadProfilePicture_Error_ThrowsExceptionMessage()
    {
        // Arrange
        var targetMemberId = Guid.NewGuid();
        var mockFile = Substitute.For<IFormFile>();

        _serviceMock.UploadProfilePicture(targetMemberId, _userId, mockFile)
            .Throws(new Exception("Upload failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.UploadProfilePicture(targetMemberId, mockFile));
    }
}

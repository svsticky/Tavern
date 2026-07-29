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

public class StudiesControllerTests
{
    private readonly IStudyService _serviceMock;
    private readonly StudiesController _controller;
    private readonly Guid _userId;

    public StudiesControllerTests()
    {
        _serviceMock = Substitute.For<IStudyService>();
        _controller = new StudiesController(_serviceMock);
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
    public async Task GetStudies_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<Study> { new Study { Id = 1, Title = "Computer Science", NominalDurationYears = 3, Type = StudyType.Bachelor } };
        _serviceMock.GetStudies(Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetStudies(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<Study>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetStudies_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetStudies(Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetStudies(CancellationToken.None));
    }

    [Fact]
    public async Task GetStudy_Found_ReturnsOk()
    {
        // Arrange
        var study = new Study { Id = 2, Title = "Mathematics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _serviceMock.GetStudy(2, Arg.Any<CancellationToken>()).Returns(study);

        // Act
        var result = await _controller.GetStudy(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudy = Assert.IsType<Study>(okResult.Value);
        Assert.Equal("Mathematics", returnedStudy.Title);
    }

    [Fact]
    public async Task GetStudy_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetStudy(3, Arg.Any<CancellationToken>()).Returns((Study?)null);

        // Act
        var result = await _controller.GetStudy(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetStudy_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetStudy(3, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetStudy(3, CancellationToken.None));
    }

    [Fact]
    public async Task PostStudy_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostStudyDTO { Title = "Physics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        var created = new Study { Id = 10, Title = "Physics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _serviceMock.CreateStudy(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostStudy(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetStudy), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostStudy_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostStudyDTO { Title = "Physics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _serviceMock.CreateStudy(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostStudy(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PostStudy_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostStudyDTO { Title = "Physics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _serviceMock.CreateStudy(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostStudy(dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteStudy_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteStudy(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteStudy(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteStudy_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteStudy(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteStudy(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteStudy_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteStudy(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteStudy(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteStudy_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteStudy(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteStudy(1, CancellationToken.None));
    }

    [Fact]
    public async Task PatchStudy_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Study>();

        // Act
        var result = await _controller.PatchStudy(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).PatchStudy(1, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchStudy_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Study>();
        _serviceMock.PatchStudy(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PatchStudy(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchStudy_Exception_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Study>();
        _serviceMock.PatchStudy(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchStudy(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PutStudy_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new StudyUpdateDTO { Title = "Updated", NominalDurationYears = 3, Type = StudyType.Bachelor };

        // Act
        var result = await _controller.PutStudy(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateStudy(1, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutStudy_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new StudyUpdateDTO { Title = "Updated", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _serviceMock.UpdateStudy(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutStudy(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutStudy_Exception_ThrowsException()
    {
        // Arrange
        var dto = new StudyUpdateDTO { Title = "Updated", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _serviceMock.UpdateStudy(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutStudy(1, dto, CancellationToken.None));
    }
}

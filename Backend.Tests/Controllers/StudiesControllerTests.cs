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
    private readonly IStudyRepository _repositoryMock;
    private readonly StudiesController _controller;
    private readonly Guid _userId;

    public StudiesControllerTests()
    {
        _repositoryMock = Substitute.For<IStudyRepository>();
        _controller = new StudiesController(_repositoryMock);
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
        _repositoryMock.GetStudies(Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetStudies(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<Study>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetStudies_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.GetStudies(Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetStudies(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task GetStudy_Found_ReturnsOk()
    {
        // Arrange
        var study = new Study { Id = 2, Title = "Mathematics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _repositoryMock.GetStudy(2, Arg.Any<CancellationToken>()).Returns(study);

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
        _repositoryMock.GetStudy(3, Arg.Any<CancellationToken>()).Returns((Study?)null);

        // Act
        var result = await _controller.GetStudy(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetStudy_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.GetStudy(3, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetStudy(3, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PostStudy_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostStudyDTO { Title = "Physics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        var created = new Study { Id = 10, Title = "Physics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _repositoryMock.CreateStudy(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostStudy(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetStudy), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostStudy_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new PostStudyDTO { Title = "Physics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _repositoryMock.CreateStudy(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PostStudy(dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task PostStudy_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PostStudyDTO { Title = "Physics", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _repositoryMock.CreateStudy(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PostStudy(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task DeleteStudy_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteStudy(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _repositoryMock.Received(1).DeleteStudy(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteStudy_NotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock.DeleteStudy(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.DeleteStudy(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteStudy_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _repositoryMock.DeleteStudy(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteStudy(1, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteStudy_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.DeleteStudy(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.DeleteStudy(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
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
        await _repositoryMock.Received(1).PatchStudy(1, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchStudy_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Study>();
        _repositoryMock.PatchStudy(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PatchStudy(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PatchStudy_Exception_ReturnsBadRequest()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Study>();
        _repositoryMock.PatchStudy(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PatchStudy(1, patchDoc, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
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
        await _repositoryMock.Received(1).UpdateStudy(1, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutStudy_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new StudyUpdateDTO { Title = "Updated", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _repositoryMock.UpdateStudy(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PutStudy(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PutStudy_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new StudyUpdateDTO { Title = "Updated", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _repositoryMock.UpdateStudy(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PutStudy(1, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }
}

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

public class StudyEnrollmentsControllerTests
{
    private readonly IStudyEnrollmentRepository _repositoryMock;
    private readonly StudyEnrollmentsController _controller;
    private readonly Guid _userId;

    public StudyEnrollmentsControllerTests()
    {
        _repositoryMock = Substitute.For<IStudyEnrollmentRepository>();
        _controller = new StudyEnrollmentsController(_repositoryMock);
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
    public async Task GetStudyEnrollments_Success_ReturnsOk()
    {
        // Arrange
        var dto = new GetStudyEnrollmentsDTO();
        var list = new List<StudyEnrollmentResponseDTO>
        {
            new StudyEnrollmentResponseDTO { Id = 1, StudyTitle = "Computer Science", EnrollmentDate = DateTimeOffset.UtcNow, Status = StudyStatus.Enrolled }
        };
        _repositoryMock.GetStudyEnrollments(dto, _userId, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetStudyEnrollments(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<StudyEnrollmentResponseDTO>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetStudyEnrollments_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new GetStudyEnrollmentsDTO();
        _repositoryMock.GetStudyEnrollments(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetStudyEnrollments(dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetStudyEnrollments_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GetStudyEnrollmentsDTO();
        _repositoryMock.GetStudyEnrollments(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetStudyEnrollments(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task GetStudyEnrollment_Found_ReturnsOk()
    {
        // Arrange
        var response = new StudyEnrollmentResponseDTO { Id = 2, StudyTitle = "Mathematics", EnrollmentDate = DateTimeOffset.UtcNow, Status = StudyStatus.Enrolled };
        _repositoryMock.GetStudyEnrollment(2, _userId, Arg.Any<CancellationToken>()).Returns(response);

        // Act
        var result = await _controller.GetStudyEnrollment(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<StudyEnrollmentResponseDTO>(okResult.Value);
        Assert.Equal("Mathematics", returned.StudyTitle);
    }

    [Fact]
    public async Task GetStudyEnrollment_NotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock.GetStudyEnrollment(3, _userId, Arg.Any<CancellationToken>()).Returns((StudyEnrollmentResponseDTO?)null);

        // Act
        var result = await _controller.GetStudyEnrollment(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetStudyEnrollment_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _repositoryMock.GetStudyEnrollment(3, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetStudyEnrollment(3, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetStudyEnrollment_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.GetStudyEnrollment(3, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetStudyEnrollment(3, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PostStudyEnrollment_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostStudyEnrollmentDTO { StudyId = 1, MemberId = Guid.NewGuid(), EnrollmentDate = DateTimeOffset.UtcNow };
        var created = new StudyEnrollmentResponseDTO { Id = 10, StudyTitle = "Physics", EnrollmentDate = DateTimeOffset.UtcNow, Status = StudyStatus.Enrolled };
        _repositoryMock.CreateStudyEnrollment(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostStudyEnrollment(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetStudyEnrollment), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostStudyEnrollment_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new PostStudyEnrollmentDTO { StudyId = 1, MemberId = Guid.NewGuid(), EnrollmentDate = DateTimeOffset.UtcNow };
        _repositoryMock.CreateStudyEnrollment(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PostStudyEnrollment(dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task PostStudyEnrollment_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PostStudyEnrollmentDTO { StudyId = 1, MemberId = Guid.NewGuid(), EnrollmentDate = DateTimeOffset.UtcNow };
        _repositoryMock.CreateStudyEnrollment(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PostStudyEnrollment(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task DeleteStudyEnrollment_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteStudyEnrollment(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _repositoryMock.Received(1).DeleteStudyEnrollment(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteStudyEnrollment_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _repositoryMock.DeleteStudyEnrollment(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteStudyEnrollment(1, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteStudyEnrollment_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.DeleteStudyEnrollment(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.DeleteStudyEnrollment(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PatchStudy_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<StudyEnrollment>();

        // Act
        var result = await _controller.PatchStudy(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _repositoryMock.Received(1).PatchStudyEnrollment(1, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchStudy_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<StudyEnrollment>();
        _repositoryMock.PatchStudyEnrollment(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PatchStudy(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PatchStudy_Exception_ReturnsBadRequest()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<StudyEnrollment>();
        _repositoryMock.PatchStudyEnrollment(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PatchStudy(1, patchDoc, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }
}

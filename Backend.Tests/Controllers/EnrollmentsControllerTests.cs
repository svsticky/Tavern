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
using Backend.Models;
using Xunit;

namespace Backend.Tests.Controllers;

public class EnrollmentsControllerTests
{
    private readonly IEnrollmentService _serviceMock;
    private readonly EnrollmentsController _controller;
    private readonly Guid _userId;

    public EnrollmentsControllerTests()
    {
        _serviceMock = Substitute.For<IEnrollmentService>();
        _controller = new EnrollmentsController(_serviceMock);
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
    public async Task GetEnrollments_Success_ReturnsOk()
    {
        // Arrange
        var dto = new GetEnrollmentsDTO();
        var list = new List<EnrollmentResponseDTO>
        {
            new EnrollmentResponseDTO
            {
                IsOnWaitingList = false,
                RegisteredOn = DateTime.UtcNow,
                Activity = new Backend.Controllers.DTOs.ActivityResponseDTO
                {
                    Id = 1,
                    Name = "Party",
                    Price = 0m,
                    DutchDescription = "Party",
                    EnglishDescription = "Party",
                    DateTimeStart = DateTimeOffset.UtcNow,
                    DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
                    Location = "Tavern",
                    ShowInKoala = true,
                    ShowOnWebsite = true,
                    IsEnrollable = true,
                    AreParticipantsVisible = true,
                    IsAdultOnly = false,
                    IsWeeklyDrinks = false,
                    Enrollments = new List<EnrollmentResponseDTO>(),
                    SpecificationQuestions = new List<GetSpecificationQuestionResponseDTO>(),
                    AllowedAudience = TargetAudience.All
                },
                Member = new MemberResponseDTO { Id = Guid.NewGuid() }
            }
        };
        _serviceMock.GetEnrollments(dto, _userId, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetEnrollments(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<EnrollmentResponseDTO>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetEnrollments_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new GetEnrollmentsDTO();
        _serviceMock.GetEnrollments(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetEnrollments(dto, CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollments_Exception_ThrowsException()
    {
        // Arrange
        var dto = new GetEnrollmentsDTO();
        _serviceMock.GetEnrollments(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetEnrollments(dto, CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollment_Found_ReturnsOk()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        var response = new EnrollmentResponseDTO
        {
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow,
            Activity = new Backend.Controllers.DTOs.ActivityResponseDTO
            {
                Id = 2,
                Name = "Symposium",
                Price = 0m,
                DutchDescription = "Symposium",
                EnglishDescription = "Symposium",
                DateTimeStart = DateTimeOffset.UtcNow,
                DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
                Location = "Tavern",
                ShowInKoala = true,
                ShowOnWebsite = true,
                IsEnrollable = true,
                AreParticipantsVisible = true,
                IsAdultOnly = false,
                IsWeeklyDrinks = false,
                Enrollments = new List<EnrollmentResponseDTO>(),
                SpecificationQuestions = new List<GetSpecificationQuestionResponseDTO>(),
                AllowedAudience = TargetAudience.All
            },
            Member = new MemberResponseDTO { Id = targetMember }
        };
        _serviceMock.GetEnrollment(2, targetMember, _userId, Arg.Any<CancellationToken>()).Returns(response);

        // Act
        var result = await _controller.GetEnrollment(2, targetMember, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetEnrollment_NotFound_ReturnsNotFound()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        _serviceMock.GetEnrollment(3, targetMember, _userId, Arg.Any<CancellationToken>()).Returns((EnrollmentResponseDTO?)null);

        // Act
        var result = await _controller.GetEnrollment(3, targetMember, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetEnrollment_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        _serviceMock.GetEnrollment(3, targetMember, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetEnrollment(3, targetMember, CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollment_Exception_ThrowsException()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        _serviceMock.GetEnrollment(3, targetMember, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetEnrollment(3, targetMember, CancellationToken.None));
    }

    [Fact]
    public async Task PostEnrollment_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostEnrollmentDTO { ActivityId = 1, MemberId = Guid.NewGuid() };
        var created = new EnrollmentResponseDTO
        {
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow,
            Activity = new Backend.Controllers.DTOs.ActivityResponseDTO
            {
                Id = 1,
                Name = "Party",
                Price = 0m,
                DutchDescription = "Party",
                EnglishDescription = "Party",
                DateTimeStart = DateTimeOffset.UtcNow,
                DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
                Location = "Tavern",
                ShowInKoala = true,
                ShowOnWebsite = true,
                IsEnrollable = true,
                AreParticipantsVisible = true,
                IsAdultOnly = false,
                IsWeeklyDrinks = false,
                Enrollments = new List<EnrollmentResponseDTO>(),
                SpecificationQuestions = new List<GetSpecificationQuestionResponseDTO>(),
                AllowedAudience = TargetAudience.All
            },
            Member = new MemberResponseDTO { Id = dto.MemberId }
        };
        _serviceMock.CreateEnrollment(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostEnrollment(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetEnrollment), createdResult.ActionName);
        Assert.Equal(1u, createdResult.RouteValues!["activityId"]);
        Assert.Equal(dto.MemberId, createdResult.RouteValues!["memberId"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostEnrollment_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new PostEnrollmentDTO { ActivityId = 1, MemberId = Guid.NewGuid() };
        _serviceMock.CreateEnrollment(dto, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PostEnrollment(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PostEnrollment_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostEnrollmentDTO { ActivityId = 1, MemberId = Guid.NewGuid() };
        _serviceMock.CreateEnrollment(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostEnrollment(dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteEnrollment_Success_ReturnsNoContent()
    {
        // Arrange
        var targetMember = Guid.NewGuid();

        // Act
        var result = await _controller.DeleteEnrollment(1, targetMember, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteEnrollment(1, targetMember, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteEnrollment_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        _serviceMock.DeleteEnrollment(1, targetMember, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteEnrollment(1, targetMember, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteEnrollment_Exception_ThrowsException()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        _serviceMock.DeleteEnrollment(1, targetMember, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteEnrollment(1, targetMember, CancellationToken.None));
    }

    [Fact]
    public async Task PutEnrollment_MismatchedIds_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PostEnrollmentDTO { ActivityId = 1, MemberId = Guid.NewGuid() };

        // Act
        var result = await _controller.PutEnrollment(2, dto.MemberId, dto, CancellationToken.None);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ActivityId and MemberId in the URL must match those in the body.", badResult.Value);
    }

    [Fact]
    public async Task PutEnrollment_Success_ReturnsNoContent()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        var dto = new PostEnrollmentDTO { ActivityId = 1, MemberId = targetMember };

        // Act
        var result = await _controller.PutEnrollment(1, targetMember, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateEnrollment(dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutEnrollment_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        var dto = new PostEnrollmentDTO { ActivityId = 1, MemberId = targetMember };
        _serviceMock.UpdateEnrollment(dto, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutEnrollment(1, targetMember, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutEnrollment_Exception_ThrowsException()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        var dto = new PostEnrollmentDTO { ActivityId = 1, MemberId = targetMember };
        _serviceMock.UpdateEnrollment(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutEnrollment(1, targetMember, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PatchEnrollment_NullDoc_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.PatchEnrollment(1, Guid.NewGuid(), null!, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task PatchEnrollment_Success_ReturnsNoContent()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        var patchDoc = new JsonPatchDocument<Enrollment>();

        // Act
        var result = await _controller.PatchEnrollment(1, targetMember, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).PatchEnrollment(1, targetMember, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchEnrollment_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        var patchDoc = new JsonPatchDocument<Enrollment>();
        _serviceMock.PatchEnrollment(1, targetMember, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PatchEnrollment(1, targetMember, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchEnrollment_Exception_ThrowsException()
    {
        // Arrange
        var targetMember = Guid.NewGuid();
        var patchDoc = new JsonPatchDocument<Enrollment>();
        _serviceMock.PatchEnrollment(1, targetMember, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchEnrollment(1, targetMember, patchDoc, CancellationToken.None));
    }
}

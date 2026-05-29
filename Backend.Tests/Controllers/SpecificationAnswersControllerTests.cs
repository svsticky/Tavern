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
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Controllers;

public class SpecificationAnswersControllerTests
{
    private readonly ISpecificationAnswerRepository _repositoryMock;
    private readonly SpecificationAnswers _controller;
    private readonly Guid _userId;

    public SpecificationAnswersControllerTests()
    {
        _repositoryMock = Substitute.For<ISpecificationAnswerRepository>();
        _controller = new SpecificationAnswers(_repositoryMock);
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
    public async Task PatchSpecificationAnswer_Success_ReturnsNoContent()
    {
        // Arrange
        uint answerId = 123;
        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();

        _repositoryMock.PatchSpecificationAnswersAsync(_userId, answerId, patchDoc, _userId)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PatchSpecificationAnswer(answerId, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _repositoryMock.Received(1).PatchSpecificationAnswersAsync(_userId, answerId, patchDoc, _userId);
    }

    [Fact]
    public async Task PatchSpecificationAnswer_Forbidden_ReturnsForbid()
    {
        // Arrange
        uint answerId = 123;
        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();

        _repositoryMock.PatchSpecificationAnswersAsync(_userId, answerId, patchDoc, _userId)
            .Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PatchSpecificationAnswer(answerId, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PatchSpecificationAnswer_Error_ReturnsBadRequest()
    {
        // Arrange
        uint answerId = 123;
        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();

        _repositoryMock.PatchSpecificationAnswersAsync(_userId, answerId, patchDoc, _userId)
            .Throws(new Exception("Invalid patch operation"));

        // Act
        var result = await _controller.PatchSpecificationAnswer(answerId, patchDoc, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorDto = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Invalid patch operation", errorDto.Message);
    }
}

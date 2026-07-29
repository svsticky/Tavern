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
    private readonly ISpecificationAnswerService _serviceMock;
    private readonly SpecificationAnswers _controller;
    private readonly Guid _userId;

    public SpecificationAnswersControllerTests()
    {
        _serviceMock = Substitute.For<ISpecificationAnswerService>();
        _controller = new SpecificationAnswers(_serviceMock);
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

        _serviceMock.PatchSpecificationAnswersAsync(_userId, answerId, patchDoc, _userId)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PatchSpecificationAnswer(answerId, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).PatchSpecificationAnswersAsync(_userId, answerId, patchDoc, _userId);
    }

    [Fact]
    public async Task PatchSpecificationAnswer_Forbidden_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        uint answerId = 123;
        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();

        _serviceMock.PatchSpecificationAnswersAsync(_userId, answerId, patchDoc, _userId)
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PatchSpecificationAnswer(answerId, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchSpecificationAnswer_Error_ThrowsException()
    {
        // Arrange
        uint answerId = 123;
        var patchDoc = new JsonPatchDocument<SpecificationAnswer>();

        _serviceMock.PatchSpecificationAnswersAsync(_userId, answerId, patchDoc, _userId)
            .Throws(new Exception("Invalid patch operation"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchSpecificationAnswer(answerId, patchDoc, CancellationToken.None));
    }
}

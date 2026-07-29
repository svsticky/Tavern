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

public class RoleAliasControllerTests
{
    private readonly IRoleAliasService _serviceMock;
    private readonly RoleAliasesController _controller;
    private readonly Guid _userId;

    public RoleAliasControllerTests()
    {
        _serviceMock = Substitute.For<IRoleAliasService>();
        _controller = new RoleAliasesController(_serviceMock);
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
    public async Task GetRoleAliases_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<RoleAlias> { new RoleAlias { Id = 1, Name = "Admin", RoleId = 1 } };
        _serviceMock.GetRoleAliases(Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetRoleAliases(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<RoleAlias>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetRoleAliases_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetRoleAliases(Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetRoleAliases(CancellationToken.None));
    }

    [Fact]
    public async Task GetRoleAlias_Found_ReturnsOk()
    {
        // Arrange
        var alias = new RoleAlias { Id = 2, Name = "User", RoleId = 1 };
        _serviceMock.GetRoleAlias(2, Arg.Any<CancellationToken>()).Returns(alias);

        // Act
        var result = await _controller.GetRoleAlias(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAlias = Assert.IsType<RoleAlias>(okResult.Value);
        Assert.Equal("User", returnedAlias.Name);
    }

    [Fact]
    public async Task GetRoleAlias_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetRoleAlias(3, Arg.Any<CancellationToken>()).Returns((RoleAlias?)null);

        // Act
        var result = await _controller.GetRoleAlias(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRoleAlias_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetRoleAlias(3, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetRoleAlias(3, CancellationToken.None));
    }

    [Fact]
    public async Task PostRoleAlias_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostRoleAliasDTO { Name = "NewRole", RoleId = 1 };
        var created = new RoleAlias { Id = 10, Name = "NewRole", RoleId = 1 };
        _serviceMock.CreateRoleAlias(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostRoleAlias(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetRoleAlias), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostRoleAlias_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostRoleAliasDTO { Name = "NewRole", RoleId = 1 };
        _serviceMock.CreateRoleAlias(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostRoleAlias(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PostRoleAlias_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostRoleAliasDTO { Name = "NewRole", RoleId = 1 };
        _serviceMock.CreateRoleAlias(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostRoleAlias(dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRoleAlias_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteRoleAlias(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteRoleAlias(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRoleAlias_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteRoleAlias(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteRoleAlias(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRoleAlias_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteRoleAlias(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteRoleAlias(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRoleAlias_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteRoleAlias(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteRoleAlias(1, CancellationToken.None));
    }

    [Fact]
    public async Task PatchRoleAlias_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<RoleAlias>();

        // Act
        var result = await _controller.PatchRoleAlias(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).PatchRoleAlias(1, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchRoleAlias_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<RoleAlias>();
        _serviceMock.PatchRoleAlias(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PatchRoleAlias(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchRoleAlias_Exception_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<RoleAlias>();
        _serviceMock.PatchRoleAlias(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchRoleAlias(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PutRoleAlias_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new RoleAliasUpdateDTO { Name = "Updated", RoleId = 1 };

        // Act
        var result = await _controller.PutRoleAlias(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateRoleAlias(1, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutRoleAlias_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new RoleAliasUpdateDTO { Name = "Updated", RoleId = 1 };
        _serviceMock.UpdateRoleAlias(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutRoleAlias(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutRoleAlias_Exception_ThrowsException()
    {
        // Arrange
        var dto = new RoleAliasUpdateDTO { Name = "Updated", RoleId = 1 };
        _serviceMock.UpdateRoleAlias(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutRoleAlias(1, dto, CancellationToken.None));
    }
}

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

public class RolesControllerTests
{
    private readonly IRoleService _serviceMock;
    private readonly RolesController _controller;
    private readonly Guid _userId;

    public RolesControllerTests()
    {
        _serviceMock = Substitute.For<IRoleService>();
        _controller = new RolesController(_serviceMock);
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
    public async Task GetRoles_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<Role> { new Role { Id = 1, Name = "Admin" } };
        _serviceMock.GetRoles(Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetRoles(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<Role>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetRoles_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetRoles(Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetRoles(CancellationToken.None));
    }

    [Fact]
    public async Task GetRole_Found_ReturnsOk()
    {
        // Arrange
        var role = new Role { Id = 2, Name = "User" };
        _serviceMock.GetRole(2, Arg.Any<CancellationToken>()).Returns(role);

        // Act
        var result = await _controller.GetRole(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedRole = Assert.IsType<Role>(okResult.Value);
        Assert.Equal("User", returnedRole.Name);
    }

    [Fact]
    public async Task GetRole_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetRole(3, Arg.Any<CancellationToken>()).Returns((Role?)null);

        // Act
        var result = await _controller.GetRole(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRole_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetRole(3, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetRole(3, CancellationToken.None));
    }

    [Fact]
    public async Task PostRole_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostRoleDTO { Name = "NewRole" };
        var created = new Role { Id = 10, Name = "NewRole" };
        _serviceMock.CreateRole(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostRole(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetRole), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostRole_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostRoleDTO { Name = "NewRole" };
        _serviceMock.CreateRole(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostRole(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PostRole_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostRoleDTO { Name = "NewRole" };
        _serviceMock.CreateRole(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostRole(dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRole_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteRole(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteRole(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRole_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteRole(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteRole(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRole_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteRole(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteRole(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRole_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteRole(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteRole(1, CancellationToken.None));
    }

    [Fact]
    public async Task PatchRole_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Role>();

        // Act
        var result = await _controller.PatchRole(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).PatchRole(1, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchRole_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Role>();
        _serviceMock.PatchRole(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PatchRole(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchRole_Exception_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Role>();
        _serviceMock.PatchRole(1, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchRole(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PutRole_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new RoleUpdateDTO { Name = "Updated" };

        // Act
        var result = await _controller.PutRole(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateRole(1, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutRole_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new RoleUpdateDTO { Name = "Updated" };
        _serviceMock.UpdateRole(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutRole(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutRole_Exception_ThrowsException()
    {
        // Arrange
        var dto = new RoleUpdateDTO { Name = "Updated" };
        _serviceMock.UpdateRole(1, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutRole(1, dto, CancellationToken.None));
    }
}

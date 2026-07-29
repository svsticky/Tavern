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

public class GroupMembershipsControllerTests
{
    private readonly IGroupMembershipService _serviceMock;
    private readonly GroupMembershipsController _controller;
    private readonly Guid _userId;

    public GroupMembershipsControllerTests()
    {
        _serviceMock = Substitute.For<IGroupMembershipService>();
        _controller = new GroupMembershipsController(_serviceMock);
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
    public async Task GetGroupMemberships_Success_ReturnsOk()
    {
        // Arrange
        var dto = new GetGroupMembershipsDTO();
        var list = new List<GroupMembershipResponseDTO>
        {
            new GroupMembershipResponseDTO { Id = 1, GroupId = 1, GroupName = "Committee", GroupType = GroupType.Committee, MembershipYear = 2026 }
        };
        _serviceMock.GetGroupMemberships(dto, _userId, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetGroupMemberships(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<GroupMembershipResponseDTO>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetGroupMemberships_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new GetGroupMembershipsDTO();
        _serviceMock.GetGroupMemberships(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetGroupMemberships(dto, CancellationToken.None));
    }

    [Fact]
    public async Task GetGroupMemberships_Exception_ThrowsException()
    {
        // Arrange
        var dto = new GetGroupMembershipsDTO();
        _serviceMock.GetGroupMemberships(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetGroupMemberships(dto, CancellationToken.None));
    }

    [Fact]
    public async Task GetGroupMembership_Found_ReturnsOk()
    {
        // Arrange
        var membership = new GroupMembershipResponseDTO { Id = 2, GroupId = 1, GroupName = "Board", GroupType = GroupType.Committee, MembershipYear = 2026 };
        _serviceMock.GetGroupMembership(2, _userId, Arg.Any<CancellationToken>()).Returns(membership);

        // Act
        var result = await _controller.GetGroupMembership(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<GroupMembershipResponseDTO>(okResult.Value);
        Assert.Equal("Board", returned.GroupName);
    }

    [Fact]
    public async Task GetGroupMembership_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetGroupMembership(3, _userId, Arg.Any<CancellationToken>()).Returns((GroupMembershipResponseDTO?)null);

        // Act
        var result = await _controller.GetGroupMembership(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetGroupMembership_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetGroupMembership(3, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetGroupMembership(3, CancellationToken.None));
    }

    [Fact]
    public async Task GetGroupMembership_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetGroupMembership(3, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetGroupMembership(3, CancellationToken.None));
    }

    [Fact]
    public async Task PostGroupMembership_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostGroupMembershipDTO { GroupId = 1, MemberId = Guid.NewGuid(), MembershipYear = 2026 };
        var created = new GroupMembership { Id = 10, GroupId = 1 };
        _serviceMock.CreateGroupMembership(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostGroupMembership(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetGroupMembership), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostGroupMembership_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostGroupMembershipDTO { GroupId = 1, MemberId = Guid.NewGuid(), MembershipYear = 2026 };
        _serviceMock.CreateGroupMembership(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostGroupMembership(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PostGroupMembership_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostGroupMembershipDTO { GroupId = 1, MemberId = Guid.NewGuid(), MembershipYear = 2026 };
        _serviceMock.CreateGroupMembership(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostGroupMembership(dto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteGroupMembership_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteGroupMembership(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteGroupMembership(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteGroupMembership_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteGroupMembership(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteGroupMembership(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteGroupMembership_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteGroupMembership(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteGroupMembership(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteGroupMembership_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteGroupMembership(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteGroupMembership(1, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroupMembership_NullPatchDoc_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.PatchGroupMembership(1, null!, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task PatchGroupMembership_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<GroupMembership>();

        // Act
        var result = await _controller.PatchGroupMembership(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).PatchGroupMembership(1, _userId, patchDoc, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchGroupMembership_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<GroupMembership>();
        _serviceMock.PatchGroupMembership(1, _userId, patchDoc, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PatchGroupMembership(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroupMembership_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<GroupMembership>();
        _serviceMock.PatchGroupMembership(1, _userId, patchDoc, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PatchGroupMembership(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroupMembership_Exception_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<GroupMembership>();
        _serviceMock.PatchGroupMembership(1, _userId, patchDoc, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchGroupMembership(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PutGroupMembership_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new GroupMembershipUpdateDTO { RoleAliasId = 5 };

        // Act
        var result = await _controller.PutGroupMembership(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateGroupMembership(1, _userId, dto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutGroupMembership_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new GroupMembershipUpdateDTO { RoleAliasId = 5 };
        _serviceMock.UpdateGroupMembership(1, _userId, dto, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutGroupMembership(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutGroupMembership_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new GroupMembershipUpdateDTO { RoleAliasId = 5 };
        _serviceMock.UpdateGroupMembership(1, _userId, dto, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutGroupMembership(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutGroupMembership_Exception_ThrowsException()
    {
        // Arrange
        var dto = new GroupMembershipUpdateDTO { RoleAliasId = 5 };
        _serviceMock.UpdateGroupMembership(1, _userId, dto, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutGroupMembership(1, dto, CancellationToken.None));
    }
}

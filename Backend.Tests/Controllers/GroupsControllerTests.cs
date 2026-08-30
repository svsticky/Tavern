using System;
using System.Collections.Generic;
using System.IO;
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

public class GroupsControllerTests
{
    private readonly IGroupService _serviceMock;
    private readonly GroupsController _controller;
    private readonly Guid _userId;

    public GroupsControllerTests()
    {
        _serviceMock = Substitute.For<IGroupService>();
        _controller = new GroupsController(_serviceMock);
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
    public async Task GetGroups_Success_ReturnsOk()
    {
        // Arrange
        var dto = new GetGroupDTO();
        var list = new List<GroupResponseDTO> { new GroupResponseDTO { Id = 1, Name = "Committee", Active = true, Type = GroupType.Committee } };
        _serviceMock.GetGroups(_userId, dto, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetGroups(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<GroupResponseDTO>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetGroups_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new GetGroupDTO();
        _serviceMock.GetGroups(_userId, dto, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetGroups(dto, CancellationToken.None));
    }

    [Fact]
    public async Task GetGroups_Exception_ThrowsException()
    {
        // Arrange
        var dto = new GetGroupDTO();
        _serviceMock.GetGroups(_userId, dto, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetGroups(dto, CancellationToken.None));
    }

    [Fact]
    public async Task GetGroup_Found_ReturnsOk()
    {
        // Arrange
        var group = new GroupResponseDTO { Id = 2, Name = "Board", Active = true, Type = GroupType.Committee };
        _serviceMock.GetGroup(2, Arg.Any<CancellationToken>()).Returns(group);

        // Act
        var result = await _controller.GetGroup(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<GroupResponseDTO>(okResult.Value);
        Assert.Equal("Board", returned.Name);
    }

    [Fact]
    public async Task GetGroup_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetGroup(3, Arg.Any<CancellationToken>()).Returns((GroupResponseDTO?)null);

        // Act
        var result = await _controller.GetGroup(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetGroup_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetGroup(3, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetGroup(3, CancellationToken.None));
    }

    [Fact]
    public async Task GetGroup_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetGroup(3, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetGroup(3, CancellationToken.None));
    }

    [Fact]
    public async Task PostGroup_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostGroupDTO { Name = "NewGroup", Type = GroupType.Committee, GroupPicture = Substitute.For<IFormFile>() };
        var created = new Group { Id = 10, Name = "NewGroup" };
        _serviceMock.CreateGroup(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostGroup(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetGroup), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostGroup_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostGroupDTO { Name = "NewGroup", Type = GroupType.Committee, GroupPicture = Substitute.For<IFormFile>() };
        _serviceMock.CreateGroup(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostGroup(dto, CancellationToken.None));
    }

    [Fact]
    public async Task PostGroup_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostGroupDTO { Name = "NewGroup", Type = GroupType.Committee, GroupPicture = Substitute.For<IFormFile>() };
        _serviceMock.CreateGroup(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostGroup(dto, CancellationToken.None));
    }

    [Fact]
    public async Task UploadGroupPicture_Success_ReturnsOk()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadGroupPicture(1, _userId, fileMock).Returns("path/to/img.png");

        // Act
        var result = await _controller.UploadGroupPicture(1, fileMock);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var val = Assert.IsType<UploadPictureResponse>(okResult.Value);
        Assert.Equal("path/to/img.png", val.Path);
    }

    [Fact]
    public async Task UploadGroupPicture_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadGroupPicture(1, _userId, fileMock).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.UploadGroupPicture(1, fileMock));
    }

    [Fact]
    public async Task UploadGroupPicture_Exception_ThrowsException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadGroupPicture(1, _userId, fileMock).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.UploadGroupPicture(1, fileMock));
    }

    [Fact]
    public async Task GetGroupPicture_GroupOrPathNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetGroup(1, Arg.Any<CancellationToken>()).Returns((GroupResponseDTO?)null);

        // Act
        var result = await _controller.GetGroupPicture(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetGroupPicture_FileNotFound_ReturnsNotFound()
    {
        // Arrange
        var group = new GroupResponseDTO { Id = 1, GroupPicturePath = "some/path.png", Name = "Committee", Active = true, Type = GroupType.Committee };
        _serviceMock.GetGroup(1, Arg.Any<CancellationToken>()).Returns(group);
        _serviceMock.GetGroupPictureFile("some/path.png").Returns((FileResultDto?)null);

        // Act
        var result = await _controller.GetGroupPicture(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetGroupPicture_FileFound_ReturnsFile()
    {
        // Arrange
        var group = new GroupResponseDTO { Id = 1, GroupPicturePath = "some/path.png", Name = "Committee", Active = true, Type = GroupType.Committee };
        _serviceMock.GetGroup(1, Arg.Any<CancellationToken>()).Returns(group);
        
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileResultDto = new FileResultDto { Stream = stream, ContentType = "image/png" };
        _serviceMock.GetGroupPictureFile("some/path.png").Returns(fileResultDto);

        // Act
        var result = await _controller.GetGroupPicture(1, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result.Result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(stream, fileResult.FileStream);
    }

    [Fact]
    public async Task GetGroupPicture_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetGroup(1, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetGroupPicture(1, CancellationToken.None));
    }

    [Fact]
    public async Task GetGroupPicture_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetGroup(1, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetGroupPicture(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteGroup_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteGroup(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteGroup(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteGroup_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteGroup(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteGroup(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteGroup_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteGroup(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteGroup(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteGroup_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteGroup(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteGroup(1, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroup_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Group>();

        // Act
        var result = await _controller.PatchGroup(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).PatchGroup(1, _userId, patchDoc, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchGroup_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Group>();
        _serviceMock.PatchGroup(1, _userId, patchDoc, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PatchGroup(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroup_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Group>();
        _serviceMock.PatchGroup(1, _userId, patchDoc, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PatchGroup(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroup_Exception_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Group>();
        _serviceMock.PatchGroup(1, _userId, patchDoc, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchGroup(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PutGroup_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new GroupUpdateDTO { Name = "Updated", Active = true, Type = GroupType.Committee };

        // Act
        var result = await _controller.PutGroup(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateGroup(1, _userId, dto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutGroup_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new GroupUpdateDTO { Name = "Updated", Active = true, Type = GroupType.Committee };
        _serviceMock.UpdateGroup(1, _userId, dto, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutGroup(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutGroup_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new GroupUpdateDTO { Name = "Updated", Active = true, Type = GroupType.Committee };
        _serviceMock.UpdateGroup(1, _userId, dto, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutGroup(1, dto, CancellationToken.None));
    }

    [Fact]
    public async Task PutGroup_Exception_ThrowsException()
    {
        // Arrange
        var dto = new GroupUpdateDTO { Name = "Updated", Active = true, Type = GroupType.Committee };
        _serviceMock.UpdateGroup(1, _userId, dto, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutGroup(1, dto, CancellationToken.None));
    }
}

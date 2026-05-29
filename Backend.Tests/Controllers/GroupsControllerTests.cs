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
    private readonly IGroupRepository _repositoryMock;
    private readonly GroupsController _controller;
    private readonly Guid _userId;

    public GroupsControllerTests()
    {
        _repositoryMock = Substitute.For<IGroupRepository>();
        _controller = new GroupsController(_repositoryMock);
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
        _repositoryMock.GetGroups(_userId, dto, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetGroups(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<GroupResponseDTO>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetGroups_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new GetGroupDTO();
        _repositoryMock.GetGroups(_userId, dto, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetGroups(dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetGroups_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GetGroupDTO();
        _repositoryMock.GetGroups(_userId, dto, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetGroups(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task GetGroup_Found_ReturnsOk()
    {
        // Arrange
        var group = new GroupResponseDTO { Id = 2, Name = "Board", Active = true, Type = GroupType.Committee };
        _repositoryMock.GetGroup(2, Arg.Any<CancellationToken>()).Returns(group);

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
        _repositoryMock.GetGroup(3, Arg.Any<CancellationToken>()).Returns((GroupResponseDTO?)null);

        // Act
        var result = await _controller.GetGroup(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetGroup_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _repositoryMock.GetGroup(3, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetGroup(3, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetGroup_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.GetGroup(3, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetGroup(3, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PostGroup_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostGroupDTO { Name = "NewGroup", Type = GroupType.Committee, GroupPicture = Substitute.For<IFormFile>() };
        var created = new Group { Id = 10, Name = "NewGroup" };
        _repositoryMock.CreateGroup(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostGroup(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetGroup), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostGroup_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new PostGroupDTO { Name = "NewGroup", Type = GroupType.Committee, GroupPicture = Substitute.For<IFormFile>() };
        _repositoryMock.CreateGroup(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PostGroup(dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task PostGroup_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PostGroupDTO { Name = "NewGroup", Type = GroupType.Committee, GroupPicture = Substitute.For<IFormFile>() };
        _repositoryMock.CreateGroup(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PostGroup(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task UploadGroupPicture_Success_ReturnsOk()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _repositoryMock.UploadGroupPicture(1, _userId, fileMock).Returns("path/to/img.png");

        // Act
        var result = await _controller.UploadGroupPicture(1, fileMock);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var val = Assert.IsType<UploadPictureResponse>(okResult.Value);
        Assert.Equal("path/to/img.png", val.Path);
    }

    [Fact]
    public async Task UploadGroupPicture_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _repositoryMock.UploadGroupPicture(1, _userId, fileMock).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.UploadGroupPicture(1, fileMock);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task UploadGroupPicture_Exception_ReturnsBadRequest()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _repositoryMock.UploadGroupPicture(1, _userId, fileMock).Throws(new Exception("Error"));

        // Act
        var result = await _controller.UploadGroupPicture(1, fileMock);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task GetGroupPicture_GroupOrPathNotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock.GetGroup(1, Arg.Any<CancellationToken>()).Returns((GroupResponseDTO?)null);

        // Act
        var result = await _controller.GetGroupPicture(1, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Group or group picture not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetGroupPicture_FileNotFound_ReturnsNotFound()
    {
        // Arrange
        var group = new GroupResponseDTO { Id = 1, GroupPicturePath = "some/path.png", Name = "Committee", Active = true, Type = GroupType.Committee };
        _repositoryMock.GetGroup(1, Arg.Any<CancellationToken>()).Returns(group);
        _repositoryMock.GetGroupPictureFile("some/path.png").Returns((FileResultDto?)null);

        // Act
        var result = await _controller.GetGroupPicture(1, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("File is no longer present on the server.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetGroupPicture_FileFound_ReturnsFile()
    {
        // Arrange
        var group = new GroupResponseDTO { Id = 1, GroupPicturePath = "some/path.png", Name = "Committee", Active = true, Type = GroupType.Committee };
        _repositoryMock.GetGroup(1, Arg.Any<CancellationToken>()).Returns(group);
        
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileResultDto = new FileResultDto { Stream = stream, ContentType = "image/png" };
        _repositoryMock.GetGroupPictureFile("some/path.png").Returns(fileResultDto);

        // Act
        var result = await _controller.GetGroupPicture(1, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result.Result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(stream, fileResult.FileStream);
    }

    [Fact]
    public async Task GetGroupPicture_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _repositoryMock.GetGroup(1, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetGroupPicture(1, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetGroupPicture_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.GetGroup(1, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetGroupPicture(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task DeleteGroup_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteGroup(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _repositoryMock.Received(1).DeleteGroup(1, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteGroup_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _repositoryMock.DeleteGroup(1, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteGroup(1, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteGroup_NotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock.DeleteGroup(1, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.DeleteGroup(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteGroup_Exception_ReturnsBadRequest()
    {
        // Arrange
        _repositoryMock.DeleteGroup(1, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.DeleteGroup(1, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
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
        await _repositoryMock.Received(1).PatchGroup(1, _userId, patchDoc, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchGroup_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Group>();
        _repositoryMock.PatchGroup(1, _userId, patchDoc, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PatchGroup(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PatchGroup_NotFound_ReturnsNotFound()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Group>();
        _repositoryMock.PatchGroup(1, _userId, patchDoc, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.PatchGroup(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PatchGroup_Exception_ReturnsBadRequest()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Group>();
        _repositoryMock.PatchGroup(1, _userId, patchDoc, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PatchGroup(1, patchDoc, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
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
        await _repositoryMock.Received(1).UpdateGroup(1, _userId, dto, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutGroup_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new GroupUpdateDTO { Name = "Updated", Active = true, Type = GroupType.Committee };
        _repositoryMock.UpdateGroup(1, _userId, dto, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PutGroup(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PutGroup_NotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new GroupUpdateDTO { Name = "Updated", Active = true, Type = GroupType.Committee };
        _repositoryMock.UpdateGroup(1, _userId, dto, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.PutGroup(1, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutGroup_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GroupUpdateDTO { Name = "Updated", Active = true, Type = GroupType.Committee };
        _repositoryMock.UpdateGroup(1, _userId, dto, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PutGroup(1, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }
}

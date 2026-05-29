using System.Security.Claims;
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

public class SettingsControllerTests
{
    private readonly ISettingsRepository _settingsRepositoryMock;
    private readonly Settings _controller;
    private readonly Guid _userId;

    public SettingsControllerTests()
    {
        _settingsRepositoryMock = Substitute.For<ISettingsRepository>();
        _controller = new Settings(_settingsRepositoryMock);
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
    public async Task GetSettings_Success_ReturnsOk()
    {
        // Arrange
        var settingsList = new List<Setting>
        {
            new Setting { Name = "Setting1", Value = "Val1" }
        };
        _settingsRepositoryMock.GetSettings(_userId, Arg.Any<CancellationToken>())
            .Returns(settingsList);

        // Act
        var result = await _controller.GetSettings(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSettings = Assert.IsAssignableFrom<IEnumerable<Setting>>(okResult.Value);
        Assert.Single(returnedSettings);
    }

    [Fact]
    public async Task GetSettings_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _settingsRepositoryMock.GetSettings(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetSettings(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetSettings_Exception_ReturnsBadRequest()
    {
        // Arrange
        _settingsRepositoryMock.GetSettings(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        // Act
        var result = await _controller.GetSettings(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorDto = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Database error", errorDto.Message);
    }

    [Fact]
    public async Task GetSetting_Authenticated_ReturnsSetting()
    {
        // Arrange
        var settingName = "SpecialSetting";
        var setting = new Setting { Name = settingName, Value = "SpecialValue" };
        _settingsRepositoryMock.GetSetting(settingName, _userId, Arg.Any<CancellationToken>())
            .Returns(setting);

        // Act
        var result = await _controller.GetSetting(settingName, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<Setting>(okResult.Value);
        Assert.Equal("SpecialValue", returned.Value);
    }

    [Fact]
    public async Task GetSetting_Unauthenticated_ReturnsSetting()
    {
        // Arrange
        // Clear HttpContext user to simulate unauthenticated
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        var settingName = "PublicSetting";
        var setting = new Setting { Name = settingName, Value = "PublicValue" };
        _settingsRepositoryMock.GetSetting(settingName, null, Arg.Any<CancellationToken>())
            .Returns(setting);

        // Act
        var result = await _controller.GetSetting(settingName, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<Setting>(okResult.Value);
        Assert.Equal("PublicValue", returned.Value);
    }

    [Fact]
    public async Task GetSetting_NotFound_ReturnsNotFound()
    {
        // Arrange
        var settingName = "NonExistent";
        _settingsRepositoryMock.GetSetting(settingName, _userId, Arg.Any<CancellationToken>())
            .Returns((Setting)null!);

        // Act
        var result = await _controller.GetSetting(settingName, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetSetting_GeneralException_ReturnsBadRequest()
    {
        // Arrange
        var settingName = "Any";
        _settingsRepositoryMock.GetSetting(settingName, _userId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Error getting setting"));

        // Act
        var result = await _controller.GetSetting(settingName, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorDto = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error getting setting", errorDto.Message);
    }

    [Fact]
    public async Task PostSetting_Success_ReturnsCreated()
    {
        // Arrange
        var settingName = "NewSetting";
        var settingValue = "NewValue";
        var returnedSetting = new Setting { Name = settingName, Value = settingValue };
        _settingsRepositoryMock.CreateSetting(settingName, settingValue, _userId, Arg.Any<CancellationToken>())
            .Returns(returnedSetting);

        // Act
        var result = await _controller.PostSetting(settingName, settingValue, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetSetting), createdResult.ActionName);
        var returned = Assert.IsType<Setting>(createdResult.Value);
        Assert.Equal(settingName, returned.Name);
    }

    [Fact]
    public async Task PostSetting_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _settingsRepositoryMock.CreateSetting(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PostSetting("Name", "Value", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task PostSetting_Exception_ReturnsBadRequest()
    {
        // Arrange
        _settingsRepositoryMock.CreateSetting(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Create error"));

        // Act
        var result = await _controller.PostSetting("Name", "Value", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorDto = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Create error", errorDto.Message);
    }

    [Fact]
    public async Task DeleteSetting_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteSetting("ToDelete", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _settingsRepositoryMock.Received(1).DeleteSetting("ToDelete", _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSetting_NotFound_ReturnsNotFound()
    {
        // Arrange
        _settingsRepositoryMock.When(x => x.DeleteSetting(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new KeyNotFoundException());

        // Act
        var result = await _controller.DeleteSetting("ToDelete", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteSetting_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _settingsRepositoryMock.When(x => x.DeleteSetting(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteSetting("ToDelete", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteSetting_Exception_ReturnsBadRequest()
    {
        // Arrange
        _settingsRepositoryMock.When(x => x.DeleteSetting(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new Exception("Delete error"));

        // Act
        var result = await _controller.DeleteSetting("ToDelete", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorDto = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Delete error", errorDto.Message);
    }

    [Fact]
    public async Task PatchSetting_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Setting>();

        // Act
        var result = await _controller.PatchSetting("ToPatch", patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _settingsRepositoryMock.Received(1).PatchSetting("ToPatch", patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchSetting_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Setting>();
        _settingsRepositoryMock.When(x => x.PatchSetting(Arg.Any<string>(), Arg.Any<JsonPatchDocument<Setting>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new UnauthorizedAccessException());

        // Act
        var result = await _controller.PatchSetting("ToPatch", patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PatchSetting_Exception_ReturnsBadRequest()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Setting>();
        _settingsRepositoryMock.When(x => x.PatchSetting(Arg.Any<string>(), Arg.Any<JsonPatchDocument<Setting>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new Exception("Patch error"));

        // Act
        var result = await _controller.PatchSetting("ToPatch", patchDoc, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorDto = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Patch error", errorDto.Message);
    }

    [Fact]
    public async Task PutSetting_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.PutSetting("ToPut", "NewVal", CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _settingsRepositoryMock.Received(1).UpdateSetting("ToPut", "NewVal", _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutSetting_Unauthorized_ReturnsForbid()
    {
        // Arrange
        _settingsRepositoryMock.When(x => x.UpdateSetting(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new UnauthorizedAccessException());

        // Act
        var result = await _controller.PutSetting("ToPut", "NewVal", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PutSetting_Exception_ReturnsBadRequest()
    {
        // Arrange
        _settingsRepositoryMock.When(x => x.UpdateSetting(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
            .Do(x => throw new Exception("Put error"));

        // Act
        var result = await _controller.PutSetting("ToPut", "NewVal", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorDto = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Put error", errorDto.Message);
    }

    [Fact]
    public async Task GetSetting_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var settingName = "SpecialSetting";
        _settingsRepositoryMock.GetSetting(settingName, _userId, Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetSetting(settingName, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }
}

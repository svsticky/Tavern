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

public class MembersControllerTests
{
    private readonly IMemberRepository _memberRepositoryMock;
    private readonly IProfilePictureRepository _profilePictureRepositoryMock;
    private readonly MembersController _controller;
    private readonly Guid _userId;

    public MembersControllerTests()
    {
        _memberRepositoryMock = Substitute.For<IMemberRepository>();
        _profilePictureRepositoryMock = Substitute.For<IProfilePictureRepository>();
        _controller = new MembersController(_memberRepositoryMock, _profilePictureRepositoryMock);
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
    public async Task GetMembers_Success_ReturnsOk()
    {
        // Arrange
        var dto = new GetMembersDto();
        var list = new List<MemberResponseDTO> { new MemberResponseDTO { Id = Guid.NewGuid(), FirstName = "Alice" } };
        _memberRepositoryMock.GetMembers(dto, _userId, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetMembers(dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<MemberResponseDTO>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetMembers_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var dto = new GetMembersDto();
        _memberRepositoryMock.GetMembers(dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetMembers(dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetMembers_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GetMembersDto();
        _memberRepositoryMock.GetMembers(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetMembers(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task GetMember_Found_ReturnsOk()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var member = new MemberResponseDTO { Id = targetId, FirstName = "Bob" };
        _memberRepositoryMock.GetMember(targetId, _userId, Arg.Any<CancellationToken>()).Returns(member);

        // Act
        var result = await _controller.GetMember(targetId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<MemberResponseDTO>(okResult.Value);
        Assert.Equal("Bob", returned.FirstName);
    }

    [Fact]
    public async Task GetMember_NotFound_ReturnsNotFound()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.GetMember(targetId, _userId, Arg.Any<CancellationToken>()).Returns((MemberResponseDTO?)null);

        // Act
        var result = await _controller.GetMember(targetId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetMember_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.GetMember(targetId, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetMember(targetId, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetMember_Exception_ReturnsBadRequest()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.GetMember(targetId, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetMember(targetId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PostMember_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "s1234567",
            FirstName = "Charlie",
            LastName = "Doe",
            Email = "charlie@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL
        };
        var created = new Member
        {
            Id = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "Charlie",
            LastName = "Doe",
            Email = "charlie@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _memberRepositoryMock.CreateMember(dto, _userId, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostMember(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetMember), createdResult.ActionName);
        Assert.Equal(created.Id, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostMember_Anonymous_Success_ReturnsCreated()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }; // Anonymous
        var dto = new PostMemberDTO
        {
            StudentNumber = "s1234567",
            FirstName = "Charlie",
            LastName = "Doe",
            Email = "charlie@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL
        };
        var created = new Member
        {
            Id = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "Charlie",
            LastName = "Doe",
            Email = "charlie@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _memberRepositoryMock.CreateMember(dto, null, Arg.Any<CancellationToken>()).Returns(created);

        // Act
        var result = await _controller.PostMember(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostMember_Exception_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "s1234567",
            FirstName = "Charlie",
            LastName = "Doe",
            Email = "charlie@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL
        };
        _memberRepositoryMock.CreateMember(dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PostMember(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task DeleteMember_Success_ReturnsNoContent()
    {
        // Act
        var targetId = Guid.NewGuid();
        var result = await _controller.DeleteMember(targetId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _memberRepositoryMock.Received(1).DeleteMember(targetId, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMember_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.DeleteMember(targetId, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteMember(targetId, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteMember_NotFound_ReturnsNotFound()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.DeleteMember(targetId, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.DeleteMember(targetId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteMember_Exception_ReturnsBadRequest()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.DeleteMember(targetId, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.DeleteMember(targetId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PatchMember_Success_ReturnsNoContent()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var patchDoc = new JsonPatchDocument<Member>();

        // Act
        var result = await _controller.PatchMember(targetId, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _memberRepositoryMock.Received(1).PatchMember(targetId, patchDoc, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchMember_NotFound_ReturnsNotFound()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var patchDoc = new JsonPatchDocument<Member>();
        _memberRepositoryMock.PatchMember(targetId, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.PatchMember(targetId, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PatchMember_Exception_ReturnsBadRequest()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var patchDoc = new JsonPatchDocument<Member>();
        _memberRepositoryMock.PatchMember(targetId, patchDoc, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PatchMember(targetId, patchDoc, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task PutMember_Success_ReturnsNoContent()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var dto = new MemberUpdateDTO
        {
            StudentNumber = "s1234567",
            FirstName = "Updated",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL
        };

        // Act
        var result = await _controller.PutMember(targetId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _memberRepositoryMock.Received(1).UpdateMember(targetId, dto, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutMember_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var dto = new MemberUpdateDTO
        {
            StudentNumber = "s1234567",
            FirstName = "Updated",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL
        };
        _memberRepositoryMock.UpdateMember(targetId, dto, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.PutMember(targetId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PutMember_NotFound_ReturnsNotFound()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var dto = new MemberUpdateDTO
        {
            StudentNumber = "s1234567",
            FirstName = "Updated",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL
        };
        _memberRepositoryMock.UpdateMember(targetId, dto, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.PutMember(targetId, dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PutMember_Exception_ReturnsBadRequest()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var dto = new MemberUpdateDTO
        {
            StudentNumber = "s1234567",
            FirstName = "Updated",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL
        };
        _memberRepositoryMock.UpdateMember(targetId, dto, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.PutMember(targetId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task GetProfilePicture_GroupOrPathNotFound_ReturnsNotFound()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.GetMember(targetId, _userId, Arg.Any<CancellationToken>()).Returns((MemberResponseDTO?)null);

        // Act
        var result = await _controller.GetProfilePicture(targetId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Member or profile picture not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetProfilePicture_FileNotFound_ReturnsNotFound()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var member = new MemberResponseDTO { Id = targetId, ProfilePicturePath = "some/path.png" };
        _memberRepositoryMock.GetMember(targetId, _userId, Arg.Any<CancellationToken>()).Returns(member);
        _profilePictureRepositoryMock.GetProfilePictureByPath("some/path.png").Returns(Task.FromResult<(Stream Stream, string ContentType)?>(null));

        // Act
        var result = await _controller.GetProfilePicture(targetId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("File is no longer present on the server.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetProfilePicture_FileFound_ReturnsFile()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var member = new MemberResponseDTO { Id = targetId, ProfilePicturePath = "some/path.png" };
        _memberRepositoryMock.GetMember(targetId, _userId, Arg.Any<CancellationToken>()).Returns(member);
        
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _profilePictureRepositoryMock.GetProfilePictureByPath("some/path.png").Returns(Task.FromResult<(Stream Stream, string ContentType)?>((stream, "image/png")));

        // Act
        var result = await _controller.GetProfilePicture(targetId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result.Result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(stream, fileResult.FileStream);
    }

    [Fact]
    public async Task GetProfilePicture_Exception_ReturnsBadRequest()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.GetMember(targetId, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.GetProfilePicture(targetId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task DeleteProfilePicture_Success_ReturnsNoContent()
    {
        // Arrange
        var targetId = Guid.NewGuid();

        // Act
        var result = await _controller.DeleteProfilePicture(targetId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _memberRepositoryMock.Received(1).DeleteProfilePicture(targetId, _userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProfilePicture_Unauthorized_ReturnsForbid()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.DeleteProfilePicture(targetId, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act
        var result = await _controller.DeleteProfilePicture(targetId, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteProfilePicture_NotFound_ReturnsNotFound()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.DeleteProfilePicture(targetId, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act
        var result = await _controller.DeleteProfilePicture(targetId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteProfilePicture_Exception_ReturnsBadRequest()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        _memberRepositoryMock.DeleteProfilePicture(targetId, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act
        var result = await _controller.DeleteProfilePicture(targetId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Error", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateEmailWebhook_InvalidSecret_ReturnsUnauthorized()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AUTH_WEBHOOK_SECRET", "supersecret");

        try
        {
            // Act
            var result = await _controller.UpdateEmailWebhook("wrongsecret", Guid.NewGuid(), CancellationToken.None);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid webhook secret.", unauthorized.Value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTH_WEBHOOK_SECRET", null);
        }
    }

    [Fact]
    public async Task UpdateEmailWebhook_ValidSecret_Success_ReturnsOk()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AUTH_WEBHOOK_SECRET", "supersecret");
        var targetUser = Guid.NewGuid();

        try
        {
            var controller = new MembersController(_memberRepositoryMock, _profilePictureRepositoryMock);
            // Act
            var result = await controller.UpdateEmailWebhook("supersecret", targetUser, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
            await _memberRepositoryMock.Received(1).RefreshEmail(targetUser, Arg.Any<CancellationToken>());
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTH_WEBHOOK_SECRET", null);
        }
    }

    [Fact]
    public async Task UpdateEmailWebhook_Exception_ReturnsBadRequest()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AUTH_WEBHOOK_SECRET", "supersecret");
        var targetUser = Guid.NewGuid();
        _memberRepositoryMock.RefreshEmail(targetUser, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        try
        {
            var controller = new MembersController(_memberRepositoryMock, _profilePictureRepositoryMock);
            // Act
            var result = await controller.UpdateEmailWebhook("supersecret", targetUser, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error", badRequestResult.Value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AUTH_WEBHOOK_SECRET", null);
        }
    }
}

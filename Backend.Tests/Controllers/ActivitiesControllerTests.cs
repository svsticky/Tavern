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
using Backend.Models;
using Xunit;

namespace Backend.Tests.Controllers;

public class ActivitiesControllerTests
{
    private readonly IActivityService _serviceMock;
    private readonly ActivitiesController _controller;
    private readonly Guid _userId;

    public ActivitiesControllerTests()
    {
        _serviceMock = Substitute.For<IActivityService>();
        _controller = new ActivitiesController(_serviceMock);
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
    public async Task GetActivities_Success_ReturnsOk()
    {
        // Arrange
        var dto = new GetActivitiesDTO();
        var list = new List<ActivityResponseDTO>
        {
            new ActivityResponseDTO
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
            }
        };
        _serviceMock.GetActivities(_userId, dto).Returns(list);

        // Act
        var result = await _controller.GetActivities(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<ActivityResponseDTO>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetActivities_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new GetActivitiesDTO();
        _serviceMock.GetActivities(_userId, dto).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetActivities(dto));
    }

    [Fact]
    public async Task GetActivities_Exception_ThrowsException()
    {
        // Arrange
        var dto = new GetActivitiesDTO();
        _serviceMock.GetActivities(_userId, dto).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetActivities(dto));
    }

    [Fact]
    public async Task GetActivities_TrustedOrigin_PassesAuthenticatedUserId()
    {
        // Arrange
        Environment.SetEnvironmentVariable("HostUrl", "https://tavern.example.com/");
        try
        {
            _controller.ControllerContext.HttpContext.Request.Headers.Origin = "https://tavern.example.com";
            var dto = new GetActivitiesDTO();
            _serviceMock.GetActivities(_userId, dto).Returns(new List<ActivityResponseDTO>());

            // Act
            var result = await _controller.GetActivities(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            await _serviceMock.Received(1).GetActivities(_userId, dto);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HostUrl", null);
        }
    }

    [Fact]
    public async Task GetActivities_UntrustedOrigin_PassesNullUserIdEvenIfAuthenticated()
    {
        // Arrange
        Environment.SetEnvironmentVariable("HostUrl", "https://tavern.example.com");
        try
        {
            _controller.ControllerContext.HttpContext.Request.Headers.Origin = "https://evil.example.com";
            var dto = new GetActivitiesDTO();
            _serviceMock.GetActivities(Arg.Any<Guid?>(), dto).Returns(new List<ActivityResponseDTO>());

            // Act
            var result = await _controller.GetActivities(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            await _serviceMock.Received(1).GetActivities(null, dto);
            await _serviceMock.DidNotReceive().GetActivities(_userId, dto);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HostUrl", null);
        }
    }

    [Fact]
    public async Task GetActivities_NoOriginHeader_PassesAuthenticatedUserId()
    {
        // Arrange - requests without an Origin header (e.g. same-origin/non-CORS) are always trusted.
        Environment.SetEnvironmentVariable("HostUrl", "https://tavern.example.com");
        try
        {
            var dto = new GetActivitiesDTO();
            _serviceMock.GetActivities(_userId, dto).Returns(new List<ActivityResponseDTO>());

            // Act
            var result = await _controller.GetActivities(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            await _serviceMock.Received(1).GetActivities(_userId, dto);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HostUrl", null);
        }
    }

    [Fact]
    public async Task GetActivity_Found_ReturnsOk()
    {
        // Arrange
        var response = new ActivityResponseDTO
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
        };
        _serviceMock.GetActivity(_userId, 2).Returns(response);

        // Act
        var result = await _controller.GetActivity(2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetActivity_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetActivity(_userId, 3).Returns((ActivityResponseDTO?)null);

        // Act
        var result = await _controller.GetActivity(3);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetActivity_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetActivity(_userId, 3).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetActivity(3));
    }

    [Fact]
    public async Task GetActivity_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetActivity(_userId, 3).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetActivity(3));
    }

    [Fact]
    public async Task PostActivity_Success_ReturnsCreated()
    {
        // Arrange
        var dto = new PostActivityDTO
        {
            Name = "Lunch",
            DutchDescription = "Lunch",
            EnglishDescription = "Lunch",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(1),
            Location = "Tavern",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false
        };
        var created = new Activity
        {
            Id = 10,
            Name = "Lunch",
            DutchDescription = "Lunch",
            EnglishDescription = "Lunch",
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7)
        };
        _serviceMock.CreateActivity(_userId, dto).Returns(created);

        // Act
        var result = await _controller.PostActivity(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_controller.GetActivity), createdResult.ActionName);
        Assert.Equal(10u, createdResult.RouteValues!["id"]);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task PostActivity_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostActivityDTO
        {
            Name = "Lunch",
            DutchDescription = "Lunch",
            EnglishDescription = "Lunch",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(1),
            Location = "Tavern",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false
        };
        _serviceMock.CreateActivity(_userId, dto).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostActivity(dto));
}

    [Fact]
    public async Task PostActivity_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostActivityDTO
        {
            Name = "Lunch",
            DutchDescription = "Lunch",
            EnglishDescription = "Lunch",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(1),
            Location = "Tavern",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false
        };
        _serviceMock.CreateActivity(_userId, dto).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostActivity(dto));
}

    [Fact]
    public async Task DeleteActivity_Success_ReturnsNoContent()
    {
        // Act
        var result = await _controller.DeleteActivity(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).DeleteActivity(_userId, 1);
    }

    [Fact]
    public async Task DeleteActivity_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.DeleteActivity(_userId, 1).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DeleteActivity(1));
    }

    [Fact]
    public async Task DeleteActivity_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.DeleteActivity(_userId, 1).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.DeleteActivity(1));
    }

    [Fact]
    public async Task DeleteActivity_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.DeleteActivity(_userId, 1).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteActivity(1));
    }

    [Fact]
    public async Task PatchActivity_Success_ReturnsNoContent()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Activity>();

        // Act
        var result = await _controller.PatchActivity(1, patchDoc, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).PatchActivity(_userId, 1, patchDoc, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchActivity_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Activity>();
        _serviceMock.PatchActivity(_userId, 1, patchDoc, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PatchActivity(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchActivity_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Activity>();
        _serviceMock.PatchActivity(_userId, 1, patchDoc, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PatchActivity(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchActivity_Exception_ThrowsException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Activity>();
        _serviceMock.PatchActivity(_userId, 1, patchDoc, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PatchActivity(1, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task UploadPoster_Success_ReturnsOk()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();

        // Act
        var result = await _controller.UploadPoster(1, fileMock);

        // Assert
        Assert.IsType<OkResult>(result);
        await _serviceMock.Received(1).UploadPoster(_userId, 1, fileMock);
    }

    [Fact]
    public async Task UploadPoster_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadPoster(_userId, 1, fileMock).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.UploadPoster(1, fileMock));
    }

    [Fact]
    public async Task UploadPoster_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadPoster(_userId, 1, fileMock).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.UploadPoster(1, fileMock));
    }

    [Fact]
    public async Task UploadPoster_Exception_ThrowsException()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        _serviceMock.UploadPoster(_userId, 1, fileMock).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.UploadPoster(1, fileMock));
    }

    [Fact]
    public async Task PutActivity_Success_ReturnsNoContent()
    {
        // Arrange
        var dto = new PutActivityDTO
        {
            Name = "Updated",
            DutchDescription = "Updated",
            EnglishDescription = "Updated",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(1),
            Location = "Tavern",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false
        };

        // Act
        var result = await _controller.PutActivity(1, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        await _serviceMock.Received(1).UpdateActivity(_userId, 1, dto);
    }

    [Fact]
    public async Task PutActivity_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PutActivityDTO
        {
            Name = "Updated",
            DutchDescription = "Updated",
            EnglishDescription = "Updated",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(1),
            Location = "Tavern",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false
        };
        _serviceMock.UpdateActivity(_userId, 1, dto).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PutActivity(1, dto));
}

    [Fact]
    public async Task PutActivity_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new PutActivityDTO
        {
            Name = "Updated",
            DutchDescription = "Updated",
            EnglishDescription = "Updated",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(1),
            Location = "Tavern",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false
        };
        _serviceMock.UpdateActivity(_userId, 1, dto).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.PutActivity(1, dto));
}

    [Fact]
    public async Task PutActivity_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PutActivityDTO
        {
            Name = "Updated",
            DutchDescription = "Updated",
            EnglishDescription = "Updated",
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(1),
            Location = "Tavern",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false
        };
        _serviceMock.UpdateActivity(_userId, 1, dto).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutActivity(1, dto));
}

    [Fact]
    public async Task GetPoster_Found_ReturnsFile()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _serviceMock.GetPoster(_userId, 1, download: false)
            .Returns(Task.FromResult<(Stream Stream, string ContentType, string? FileName)?>((stream, "image/webp", "poster.webp")));

        // Act
        var result = await _controller.GetPoster(1);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result.Result);
        Assert.Equal("image/webp", fileResult.ContentType);
        Assert.Equal(stream, fileResult.FileStream);
    }

    [Fact]
    public async Task GetPoster_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetPoster(_userId, 1, download: false)
            .Returns(Task.FromResult<(Stream Stream, string ContentType, string? FileName)?>(null));

        // Act
        var result = await _controller.GetPoster(1);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPoster_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetPoster(_userId, 1, download: false).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetPoster(1));
    }

    [Fact]
    public async Task GetPoster_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetPoster(_userId, 1, download: false).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetPoster(1));
    }

    [Fact]
    public async Task DownloadPoster_Found_ReturnsFile()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 4, 5 });
        _serviceMock.GetPoster(_userId, 1, download: true)
            .Returns(Task.FromResult<(Stream Stream, string ContentType, string? FileName)?>((stream, "image/png", "file.png")));

        // Act
        var result = await _controller.DownloadPoster(1);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result.Result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal("file.png", fileResult.FileDownloadName);
        Assert.Equal(stream, fileResult.FileStream);
    }

    [Fact]
    public async Task DownloadPoster_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetPoster(_userId, 1, download: true)
            .Returns(Task.FromResult<(Stream Stream, string ContentType, string? FileName)?>(null));

        // Act
        var result = await _controller.DownloadPoster(1);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DownloadPoster_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetPoster(_userId, 1, download: true).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.DownloadPoster(1));
    }

    [Fact]
    public async Task DownloadPoster_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetPoster(_userId, 1, download: true).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DownloadPoster(1));
    }

    [Fact]
    public async Task ExportEnrollments_Success_ReturnsFile()
    {
        // Arrange
        var csvContent = new byte[] { 1, 2, 3 };
        _serviceMock.GetEnrollmentsCsv(_userId, 1, Arg.Any<CancellationToken>()).Returns((csvContent, "enrollments.csv"));

        // Act
        var result = await _controller.ExportEnrollments(1, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result.Result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("enrollments.csv", fileResult.FileDownloadName);
        Assert.Equal(csvContent, fileResult.FileContents);
    }

    [Fact]
    public async Task ExportEnrollments_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetEnrollmentsCsv(_userId, 1, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.ExportEnrollments(1, CancellationToken.None));
    }

    [Fact]
    public async Task ExportEnrollments_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _serviceMock.GetEnrollmentsCsv(_userId, 1, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.ExportEnrollments(1, CancellationToken.None));
    }

    [Fact]
    public async Task ExportEnrollments_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetEnrollmentsCsv(_userId, 1, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.ExportEnrollments(1, CancellationToken.None));
    }
}

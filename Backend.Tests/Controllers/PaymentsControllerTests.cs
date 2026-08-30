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
using Backend.Services.PaymentServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly IPaymentService _serviceMock;
    private readonly PaymentsController _controller;
    private readonly Guid _userId;

    public PaymentsControllerTests()
    {
        _serviceMock = Substitute.For<IPaymentService>();
        _controller = new PaymentsController(_serviceMock);
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
    public async Task GetMembershipPayments_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<MembershipPayment> { new MembershipPayment { Id = 1, Price = 10.0m, PaymentServiceId = "service_1", PaymentIntentUrl = "https://intent.url" } };
        _serviceMock.GetMembershipPayments(_userId, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetMembershipPayments(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<MembershipPayment>>(okResult.Value);
        Assert.Single(returnedList);
    }

    [Fact]
    public async Task GetMembershipPayments_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetMembershipPayments(_userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetMembershipPayments(CancellationToken.None));
    }

    [Fact]
    public async Task GetMembershipPayments_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetMembershipPayments(_userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetMembershipPayments(CancellationToken.None));
    }

    [Fact]
    public async Task GetMembershipPayment_Found_ReturnsOk()
    {
        // Arrange
        var payment = new MembershipPayment { Id = 2, Price = 15m, PaymentServiceId = "service_2", PaymentIntentUrl = "https://intent.url" };
        _serviceMock.GetMembershipPayment(2, _userId, Arg.Any<CancellationToken>()).Returns(payment);

        // Act
        var result = await _controller.GetMembershipPayment(2, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<MembershipPayment>(okResult.Value);
        Assert.Equal(15m, returned.Price);
    }

    [Fact]
    public async Task GetMembershipPayment_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetMembershipPayment(3, _userId, Arg.Any<CancellationToken>()).Returns((MembershipPayment?)null);

        // Act
        var result = await _controller.GetMembershipPayment(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetMembershipPayment_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetMembershipPayment(3, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetMembershipPayment(3, CancellationToken.None));
    }

    [Fact]
    public async Task GetMembershipPayment_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetMembershipPayment(3, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetMembershipPayment(3, CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollmentPayments_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<EnrollmentPayment> { new EnrollmentPayment { Id = 1, Price = 5m, PaymentServiceId = "service_3", PaymentIntentUrl = "https://intent.url" } };
        _serviceMock.GetEnrollmentPayments(_userId, Arg.Any<CancellationToken>()).Returns(list);

        // Act
        var result = await _controller.GetEnrollmentPayments(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<EnrollmentPayment>>(okResult.Value);
        Assert.Single(returned);
    }

    [Fact]
    public async Task GetEnrollmentPayments_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetEnrollmentPayments(_userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetEnrollmentPayments(CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollmentPayments_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetEnrollmentPayments(_userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetEnrollmentPayments(CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollmentPayment_Found_ReturnsOk()
    {
        // Arrange
        var payment = new EnrollmentPayment { Id = 2, Price = 2.5m, PaymentServiceId = "service_4", PaymentIntentUrl = "https://intent.url" };
        _serviceMock.GetEnrollmentPayment(2, _userId, Arg.Any<CancellationToken>()).Returns(payment);

        // Act
        var result = await GetSampleEnrollmentPaymentWrapper(2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<EnrollmentPayment>(okResult.Value);
        Assert.Equal(2.5m, returned.Price);
    }

    // Helper wrapper because the method parameter differs slightly or to call it cleanly
    private Task<ActionResult<EnrollmentPayment>> GetSampleEnrollmentPaymentWrapper(uint id)
    {
        return _controller.GetEnrollmentPayment(id, CancellationToken.None);
    }

    [Fact]
    public async Task GetEnrollmentPayment_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetEnrollmentPayment(3, _userId, Arg.Any<CancellationToken>()).Returns((EnrollmentPayment?)null);

        // Act
        var result = await _controller.GetEnrollmentPayment(3, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetEnrollmentPayment_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetEnrollmentPayment(3, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetEnrollmentPayment(3, CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollmentPayment_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetEnrollmentPayment(3, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetEnrollmentPayment(3, CancellationToken.None));
    }

    [Fact]
    public async Task PostMembershipPayment_Success_ReturnsOk()
    {
        // Arrange
        var dto = new PostMembershipPaymentDTO { MemberId = Guid.NewGuid() };
        var response = new PostPaymentResponse { CheckoutUrl = "http://pay" };
        _serviceMock.CreateMembershipPayment(dto, _userId).Returns(response);

        // Act
        var result = await _controller.PostMembershipPayment(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task PostMembershipPayment_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostMembershipPaymentDTO { MemberId = Guid.NewGuid() };
        _serviceMock.CreateMembershipPayment(dto, _userId).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostMembershipPayment(dto));
    }

    [Fact]
    public async Task PostActivityPayment_Success_ReturnsOk()
    {
        // Arrange
        var dto = new PostActivityPaymentDTO { MemberId = Guid.NewGuid(), ActivityIds = new List<uint> { 5 } };
        var response = new PostPaymentResponse { CheckoutUrl = "http://pay" };
        _serviceMock.CreateActivityPayment(dto, _userId).Returns(response);

        // Act
        var result = await _controller.PostActivityPayment(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task PostActivityPayment_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostActivityPaymentDTO { MemberId = Guid.NewGuid(), ActivityIds = new List<uint> { 5 } };
        _serviceMock.CreateActivityPayment(dto, _userId).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostActivityPayment(dto));
    }

    [Fact]
    public async Task PostActivityPayment_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostActivityPaymentDTO { MemberId = Guid.NewGuid(), ActivityIds = new List<uint> { 5 } };
        _serviceMock.CreateActivityPayment(dto, _userId).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostActivityPayment(dto));
    }

    [Fact]
    public async Task PostBegunstigerPayment_Success_ReturnsOk()
    {
        // Arrange
        var dto = new PostBegunstigerPaymentDTO { MemberId = Guid.NewGuid() };
        var response = new PostPaymentResponse { CheckoutUrl = "http://pay" };
        _serviceMock.CreateBegunstigerPayment(dto, _userId).Returns(response);

        // Act
        var result = await _controller.PostBegunstigerPayment(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task PostBegunstigerPayment_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var dto = new PostBegunstigerPaymentDTO { MemberId = Guid.NewGuid() };
        _serviceMock.CreateBegunstigerPayment(dto, _userId).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.PostBegunstigerPayment(dto));
    }

    [Fact]
    public async Task PostBegunstigerPayment_Exception_ThrowsException()
    {
        // Arrange
        var dto = new PostBegunstigerPaymentDTO { MemberId = Guid.NewGuid() };
        _serviceMock.CreateBegunstigerPayment(dto, _userId).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostBegunstigerPayment(dto));
    }

    [Fact]
    public async Task PaymentWebhook_Success_ReturnsOk()
    {
        // Arrange
        var webhookService = Substitute.For<AbstractPaymentService>(null, null);

        // Act
        var result = await _controller.PaymentWebhook("tr_123", webhookService);

        // Assert
        Assert.IsType<OkResult>(result);
        await webhookService.Received(1).HandleWebhookAsync("tr_123");
    }

    [Fact]
    public async Task PaymentWebhook_Exception_ThrowsException()
    {
        // Arrange
        var webhookService = Substitute.For<AbstractPaymentService>(null, null);
        webhookService.HandleWebhookAsync("tr_123").Throws(new Exception("Webhook failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PaymentWebhook("tr_123", webhookService));
    }

    [Fact]
    public void GetUnpaid_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<EnrollmentBalance> { new EnrollmentBalance { Enrollment = new Enrollment { ActivityId = 1 }, Balance = 100 } };
        _serviceMock.GetUnpaid(_userId, false).Returns(list);

        // Act
        var result = _controller.GetUnpaid(false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(list, okResult.Value);
    }

    [Fact]
    public void GetUnpaid_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetUnpaid(_userId, false).Returns((IEnumerable<EnrollmentBalance>?)null);

        // Act
        var result = _controller.GetUnpaid(false);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void GetUnpaid_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetUnpaid(_userId, false).Throws(new UnauthorizedAccessException());

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => _controller.GetUnpaid(false));
    }

    [Fact]
    public void GetUnpaid_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetUnpaid(_userId, false).Throws(new Exception("Error"));

        // Act & Assert
        Assert.Throws<Exception>(() => _controller.GetUnpaid(false));
    }

    [Fact]
    public void GetOverpaid_Success_ReturnsOk()
    {
        // Arrange
        var list = new List<EnrollmentBalance> { new EnrollmentBalance { Enrollment = new Enrollment { ActivityId = 1 }, Balance = -50 } };
        _serviceMock.GetOverpaid(_userId).Returns(list);

        // Act
        var result = _controller.GetOverpaid();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(list, okResult.Value);
    }

    [Fact]
    public void GetOverpaid_NotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.GetOverpaid(_userId).Returns((IEnumerable<EnrollmentBalance>?)null);

        // Act
        var result = _controller.GetOverpaid();

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void GetOverpaid_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _serviceMock.GetOverpaid(_userId).Throws(new UnauthorizedAccessException());

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => _controller.GetOverpaid());
    }

    [Fact]
    public void GetOverpaid_Exception_ThrowsException()
    {
        // Arrange
        _serviceMock.GetOverpaid(_userId).Throws(new Exception("Error"));

        // Act & Assert
        Assert.Throws<Exception>(() => _controller.GetOverpaid());
    }

    [Fact]
    public async Task GetMemberPaymentStatus_Success_ReturnsOk()
    {
        // Arrange
        var targetUser = Guid.NewGuid();
        var response = new PaymentStatusResponse
        {
            MemberId = targetUser,
            HasEverPaidMembership = true,
            HasPaidMembershipBeforeExpirationTime = true,
            HasPaidAllActivities = true,
            IsBegunstiger = false,
            CanPayMembership = true,
            UnpaidEnrollments = new List<EnrollmentBalance>()
        };
        _serviceMock.GetMemberPaymentStatus(targetUser, _userId, Arg.Any<CancellationToken>()).Returns(response);

        // Act
        var result = await _controller.GetMemberPaymentStatus(targetUser, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetMemberPaymentStatus_NotFound_ReturnsNotFound()
    {
        // Arrange
        var targetUser = Guid.NewGuid();
        _serviceMock.GetMemberPaymentStatus(targetUser, _userId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<PaymentStatusResponse>(null!));

        // Act
        var result = await _controller.GetMemberPaymentStatus(targetUser, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetMemberPaymentStatus_KeyNotFoundException_ThrowsKeyNotFoundException()
    {
        // Arrange
        var targetUser = Guid.NewGuid();
        _serviceMock.GetMemberPaymentStatus(targetUser, _userId, Arg.Any<CancellationToken>()).Throws(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.GetMemberPaymentStatus(targetUser, CancellationToken.None));
    }

    [Fact]
    public async Task GetMemberPaymentStatus_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var targetUser = Guid.NewGuid();
        _serviceMock.GetMemberPaymentStatus(targetUser, _userId, Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetMemberPaymentStatus(targetUser, CancellationToken.None));
    }

    [Fact]
    public async Task GetMemberPaymentStatus_Exception_ThrowsException()
    {
        // Arrange
        var targetUser = Guid.NewGuid();
        _serviceMock.GetMemberPaymentStatus(targetUser, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetMemberPaymentStatus(targetUser, CancellationToken.None));
    }

    [Fact]
    public async Task ExportPaymentsToCsv_Success_ReturnsFile()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(-7);
        var end = DateTime.UtcNow;
        var csvContent = new byte[] { 1, 2, 3 };
        _serviceMock.ExportPaymentsToCsv(start, end, _userId, Arg.Any<CancellationToken>()).Returns((csvContent, "export.csv"));

        // Act
        var result = await _controller.ExportPaymentsToCsv(start, end, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result.Result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("export.csv", fileResult.FileDownloadName);
        Assert.Equal(csvContent, fileResult.FileContents);
    }

    [Fact]
    public async Task ExportPaymentsToCsv_Exception_ThrowsException()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(-7);
        var end = DateTime.UtcNow;
        _serviceMock.ExportPaymentsToCsv(start, end, _userId, Arg.Any<CancellationToken>()).Throws(new Exception("Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.ExportPaymentsToCsv(start, end, CancellationToken.None));
    }
}

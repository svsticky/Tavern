using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Backend.Services;
using Backend.Services.PaymentServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class TestUintSequenceValueGenerator : Microsoft.EntityFrameworkCore.ValueGeneration.ValueGenerator
{
    private int _current = 1000;

    public override bool GeneratesTemporaryValues => false;

    protected override object? NextValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        return (uint)System.Threading.Interlocked.Increment(ref _current);
    }
}

public class PaymentTestPostgresDbContext : PostgresDbContext
{
    public PaymentTestPostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>()
            .Property(p => p.Id)
            .HasValueGenerator<TestUintSequenceValueGenerator>();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));
            foreach (var property in properties)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter());
            }
        }
    }
}

public class PaymentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IPaymentValidationService _paymentValidationService;
    private readonly AbstractPaymentService _paymentService;
    private readonly AuthOutboxWorker _authOutboxWorker;
    private readonly PaymentService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public PaymentServiceTests()
    {
        Environment.SetEnvironmentVariable("HostUrl", "http://localhost:3000");
        Environment.SetEnvironmentVariable("ApiUrl", "http://localhost:5000");

        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PaymentTestPostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _paymentValidationService = Substitute.For<IPaymentValidationService>();
        // Default to "has done or is doing a study" so existing membership payment tests (which
        // aren't about eligibility) don't all need to stub this. Tests for the eligibility guard
        // itself override it.
        _paymentValidationService.HasDoneOrDoingStudy(Arg.Any<Guid>()).Returns(true);
        _paymentService = Substitute.For<AbstractPaymentService>(_db, NullLogger<AbstractPaymentService>.Instance);

        var serviceProvider = Substitute.For<IServiceProvider>();
        _authOutboxWorker = Substitute.For<AuthOutboxWorker>(serviceProvider, NullLogger<AuthOutboxWorker>.Instance);

        _service = new PaymentService(
            _db,
            _permissionService,
            _paymentValidationService,
            _paymentService,
            _authOutboxWorker,
            NullLogger<PaymentService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Member CreateMember(string studentNumber)
    {
        return new Member
        {
            Id = Guid.NewGuid(),
            StudentNumber = studentNumber,
            FirstName = "John",
            LastName = "Doe",
            Email = $"john-{Guid.NewGuid()}@example.com",
            PhoneNumber = "0612345678",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "City",
            DateOfBirth = new DateTime(2000, 1, 1),
            Suspended = false,
            PreferredLanguage = Language.EN,
            AuthSystemUserId = Guid.NewGuid()
        };
    }

    [Fact]
    public async Task GetMembershipPayments_EnforcesBoardPermissionAndReturnsList()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment { MemberId = member.Id, Price = 7.50m, PaymentServiceId = "ps1", PaymentIntentUrl = "url" };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        var result = await _service.GetMembershipPayments(_userId, CancellationToken.None);

        Assert.Single(result);
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ViewFinances, Arg.Any<uint?>());
    }

    [Fact]
    public async Task GetMembershipPayment_EnforcesBoardPermissionAndReturnsPayment()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment { MemberId = member.Id, Price = 7.50m, PaymentServiceId = "ps1", PaymentIntentUrl = "url" };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        var result = await _service.GetMembershipPayment(payment.Id, _userId, CancellationToken.None);

        Assert.NotNull(result);
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ViewFinances, Arg.Any<uint?>());
    }

    [Fact]
    public async Task GetEnrollmentPayments_EnforcesBoardPermissionAndReturnsList()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new EnrollmentPayment { MemberId = member.Id, Price = 10m, PaymentServiceId = "ps1", PaymentIntentUrl = "url" };
        _db.EnrollmentPayments.Add(payment);
        await _db.SaveChangesAsync();

        var result = await _service.GetEnrollmentPayments(_userId, CancellationToken.None);

        Assert.Single(result);
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ViewFinances, Arg.Any<uint?>());
    }

    [Fact]
    public async Task GetEnrollmentPayment_EnforcesBoardPermissionAndReturnsPayment()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new EnrollmentPayment { MemberId = member.Id, Price = 10m, PaymentServiceId = "ps1", PaymentIntentUrl = "url" };
        _db.EnrollmentPayments.Add(payment);
        await _db.SaveChangesAsync();

        var result = await _service.GetEnrollmentPayment(payment.Id, _userId, CancellationToken.None);

        Assert.NotNull(result);
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ViewFinances, Arg.Any<uint?>());
    }

    [Fact]
    public async Task CreateMembershipPayment_AlreadyPaid_ThrowsInvalidOperationException()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMembershipPayment(dto, null));
    }

    [Fact]
    public async Task CreateMembershipPayment_NotBegunstigerAndNoStudy_ThrowsInvalidOperationException()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);
        _paymentValidationService.HasDoneOrDoingStudy(member.Id).Returns(false);

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMembershipPayment(dto, null));
        Assert.Equal("Member is not eligible to pay membership", exception.Message);

        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.MembershipPayments.Where(p => p.MemberId == member.Id).ToListAsync());
    }

    [Fact]
    public async Task CreateMembershipPayment_Begunstiger_ThrowsInvalidOperationExceptionEvenWithStudy()
    {
        // Begunstigers must always pay through CreateBegunstigerPayment, regardless of study status -
        // otherwise they could use the (cheaper) membership endpoint to underpay.
        var member = CreateMember("1234567");
        member.Begunstiger = true;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);
        _paymentValidationService.HasDoneOrDoingStudy(member.Id).Returns(true);

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMembershipPayment(dto, null));
        Assert.Equal("Member is not eligible to pay membership", exception.Message);

        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.MembershipPayments.Where(p => p.MemberId == member.Id).ToListAsync());
    }

    [Fact]
    public async Task CreateMembershipPayment_PendingPaymentExists_CancelsItAndCreatesNewPayment()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment { MemberId = member.Id, Price = 7.50m, PaymentServiceId = "pending_id", PaymentIntentUrl = "pending_url" };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);

        _paymentService.GetPaymentAsync("pending_id")
            .Returns(Task.FromResult(new GetPaymentResponse("pending_id", PaymentStatus.Pending, null)));

        _paymentService.CreatePaymentAsync(7.50m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("new_id", "new_url")));

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        var result = await _service.CreateMembershipPayment(dto, null);

        Assert.Equal("new_url", result.CheckoutUrl);
        await _paymentService.Received(1).CancelPaymentAsync("pending_id");

        _db.ChangeTracker.Clear();
        var payments = await _db.MembershipPayments.Where(p => p.MemberId == member.Id).ToListAsync();
        Assert.Single(payments);
        Assert.Equal("new_id", payments[0].PaymentServiceId);
    }

    [Fact]
    public async Task CreateMembershipPayment_ExistingUnconfirmedPaymentAlreadyPaidAtMollie_ThrowsInsteadOfDoubleCharging()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment { MemberId = member.Id, Price = 7.50m, PaymentServiceId = "paid_id", PaymentIntentUrl = "paid_url" };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        // First call (top-level gate, before HandleExistingMembershipPayment) simulates the webhook
        // not having landed yet; second call (after self-healing PaidAt) simulates the payment now
        // being visible as within the current window, which should reject the duplicate attempt.
        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false, true);

        _paymentService.GetPaymentAsync("paid_id")
            .Returns(Task.FromResult(new GetPaymentResponse("paid_id", PaymentStatus.Paid, DateTimeOffset.UtcNow)));

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMembershipPayment(dto, null));

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, member.Id, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task CreateMembershipPayment_ExpiredPayment_DoesNotDeleteMemberAndCreatesNewPayment()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment { MemberId = member.Id, Price = 7.50m, PaymentServiceId = "expired_id", PaymentIntentUrl = "expired_url" };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);

        _paymentService.GetPaymentAsync("expired_id")
            .Returns(Task.FromResult(new GetPaymentResponse("expired_id", PaymentStatus.Failed, null)));

        _paymentService.CreatePaymentAsync(7.50m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("new_id", "new_url")));

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        var result = await _service.CreateMembershipPayment(dto, null);

        Assert.Equal("new_url", result.CheckoutUrl);

        _db.ChangeTracker.Clear();
        var stillExistingMember = await _db.Members.FindAsync(member.Id);
        Assert.NotNull(stillExistingMember);

        _authOutboxWorker.DidNotReceive().EnqueueTask(AuthTaskType.Delete, Arg.Any<Guid>(), Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task CreateMembershipPayment_NewPayment_CreatesPaymentRequest()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);

        _paymentService.CreatePaymentAsync(7.50m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("new_id", "new_url")));

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        var result = await _service.CreateMembershipPayment(dto, null);

        Assert.Equal("new_url", result.CheckoutUrl);

        _db.ChangeTracker.Clear();
        var created = await _db.MembershipPayments.FirstOrDefaultAsync(p => p.MemberId == member.Id);
        Assert.NotNull(created);
        Assert.Equal("new_id", created.PaymentServiceId);
        Assert.False(created.ManuallyMarkedAsPaid);
    }

    [Fact]
    public async Task CreateMembershipPayment_MemberNeverActivated_RedirectsToConfirmMail()
    {
        var member = CreateMember("1234567");
        member.ActivationEmailSentAt = null;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);

        _paymentService.CreatePaymentAsync(7.50m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("new_id", "new_url")));

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        await _service.CreateMembershipPayment(dto, null);

        await _paymentService.Received(1).CreatePaymentAsync(
            7.50m,
            Arg.Any<string>(),
            $"http://localhost:3000/confirm-mail?memberId={member.Id}",
            Arg.Any<string>(),
            Arg.Any<string>()
        );
    }

    [Fact]
    public async Task CreateMembershipPayment_MemberAlreadyActivated_RedirectsBackToApp()
    {
        var member = CreateMember("1234567");
        member.ActivationEmailSentAt = DateTimeOffset.UtcNow;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);

        _paymentService.CreatePaymentAsync(7.50m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("new_id", "new_url")));

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        await _service.CreateMembershipPayment(dto, null);

        await _paymentService.Received(1).CreatePaymentAsync(
            7.50m,
            Arg.Any<string>(),
            "http://localhost:3000",
            Arg.Any<string>(),
            Arg.Any<string>()
        );
    }

    [Fact]
    public async Task CreateMembershipPayment_Manual_CreatesPaidMembershipPayment()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id, ManuallyMarkedAsPaid = true };

        var result = await _service.CreateMembershipPayment(dto, _userId);

        Assert.Null(result.CheckoutUrl);
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ManageFinances, Arg.Any<uint?>());
        await _paymentService.DidNotReceive().CreatePaymentAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>());

        _db.ChangeTracker.Clear();
        var payment = await _db.MembershipPayments.FirstOrDefaultAsync(p => p.MemberId == member.Id);
        Assert.NotNull(payment);
        Assert.True(payment.PaidAt.HasValue);
        Assert.Equal("", payment.PaymentServiceId);
        Assert.True(payment.ManuallyMarkedAsPaid);

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, member.Id, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task CreateMembershipPayment_ManualWithoutAuthentication_ThrowsUnauthorizedAccessException()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id, ManuallyMarkedAsPaid = true };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateMembershipPayment(dto, null));

        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.MembershipPayments.Where(p => p.MemberId == member.Id).ToListAsync());
    }

    [Fact]
    public async Task CreateBegunstigerPayment_SelfPayAsBegunstiger_CreatesPaymentRequest()
    {
        var member = CreateMember("1234567");
        member.Begunstiger = true;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id).Returns(false);
        _paymentService.CreatePaymentAsync(10.00m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("new_id", "new_url")));

        var dto = new PostBegunstigerPaymentDTO { MemberId = member.Id };

        var result = await _service.CreateBegunstigerPayment(dto, member.Id);

        Assert.Equal("new_url", result.CheckoutUrl);
        _permissionService.DidNotReceive().EnsureBoardOrCandidateBoardMember(Arg.Any<Guid>());

        _db.ChangeTracker.Clear();
        var created = await _db.BegunstigerPayments.FirstOrDefaultAsync(p => p.MemberId == member.Id);
        Assert.NotNull(created);
        Assert.Equal("new_id", created.PaymentServiceId);
    }

    [Fact]
    public async Task CreateBegunstigerPayment_SelfPayButNotFlaggedBegunstiger_ThrowsInvalidOperationExceptionWithoutPermissionCheck()
    {
        var member = CreateMember("1234567");
        member.Begunstiger = false;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id).Returns(false);

        var dto = new PostBegunstigerPaymentDTO { MemberId = member.Id };

        // Self-pay (userId == dto.MemberId) skips the board-permission check entirely; a non-begunstiger
        // is instead rejected by the eligibility guard - a begunstiger fee can never be created for a
        // non-begunstiger member, whoever is asking.
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateBegunstigerPayment(dto, member.Id));
        Assert.Equal("Member is not a begunstiger", exception.Message);

        _permissionService.DidNotReceive().EnsureBoardOrCandidateBoardMember(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateBegunstigerPayment_BoardMemberOnBehalfOfNonBegunstiger_ThrowsInvalidOperationException()
    {
        var member = CreateMember("1234567");
        member.Begunstiger = false;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var dto = new PostBegunstigerPaymentDTO { MemberId = member.Id, ManuallyMarkedAsPaid = true };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateBegunstigerPayment(dto, _userId));
        Assert.Equal("Member is not a begunstiger", exception.Message);

        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.BegunstigerPayments.Where(p => p.MemberId == member.Id).ToListAsync());
    }

    [Fact]
    public async Task CreateBegunstigerPayment_BoardMemberOnBehalf_Manual_CreatesPaidPayment()
    {
        var member = CreateMember("1234567");
        member.Begunstiger = true;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id).Returns(false);

        var dto = new PostBegunstigerPaymentDTO { MemberId = member.Id, ManuallyMarkedAsPaid = true };

        var result = await _service.CreateBegunstigerPayment(dto, _userId);

        Assert.Null(result.CheckoutUrl);
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ManageFinances, Arg.Any<uint?>());
        await _paymentService.DidNotReceive().CreatePaymentAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>());

        _db.ChangeTracker.Clear();
        var payment = await _db.BegunstigerPayments.FirstOrDefaultAsync(p => p.MemberId == member.Id);
        Assert.NotNull(payment);
        Assert.True(payment.PaidAt.HasValue);
        Assert.Equal("", payment.PaymentServiceId);
        Assert.True(payment.ManuallyMarkedAsPaid);

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, member.Id, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task CreateBegunstigerPayment_PendingPaymentExists_CancelsItAndCreatesNewPayment()
    {
        var member = CreateMember("1234567");
        member.Begunstiger = true;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new BegunstigerPayment { MemberId = member.Id, Price = 10.00m, PaymentServiceId = "pending_id", PaymentIntentUrl = "pending_url" };
        _db.BegunstigerPayments.Add(payment);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id).Returns(false);

        _paymentService.GetPaymentAsync("pending_id")
            .Returns(Task.FromResult(new GetPaymentResponse("pending_id", PaymentStatus.Pending, null)));

        _paymentService.CreatePaymentAsync(10.00m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("new_id", "new_url")));

        var dto = new PostBegunstigerPaymentDTO { MemberId = member.Id };

        var result = await _service.CreateBegunstigerPayment(dto, member.Id);

        Assert.Equal("new_url", result.CheckoutUrl);
        await _paymentService.Received(1).CancelPaymentAsync("pending_id");

        _db.ChangeTracker.Clear();
        var payments = await _db.BegunstigerPayments.Where(p => p.MemberId == member.Id).ToListAsync();
        Assert.Single(payments);
        Assert.Equal("new_id", payments[0].PaymentServiceId);
    }

    [Fact]
    public async Task CreateBegunstigerPayment_ExistingUnconfirmedPaymentAlreadyPaidAtMollie_ThrowsInsteadOfDoubleCharging()
    {
        var member = CreateMember("1234567");
        member.Begunstiger = true;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new BegunstigerPayment { MemberId = member.Id, Price = 10.00m, PaymentServiceId = "paid_id", PaymentIntentUrl = "paid_url" };
        _db.BegunstigerPayments.Add(payment);
        await _db.SaveChangesAsync();

        // First call (top-level gate) simulates the webhook not having landed yet; second call (after
        // self-healing PaidAt) simulates the payment now being visible, which should reject the duplicate.
        _paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id).Returns(false, true);

        _paymentService.GetPaymentAsync("paid_id")
            .Returns(Task.FromResult(new GetPaymentResponse("paid_id", PaymentStatus.Paid, DateTimeOffset.UtcNow)));

        var dto = new PostBegunstigerPaymentDTO { MemberId = member.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateBegunstigerPayment(dto, member.Id));

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, member.Id, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task CreateBegunstigerPayment_AlreadyPaidSinceLastBoardChange_ThrowsInvalidOperationException()
    {
        var member = CreateMember("1234567");
        member.Begunstiger = true;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id).Returns(true);

        var dto = new PostBegunstigerPaymentDTO { MemberId = member.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateBegunstigerPayment(dto, member.Id));
    }

    [Fact]
    public async Task CreateBegunstigerPayment_ManualWithoutAuthentication_ThrowsUnauthorizedAccessException()
    {
        var member = CreateMember("1234567");
        member.Begunstiger = true;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var dto = new PostBegunstigerPaymentDTO { MemberId = member.Id, ManuallyMarkedAsPaid = true };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateBegunstigerPayment(dto, null));

        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.BegunstigerPayments.Where(p => p.MemberId == member.Id).ToListAsync());
    }

    [Fact]
    public async Task ExportPaymentsToCsv_GeneratesValidCsv()
    {
        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        _db.Settings.Add(new Setting { Name = "PaymentServicePaymentsCondition", Value = "2" });
        _db.Settings.Add(new Setting { Name = "PaymentServiceRelationCode", Value = "473" });
        _db.Settings.Add(new Setting { Name = "MembershipGLAccount", Value = "8000" });
        _db.Settings.Add(new Setting { Name = "MembershipVATCode", Value = "0" });
        _db.Settings.Add(new Setting { Name = "ActivityGLAccount", Value = "7001" });
        _db.Settings.Add(new Setting { Name = "PaymentServiceFeeGLAccount", Value = "5007" });
        _db.Settings.Add(new Setting { Name = "PaymentServiceFeeCostCenter", Value = "TRX" });
        _db.Settings.Add(new Setting { Name = "PaymentServiceFeeVATCode", Value = "21" });
        _db.Settings.Add(new Setting { Name = "BegunstigerGLAccount", Value = "8010" });
        _db.Settings.Add(new Setting { Name = "BegunstigerVATCode", Value = "0" });
        _db.Settings.Add(new Setting { Name = "BegunstigerCostCenter", Value = "BEG" });
        _db.Settings.Add(new Setting { Name = "BegunstigerCostUnit", Value = "BU1" });
        await _db.SaveChangesAsync();

        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment { MemberId = member.Id, Price = 7.50m, PaymentServiceId = "ps1", PaymentIntentUrl = "url", PaidAt = DateTime.UtcNow };
        _db.MembershipPayments.Add(payment);

        var organizer = new Group
        {
            Name = "Test Organizer",
            DefaultGLAccount = "7001",
            DefaultCostCenter = "CC1",
            Active = true,
            Type = GroupType.Committee
        };
        _db.Groups.Add(organizer);

        var activity = new Activity
        {
            Name = "Test Activity",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5),
            Organizer = organizer,
            VatRate = 21
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollmentPayment = new EnrollmentPayment
        {
            MemberId = member.Id,
            ActivityId = activity.Id,
            Price = 15m,
            PaymentServiceId = "ps2",
            PaymentIntentUrl = "url2",
            PaidAt = DateTime.UtcNow,
            ManuallyMarkedAsPaid = false
        };
        _db.EnrollmentPayments.Add(enrollmentPayment);

        var feePayment = new PaymentServiceFeePayment
        {
            MemberId = member.Id,
            Price = 0.50m,
            PaymentServiceId = "ps3",
            PaymentIntentUrl = "url3",
            PaidAt = DateTime.UtcNow,
            ManuallyMarkedAsPaid = false
        };
        _db.PaymentServiceFeePayments.Add(feePayment);

        var begunstigerPayment = new BegunstigerPayment
        {
            MemberId = member.Id,
            Price = 10.00m,
            PaymentServiceId = "ps4",
            PaymentIntentUrl = "url4",
            PaidAt = DateTime.UtcNow,
            ManuallyMarkedAsPaid = false
        };
        _db.BegunstigerPayments.Add(begunstigerPayment);
        await _db.SaveChangesAsync();

        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);

        var result = await _service.ExportPaymentsToCsv(startDate, endDate, _userId, CancellationToken.None);

        Assert.NotNull(result.Content);
        var csvStr = Encoding.UTF8.GetString(result.Content);
        Assert.Contains(";8000;Lidmaatschap;0;7.50;;", csvStr);
        Assert.Contains("Test Organizer | Test Activity", csvStr);
        Assert.Contains("Transaction costs 0.50 x 1", csvStr);
        Assert.Contains(";8010;Begunstiger;0;10.00;BEG;BU1", csvStr);
    }

    [Fact]
    public async Task CreateActivityPayment_Manual_CreatesPaidEnrollmentPayments()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);

        var activity = new Activity
        {
            Name = "Act",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5)
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 15m, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidAmountForEnrollment(Arg.Is<Enrollment>(e => e.ActivityId == activity.Id)).Returns(15m);

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PostActivityPaymentDTO
        {
            MemberId = member.Id,
            ActivityIds = new List<uint> { activity.Id },
            ManuallyMarkedAsPaid = true
        };

        var result = await _service.CreateActivityPayment(dto, _userId);

        Assert.Null(result.CheckoutUrl);

        _db.ChangeTracker.Clear();
        var payment = await _db.EnrollmentPayments.FirstOrDefaultAsync(p => p.MemberId == member.Id && p.ActivityId == activity.Id);
        Assert.NotNull(payment);
        Assert.True(payment.PaidAt.HasValue);
    }

    [Fact]
    public async Task CreateActivityPayment_Online_CreatesPaymentServiceFeeRequest()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);

        var activity = new Activity
        {
            Name = "Act",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5)
        };
        _db.Activities.Add(activity);

        _db.Settings.Add(new Setting { Name = "PaymentServiceFee", Value = "0.50" });
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 15m, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidAmountForEnrollment(Arg.Is<Enrollment>(e => e.ActivityId == activity.Id)).Returns(15m);

        _paymentService.CreatePaymentAsync(15.50m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("act_id", "act_url")));

        var dto = new PostActivityPaymentDTO
        {
            MemberId = member.Id,
            ActivityIds = new List<uint> { activity.Id },
            ManuallyMarkedAsPaid = false
        };

        var result = await _service.CreateActivityPayment(dto, member.Id);

        Assert.Equal("act_url", result.CheckoutUrl);

        _db.ChangeTracker.Clear();
        var p = await _db.EnrollmentPayments.FirstOrDefaultAsync(x => x.MemberId == member.Id);
        Assert.NotNull(p);
        Assert.Equal("act_id", p.PaymentServiceId);

        var feePayment = await _db.PaymentServiceFeePayments.FirstOrDefaultAsync(x => x.MemberId == member.Id);
        Assert.NotNull(feePayment);
        Assert.Equal(0.50m, feePayment.Price);
    }

    [Fact]
    public async Task CreateActivityPayment_ExistingPendingPayment_CancelsItAndCreatesFreshPayment()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);

        var activity = new Activity
        {
            Name = "Act",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5)
        };
        _db.Activities.Add(activity);

        _db.Settings.Add(new Setting { Name = "PaymentServiceFee", Value = "0.50" });
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 15m, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);

        var pendingPayment = new EnrollmentPayment
        {
            MemberId = member.Id,
            ActivityId = activity.Id,
            Price = 15m,
            PaymentServiceId = "pending_ps",
            PaymentIntentUrl = "pending_checkout_url",
            PaidAt = null
        };
        _db.EnrollmentPayments.Add(pendingPayment);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidAmountForEnrollment(Arg.Is<Enrollment>(e => e.ActivityId == activity.Id)).Returns(15m);

        _paymentService.GetPaymentAsync("pending_ps")
            .Returns(Task.FromResult(new GetPaymentResponse("pending_ps", PaymentStatus.Pending, null)));

        _paymentService.CreatePaymentAsync(15.50m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("new_ps", "new_checkout_url")));

        var dto = new PostActivityPaymentDTO
        {
            MemberId = member.Id,
            ActivityIds = new List<uint> { activity.Id },
            ManuallyMarkedAsPaid = false
        };

        var result = await _service.CreateActivityPayment(dto, member.Id);

        Assert.Equal("new_checkout_url", result.CheckoutUrl);
        await _paymentService.Received(1).CancelPaymentAsync("pending_ps");

        _db.ChangeTracker.Clear();
        var payments = await _db.EnrollmentPayments.Where(p => p.MemberId == member.Id).ToListAsync();
        Assert.Single(payments);
        Assert.Equal("new_ps", payments[0].PaymentServiceId);
    }

    [Fact]
    public async Task CreateActivityPayment_ExistingPaymentAlreadyPaidAtMollie_SelfHealsWithoutDoubleCharging()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);

        var activity = new Activity
        {
            Name = "Act",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5)
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 15m, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);

        var pendingPayment = new EnrollmentPayment
        {
            MemberId = member.Id,
            ActivityId = activity.Id,
            Price = 15m,
            PaymentServiceId = "paid_ps",
            PaymentIntentUrl = "paid_checkout_url",
            PaidAt = null
        };
        _db.EnrollmentPayments.Add(pendingPayment);

        var pendingFeePayment = new PaymentServiceFeePayment
        {
            MemberId = member.Id,
            Price = 0.50m,
            PaymentServiceId = "paid_ps",
            PaymentIntentUrl = "paid_checkout_url",
            PaidAt = null
        };
        _db.PaymentServiceFeePayments.Add(pendingFeePayment);
        await _db.SaveChangesAsync();

        // After self-healing, the €15 payment fully covers the €15 enrollment, so nothing remains owed.
        _paymentValidationService.GetUnpaidAmountForEnrollment(Arg.Is<Enrollment>(e => e.ActivityId == activity.Id)).Returns(0m);

        var paidAt = DateTimeOffset.UtcNow;
        _paymentService.GetPaymentAsync("paid_ps")
            .Returns(Task.FromResult(new GetPaymentResponse("paid_ps", PaymentStatus.Paid, paidAt)));

        var dto = new PostActivityPaymentDTO
        {
            MemberId = member.Id,
            ActivityIds = new List<uint> { activity.Id },
            ManuallyMarkedAsPaid = false
        };

        var result = await _service.CreateActivityPayment(dto, member.Id);

        Assert.Null(result.CheckoutUrl);
        await _paymentService.DidNotReceive().CreatePaymentAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

        _db.ChangeTracker.Clear();
        var enrollmentPayment = await _db.EnrollmentPayments.FirstAsync(p => p.MemberId == member.Id);
        Assert.True(enrollmentPayment.PaidAt.HasValue);

        var feePayment = await _db.PaymentServiceFeePayments.FirstAsync(p => p.MemberId == member.Id);
        Assert.True(feePayment.PaidAt.HasValue);
    }

    [Fact]
    public async Task CreateActivityPayment_ExistingPaymentExpiredAtMollie_CreatesFreshPayment()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);

        var activity = new Activity
        {
            Name = "Act",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5)
        };
        _db.Activities.Add(activity);

        _db.Settings.Add(new Setting { Name = "PaymentServiceFee", Value = "0.50" });
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 15m, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);

        var stalePayment = new EnrollmentPayment
        {
            MemberId = member.Id,
            ActivityId = activity.Id,
            Price = 15m,
            PaymentServiceId = "expired_ps",
            PaymentIntentUrl = "expired_checkout_url",
            PaidAt = null
        };
        _db.EnrollmentPayments.Add(stalePayment);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidAmountForEnrollment(Arg.Is<Enrollment>(e => e.ActivityId == activity.Id)).Returns(15m);

        _paymentService.GetPaymentAsync("expired_ps")
            .Returns(Task.FromResult(new GetPaymentResponse("expired_ps", PaymentStatus.Failed, null)));

        _paymentService.CreatePaymentAsync(15.50m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(new CreatePaymentResponse("new_ps", "new_checkout_url")));

        var dto = new PostActivityPaymentDTO
        {
            MemberId = member.Id,
            ActivityIds = new List<uint> { activity.Id },
            ManuallyMarkedAsPaid = false
        };

        var result = await _service.CreateActivityPayment(dto, member.Id);

        Assert.Equal("new_checkout_url", result.CheckoutUrl);

        _db.ChangeTracker.Clear();
        var payments = await _db.EnrollmentPayments.Where(p => p.MemberId == member.Id).ToListAsync();
        Assert.Equal(2, payments.Count);
        Assert.Contains(payments, p => p.PaymentServiceId == "new_ps");
    }

    [Fact]
    public void GetUnpaid_ReturnsUnpaidEnrollments()
    {
        var enrollment = new Enrollment { MemberId = _userId, ActivityId = 1, Price = 10 };
        var unpaidList = new List<EnrollmentBalance>
        {
            new EnrollmentBalance { Enrollment = enrollment, Balance = 10m }
        };
        _paymentValidationService.GetUnpaidEnrollmentsForMember(_userId).Returns(unpaidList);

        var result = _service.GetUnpaid(_userId, false);

        Assert.Single(result);
    }

    [Fact]
    public void GetOverpaid_ReturnsOverpaidEnrollments()
    {
        var enrollment = new Enrollment { MemberId = _userId, ActivityId = 1, Price = 10 };
        var overpaidList = new List<EnrollmentBalance>
        {
            new EnrollmentBalance { Enrollment = enrollment, Balance = -5m }
        };
        _paymentValidationService.GetAllOverpaidEnrollments().Returns(overpaidList);

        var result = _service.GetOverpaid(_userId);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetMemberPaymentStatus_ReturnsStatus()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidEnrollmentsForMember(member.Id).Returns(new List<EnrollmentBalance>());
        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);
        _paymentValidationService.HasEverPaidMembershipPayment(member.Id).Returns(true);
        _paymentValidationService.HasDoneOrDoingStudy(member.Id).Returns(true);

        var result = await _service.GetMemberPaymentStatus(member.Id, member.Id, CancellationToken.None);

        Assert.Equal(member.Id, result.MemberId);
        Assert.True(result.HasEverPaidMembership);
        Assert.True(result.HasPaidMembershipBeforeExpirationTime);
        Assert.True(result.HasPaidAllActivities);
        Assert.False(result.IsBegunstiger);
        Assert.True(result.CanPayMembership);
    }

    [Fact]
    public async Task GetMemberPaymentStatus_NeitherStudyNorBegunstiger_CannotPayMembership()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidEnrollmentsForMember(member.Id).Returns(new List<EnrollmentBalance>());
        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);
        _paymentValidationService.HasEverPaidMembershipPayment(member.Id).Returns(false);
        _paymentValidationService.HasDoneOrDoingStudy(member.Id).Returns(false);

        var result = await _service.GetMemberPaymentStatus(member.Id, member.Id, CancellationToken.None);

        Assert.False(result.IsBegunstiger);
        Assert.False(result.CanPayMembership);
    }

    [Fact]
    public async Task GetMemberPaymentStatus_Begunstiger_UsesBegunstigerFeeCheckInsteadOfMembership()
    {
        var member = CreateMember("1234567");
        member.Begunstiger = true;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidEnrollmentsForMember(member.Id).Returns(new List<EnrollmentBalance>());
        _paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id).Returns(true);
        _paymentValidationService.HasDoneOrDoingStudy(member.Id).Returns(false);

        var result = await _service.GetMemberPaymentStatus(member.Id, member.Id, CancellationToken.None);

        Assert.True(result.IsBegunstiger);
        Assert.True(result.CanPayMembership);
        Assert.True(result.HasPaidMembershipBeforeExpirationTime);
        Assert.True(result.HasEverPaidMembership);
        _paymentValidationService.DidNotReceive().HasPaidMembershipPaymentBeforeExpirationTime(Arg.Any<Guid>());
        _paymentValidationService.DidNotReceive().HasEverPaidMembershipPayment(Arg.Any<Guid>());
    }

    [Fact]
    public async Task CreateActivityPayment_EnrollmentsMismatch_ThrowsException()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var dto = new PostActivityPaymentDTO
        {
            MemberId = member.Id,
            ActivityIds = new List<uint> { 999 },
            ManuallyMarkedAsPaid = true
        };

        await Assert.ThrowsAsync<Exception>(() =>
            _service.CreateActivityPayment(dto, _userId));
    }

    [Fact]
    public async Task CreateActivityPayment_DatabaseException_RollsBackAndThrows()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);

        var activity = new Activity
        {
            Name = "Act",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5)
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 15m, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidAmountForEnrollment(Arg.Any<Enrollment>()).Throws(new InvalidOperationException("DB error simulation"));

        var dto = new PostActivityPaymentDTO
        {
            MemberId = member.Id,
            ActivityIds = new List<uint> { activity.Id },
            ManuallyMarkedAsPaid = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateActivityPayment(dto, _userId));
    }

    [Fact]
    public void GetUnpaid_AllUsersTrue_EnforcesPermissionAndReturnsAll()
    {
        var enrollment = new Enrollment { MemberId = _userId, ActivityId = 1, Price = 10 };
        var unpaidList = new List<EnrollmentBalance>
        {
            new EnrollmentBalance { Enrollment = enrollment, Balance = 10m }
        };
        _paymentValidationService.GetAllUnpaidEnrollments().Returns(unpaidList);

        var result = _service.GetUnpaid(_userId, true);

        Assert.Single(result);
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ViewFinances, Arg.Any<uint?>());
    }

    [Fact]
    public void CreateEnrollmentPayments_NullPaymentResponseAndNotManual_ThrowsArgumentException()
    {
        var method = typeof(PaymentService).GetMethod("CreateEnrollmentPayments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var action = () =>
        {
            try
            {
                method?.Invoke(_service, new object?[] { Guid.NewGuid(), new List<Enrollment>(), false, null });
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        };
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public async Task CreateActivityPayment_ActivityNotOpenForPayment_ThrowsException()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);

        var activity = new Activity
        {
            Name = "Act",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = false,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5)
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 15m, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidAmountForEnrollment(Arg.Is<Enrollment>(e => e.ActivityId == activity.Id)).Returns(15m);

        var dto = new PostActivityPaymentDTO
        {
            MemberId = member.Id,
            ActivityIds = new List<uint> { activity.Id },
            ManuallyMarkedAsPaid = true
        };

        await Assert.ThrowsAsync<Exception>(() =>
            _service.CreateActivityPayment(dto, _userId));
    }

    [Fact]
    public async Task GetMemberPaymentStatus_DifferentUser_EnforcesPermission()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.GetUnpaidEnrollmentsForMember(member.Id).Returns(new List<EnrollmentBalance>());
        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);
        _paymentValidationService.HasEverPaidMembershipPayment(member.Id).Returns(true);

        var differentUserId = Guid.NewGuid();
        var result = await _service.GetMemberPaymentStatus(member.Id, differentUserId, CancellationToken.None);

        Assert.Equal(member.Id, result.MemberId);
        _permissionService.Received(1).EnsurePermission(differentUserId, Permission.ViewFinances, Arg.Any<uint?>());
    }
}

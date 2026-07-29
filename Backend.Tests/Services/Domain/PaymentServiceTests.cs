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
        modelBuilder.Entity<MembershipPayment>()
            .HasIndex(p => p.MemberId)
            .IsUnique()
            .HasFilter("MemberId IS NOT NULL");

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
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
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
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
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
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
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
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
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
            _service.CreateMembershipPayment(dto));
    }

    [Fact]
    public async Task CreateMembershipPayment_PendingPaymentExists_ReturnsExistingUrl()
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

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        var result = await _service.CreateMembershipPayment(dto);

        Assert.Equal("pending_url", result.CheckoutUrl);
    }

    [Fact]
    public async Task CreateMembershipPayment_ExistingPaidPayment_ThrowsInvalidOperation()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var payment = new MembershipPayment { MemberId = member.Id, Price = 7.50m, PaymentServiceId = "paid_id", PaymentIntentUrl = "paid_url" };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);
        
        _paymentService.GetPaymentAsync("paid_id")
            .Returns(Task.FromResult(new GetPaymentResponse("paid_id", PaymentStatus.Paid, null)));

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMembershipPayment(dto));
    }

    [Fact]
    public async Task CreateMembershipPayment_ExpiredPayment_DeletesAndThrows()
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

        var dto = new PostMembershipPaymentDTO { MemberId = member.Id };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMembershipPayment(dto));

        _db.ChangeTracker.Clear();
        var deletedPayment = await _db.MembershipPayments.FirstOrDefaultAsync(p => p.MemberId == member.Id);
        var deletedMember = await _db.Members.FindAsync(member.Id);

        Assert.Null(deletedPayment);
        Assert.Null(deletedMember);
        await _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Delete, member.AuthSystemUserId!.Value);
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

        var result = await _service.CreateMembershipPayment(dto);

        Assert.Equal("new_url", result.CheckoutUrl);

        _db.ChangeTracker.Clear();
        var created = await _db.MembershipPayments.FirstOrDefaultAsync(p => p.MemberId == member.Id);
        Assert.NotNull(created);
        Assert.Equal("new_id", created.PaymentServiceId);
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
        await _db.SaveChangesAsync();

        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);

        var result = await _service.ExportPaymentsToCsv(startDate, endDate, _userId, CancellationToken.None);

        Assert.NotNull(result.Content);
        var csvStr = Encoding.UTF8.GetString(result.Content);
        Assert.Contains(";8000;Lidmaatschap;0;7.50;;", csvStr);
        Assert.Contains("Test Organizer | Test Activity", csvStr);
        Assert.Contains("Transaction costs 0.50 x 1", csvStr);
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

        var result = await _service.GetMemberPaymentStatus(member.Id, member.Id, CancellationToken.None);

        Assert.Equal(member.Id, result.MemberId);
        Assert.True(result.HasEverPaidMembership);
        Assert.True(result.HasPaidMembershipBeforeExpirationTime);
        Assert.True(result.HasPaidAllActivities);
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
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
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
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(differentUserId);
    }
}

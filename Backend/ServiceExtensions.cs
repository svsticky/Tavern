using Amazon.S3;
using Backend.Database;
using Backend.Filters;
using Backend.Interfaces;
using Backend.Services;
using Backend.Services.AccountingToolServices;
using Backend.Services.AuthServices;
using Backend.Services.Domain;
using Backend.Services.FileCompressServices;
using Backend.Services.MailServices;
using Backend.Services.MailSubscriptionServices;
using Backend.Services.OutlineServices;
using Backend.Services.PaymentServices;
using Backend.Services.StorageServices;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Backend;

[ExcludeFromCodeCoverage]
internal static class ServiceExtensions
{
    internal static IServiceCollection AddAuthAndAuthorization(this IServiceCollection services, string authSystem)
    {
        switch (authSystem)
        {
            case "keycloak":
                string? devcontainer_issuer = Environment.GetEnvironmentVariable("VITE_KeycloakUrl");
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Authority = $"{Environment.GetEnvironmentVariable("KeycloakUrl")}/realms/{Environment.GetEnvironmentVariable("KeycloakRealm")}";

                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var token = context.Request.Cookies["access_token"];

                                if (!string.IsNullOrEmpty(token))
                                {
                                    context.Token = token;
                                }

                                return Task.CompletedTask;
                            },

                            OnTokenValidated = async context =>
                            {
                                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearerEvents");
                                var dbContext = context.HttpContext.RequestServices.GetRequiredService<PostgresDbContext>();
                                var mailSyncWorkers = context.HttpContext.RequestServices.GetServices<IMailSyncOutboxWorker>();

                                var authIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                                var emailClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)
                                                 ?? context.Principal?.FindFirst("email");

                                if (authIdClaim != null && Guid.TryParse(authIdClaim.Value, out var authId))
                                {
                                    var member = await dbContext.Members.FirstOrDefaultAsync(m => m.AuthSystemUserId == authId);

                                    if (member != null && emailClaim != null)
                                    {
                                        var newEmail = emailClaim.Value;

                                        if (!string.Equals(member.Email, newEmail, StringComparison.OrdinalIgnoreCase))
                                        {
                                            using var transaction = await dbContext.Database.BeginTransactionAsync();
                                            try
                                            {
                                                foreach (var worker in mailSyncWorkers)
                                                {
                                                    worker.EnqueueSyncMail(member.Email, newEmail, dbContext);
                                                }
                                                member.Email = newEmail;
                                                await dbContext.SaveChangesAsync();
                                                logger.LogInformation("Updated member email from validated token for member {MemberId}.", member.Id);
                                                await transaction.CommitAsync();
                                            }
                                            catch (Exception ex)
                                            {
                                                logger.LogError(ex, "Error updating member email for member {MemberId}.", member.Id);
                                                await transaction.RollbackAsync();
                                                throw;
                                            }
                                        }
                                    }
                                }
                            },

                            OnAuthenticationFailed = context =>
                            {
                                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearerEvents");
                                logger.LogWarning(context.Exception, "JWT authentication failed.");
                                return Task.CompletedTask;
                            }
                        };

                        options.RequireHttpsMetadata = false;

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidIssuer = (devcontainer_issuer == null ? Environment.GetEnvironmentVariable("KeycloakUrl") : devcontainer_issuer) + "/realms/" + Environment.GetEnvironmentVariable("KeycloakRealm"),
                            ValidAudience = "tavern"

                        };
                    });

                services.AddScoped<IAuthService, KeycloakAPIService>();
                break;

            default:
                throw new NotSupportedException($"Unsupported AUTH_SYSTEM '{authSystem}'.");
        }

        services.AddAuthorization();
        return services;
    }

    internal static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SupportNonNullableReferenceTypes();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            c.IncludeXmlComments(xmlPath);
            c.SchemaFilter<InheritdocSchemaFilter>(xmlPath);
            c.OperationFilter<FormRequestBodyInheritdocFilter>(xmlPath);

            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Paste your JWT token here"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });

            c.SchemaFilter<EnumSchemaFilter>();
            c.SupportNonNullableReferenceTypes();
        });

        return services;
    }

    internal static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString, bool isGeneratingDocs, IConfiguration configuration)
    {
        services.AddNpgsql<PostgresDbContext>(connectionString: connectionString);

        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "Tavern_";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, CacheService>();

        if (!isGeneratingDocs)
        {
            services.AddHangfire(config => config
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                })
                .UseRecommendedSerializerSettings());

            services.AddHangfireServer();
        }

        var awsOptions = configuration.GetAWSOptions();
        services.AddDefaultAWSOptions(awsOptions);
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var s3Config = new AmazonS3Config
            {
                ServiceURL = Environment.GetEnvironmentVariable("S3_SERVICE_URL") ?? "http://localstack:4566",
                ForcePathStyle = true,
                AuthenticationRegion = Environment.GetEnvironmentVariable("S3_REGION") ?? Environment.GetEnvironmentVariable("S3_REGION") ?? "us-east-1"
            };

            var accessKey = Environment.GetEnvironmentVariable("S3_ACCESS_KEY_ID") ?? Environment.GetEnvironmentVariable("S3_ACCESS_KEY_ID");
            var secretKey = Environment.GetEnvironmentVariable("S3_SECRET_ACCESS_KEY") ?? Environment.GetEnvironmentVariable("S3_SECRET_ACCESS_KEY");

            return new AmazonS3Client(accessKey, secretKey, s3Config);
        });

        services.AddScoped<IStorageService, S3StorageService>();

        return services;
    }

    internal static IServiceCollection AddThirdPartyIntegrations(this IServiceCollection services)
    {
        // Payments
        services.AddScoped<MollieService>();
        services.AddScoped<AbstractPaymentService>(sp =>
        {
            var db = sp.GetRequiredService<PostgresDbContext>();
            var paymentProvider = db.Settings.FirstOrDefault(s => s.Name == "PaymentProvider")?.Value?.Trim().ToUpperInvariant();
            return paymentProvider switch
            {
                "MOLLIE" => sp.GetRequiredService<MollieService>(),
                _ => sp.GetRequiredService<MollieService>()
            };
        });
        services.AddHostedService<PaymentSyncService>();

        // Mail
        services.AddScoped<MailgunService>();
        services.AddScoped<SMTPMailService>();
        services.AddScoped<AbstractMailService>(sp =>
        {
            var db = sp.GetRequiredService<PostgresDbContext>();
            var mailProvider = db.Settings.FirstOrDefault(s => s.Name == "MailService")?.Value?.Trim().ToUpperInvariant();
            return mailProvider switch
            {
                "MAILGUN" => sp.GetRequiredService<MailgunService>(),
                _ => sp.GetRequiredService<SMTPMailService>()
            };
        });

        // Accounting
        services.AddScoped<ExactService>();
        services.AddScoped<AbstractAccountingToolService>(sp =>
        {
            var db = sp.GetRequiredService<PostgresDbContext>();
            var accountingTool = db.Settings.FirstOrDefault(s => s.Name == "AccountingService")?.Value?.Trim().ToUpperInvariant();
            return accountingTool switch
            {
                "EXACT" => sp.GetRequiredService<ExactService>(),
                _ => sp.GetRequiredService<ExactService>()
            };
        });

        // Mail Subscription
        services.AddHttpClient<MailChimpSubscriptionService>();
        services.AddScoped<IMailSubscriptionService>(sp =>
        {
            var db = sp.GetRequiredService<PostgresDbContext>();
            var mailSubscriptionService = db.Settings.FirstOrDefault(s => s.Name == "MailSubscriptionService")?.Value?.Trim().ToUpperInvariant();
            return mailSubscriptionService switch
            {
                "MAILCHIMP" => sp.GetRequiredService<MailChimpSubscriptionService>(),
                _ => sp.GetRequiredService<MailChimpSubscriptionService>()
            };
        });
        services.AddScoped<IMailUpdateService>(sp => sp.GetRequiredService<MailChimpSubscriptionService>());

        // Outline
        services.AddHttpClient<OutlineService>();
        services.AddScoped<IOutlineService, OutlineService>();
        services.AddScoped<OutlineService>(sp => (OutlineService)sp.GetRequiredService<IOutlineService>());
        services.AddScoped<IMailUpdateService>(sp => sp.GetRequiredService<IOutlineService>());
        services.AddScoped<IAdminStatusUpdateService>(sp => sp.GetRequiredService<IOutlineService>());

        return services;
    }

    internal static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IGroupMembershipService, GroupMembershipService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IMailinglistCurationService, MailinglistCurationService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IProfilePictureService, ProfilePictureService>();
        services.AddScoped<IRoleAliasService, RoleAliasService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IStudyEnrollmentService, StudyEnrollmentService>();
        services.AddScoped<IStudyService, StudyService>();
        services.AddScoped<ISpecificationAnswerService, SpecificationAnswerService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IRegisterReasonService, RegisterReasonService>();
        services.AddScoped<IRegistrationDocumentService, RegistrationDocumentService>();
        services.AddScoped<IRegisterSlideService, RegisterSlideService>();
        services.AddScoped<IExternalLinkService, ExternalLinkService>();

        return services;
    }

    internal static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<AuthOutboxWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<AuthOutboxWorker>());
        services.AddHostedService<AccountingToolOutboxWorker>();
        services.AddHostedService<MembershipExpirationSyncService>();
        services.AddHostedService<YearSettingsRefreshWorker>();
        services.AddSingleton<MailSubscriptionOutboxWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<MailSubscriptionOutboxWorker>());
        services.AddSingleton<IMailSyncOutboxWorker>(sp => sp.GetRequiredService<MailSubscriptionOutboxWorker>());

        services.AddSingleton<OutlineOutboxWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<OutlineOutboxWorker>());
        services.AddSingleton<IMailSyncOutboxWorker>(sp => sp.GetRequiredService<OutlineOutboxWorker>());
        services.AddSingleton<IAdminStatusUpdateOutboxWorker>(sp => sp.GetRequiredService<OutlineOutboxWorker>());

        services.AddScoped<IFileCompressService, FileCompressService>();
        services.AddScoped<IPaymentValidationService, PaymentValidationService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddMemoryCache();

        services.AddScoped<ICreateNewBoardService, CreateNewBoardService>();
        services.AddHostedService<DatabaseSeeder>();

        return services;
    }

    internal static WebApplication ConfigureHangfireJobs(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        string timezoneId = Environment.GetEnvironmentVariable("AssociationTimeZone") ?? "Europe/Amsterdam";
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        var recurringJobOptions = new RecurringJobOptions
        {
            TimeZone = tz
        };

        recurringJobManager.AddOrUpdate<AbstractMailService>(
            "outstanding-payments-mail",
            service => service.SendOutstandingPaymentMails(),
            "0 10 * * 5", // Each Friday at 10:00 AM
            recurringJobOptions
        );

        recurringJobManager.AddOrUpdate<AbstractMailService>(
            "annual-study-status-update",
            service => service.SendStudyStatusUpdateMails(),
            BuildYearlyMailCronExpression(db),
            recurringJobOptions
        );


        return app;
    }

    /// <summary>
    /// Builds the cron expression for the yearly account-status mail job from the configurable
    /// "YearlyMailSendDate" (MM-DD) setting, falling back to 1 September if unset or malformed.
    /// </summary>
    private static string BuildYearlyMailCronExpression(PostgresDbContext db)
    {
        var dateSetting = db.Settings.Find("YearlyMailSendDate")?.Value;
        var parts = dateSetting?.Split('-') ?? [];

        if (parts.Length == 2 && int.TryParse(parts[0], out var month) && int.TryParse(parts[1], out var day))
        {
            return $"0 9 {day} {month} *";
        }

        return "0 9 1 9 *"; // 1 September
    }

    internal static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        await db.Database.MigrateAsync();
    }
}

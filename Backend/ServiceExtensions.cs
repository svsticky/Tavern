using Backend.Database;
using Backend.Repositories;
using Backend.Services;
using Backend.Services.AccountingToolServices;
using Backend.Services.AuthServices;
using Backend.Services.FileCompressServices;
using Backend.Services.MailServices;
using Backend.Services.MailSubscriptionServices;
using Backend.Services.StorageServices;
using Backend.Services.PaymentServices;
using Backend.Interfaces;
using Backend.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using Hangfire;
using Hangfire.PostgreSql;
using Mollie.Api;
using System.Text;
using System.Net.Http.Headers;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

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
                                var mailSubscriptionOutboxWorker = context.HttpContext.RequestServices.GetRequiredService<MailSubscriptionOutboxWorker>();
                                
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
                                                mailSubscriptionOutboxWorker.EnqueueTask(member.Email, 0, dbContext);
                                                member.Email = newEmail;
                                                mailSubscriptionOutboxWorker.EnqueueTask(newEmail, member.MailSubscriptions, dbContext);
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
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            ValidIssuer = (devcontainer_issuer == null ? Environment.GetEnvironmentVariable("KeycloakUrl") : devcontainer_issuer) + "/realms/" + Environment.GetEnvironmentVariable("KeycloakRealm")
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
                AuthenticationRegion = "us-east-1"
            };

            return new AmazonS3Client(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"), Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"), s3Config);
        });

        services.AddScoped<IStorageService, S3StorageService>();

        return services;
    }

    internal static IServiceCollection AddThirdPartyIntegrations(this IServiceCollection services)
    {
        // Payments
        string? paymentProvider = Environment.GetEnvironmentVariable("PAYMENT_PROVIDER")?.Trim().ToUpperInvariant();
        switch (paymentProvider)
        {
            default:
                services.AddMollieApi(options => 
                {
                    options.ApiKey = Environment.GetEnvironmentVariable("MollieApiKey") ?? string.Empty;
                });
                services.AddScoped<AbstractPaymentService, MollieService>();
                break;
        }
        services.AddHostedService<PaymentSyncService>();

        // Mail
        string? mailProvider = Environment.GetEnvironmentVariable("MAIL_SERVICE");
        switch (mailProvider)
        {
            case "MAILGUN":
                services.AddScoped<AbstractMailService, MailgunService>();
                break;
            case "SMTP":
                services.AddScoped<AbstractMailService, SMTPMailService>();
                break;
            default:
                break;
        }

        // Accounting
        string? accountingTool = Environment.GetEnvironmentVariable("ACCOUNTING_SERVICE");
        switch (accountingTool)
        {
            case "EXACT":
                services.AddScoped<IAccountingToolService, ExactService>();
                break;
            default:
                break;
        }

        // Mail Subscription
        string? mailSubscriptionService = Environment.GetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE");
        switch (mailSubscriptionService)
        {
            case "MAILCHIMP":
                services.AddHttpClient<IMailSubscriptionService, MailChimpSubscriptionService>(client =>
                {
                    var apiKey = Environment.GetEnvironmentVariable("MAILCHIMP_API_KEY") ?? throw new InvalidOperationException("MAILCHIMP_API_KEY environment variable is not set.");
                    var dataCenter = apiKey.Split('-')[1];
                    
                    client.BaseAddress = new Uri($"https://{dataCenter}.api.mailchimp.com/3.0/");
                    
                    var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"anyuser:{apiKey}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                });
                services.AddScoped<IMailSubscriptionService, MailChimpSubscriptionService>();
                break;
            default:
                break;
        }

        return services;
    }

    internal static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IGroupMembershipRepository, GroupMembershipRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IProfilePictureRepository, ProfilePictureRepository>();
        services.AddScoped<IRoleAliasRepository, RoleAliasRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IStudyEnrollmentRepository, StudyEnrollmentRepository>();
        services.AddScoped<IStudyRepository, StudyRepository>();
        services.AddScoped<ISpecificationAnswerRepository, SpecificationAnswerRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IMailinglistRepository, MailinglistRepository>();
        services.AddScoped<IRegisterReasonRepository, RegisterReasonRepository>();
        services.AddScoped<IRegisterSlideRepository, RegisterSlideRepository>();
        services.AddScoped<IExternalLinkRepository, ExternalLinkRepository>();
        
        return services;
    }

    internal static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<AuthOutboxWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<AuthOutboxWorker>());
        services.AddHostedService<AccountingToolOutboxWorker>();
        services.AddSingleton<MailSubscriptionOutboxWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<MailSubscriptionOutboxWorker>());

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
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var amsterdamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        var recurringJobOptions = new RecurringJobOptions
        {
            TimeZone = amsterdamTimeZone
        };
        
        recurringJobManager.AddOrUpdate<AbstractMailService>(
            "outstanding-payments-mail", 
            service => service.SendOutstandingPaymentMails(), 
            "0 10 * * 5", // Each Friday at 10:00 AM
            recurringJobOptions
        );

        recurringJobManager.AddOrUpdate<AbstractMailService>(
            "annual-board-rotation",
            service => service.SendStudyStatusUpdateMails(),
            "0 9 1 9 *", // 1 September
            recurringJobOptions
        );

        recurringJobManager.AddOrUpdate<ICreateNewBoardService>(
            "annual-board-rotation",
            service => service.PromoteCandidateBoardToBoardAsync(),
            "0 0 1 8 *", // 1 Augustus
            recurringJobOptions
        );

        return app;
    }

    internal static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        await db.Database.MigrateAsync();
    }
}

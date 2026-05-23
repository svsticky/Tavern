using Backend.Database;
using Backend.Services;
using DotNetEnv;
using Mollie.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Backend.Interfaces;
using Amazon.S3;
using Microsoft.AspNetCore.HttpOverrides;
using Backend.Filters;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.HttpLogging;
using System.Text;
using System.Net.Http.Headers;
using Npgsql;
using System.Reflection;

Env.Load();

bool isGeneratingDocs = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "docs";
var authSystem = (Environment.GetEnvironmentVariable("AUTH_SYSTEM") ?? "keycloak").Trim().ToLowerInvariant();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

switch (authSystem)
{
    case "keycloak":
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                    ValidIssuer = Environment.GetEnvironmentVariable("KeycloakUrl") + "/realms/" + Environment.GetEnvironmentVariable("KeycloakRealm")
                };
            });

        builder.Services.AddScoped<IAuthService, KeycloakAPIService>();
        break;

    default:
        throw new NotSupportedException($"Unsupported AUTH_SYSTEM '{authSystem}'.");
}

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    });

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.AddSwaggerGen(c =>
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
string connectionstring = Environment.GetEnvironmentVariable("PostgresqlConnectionString") ?? string.Empty;
builder.Services.AddNpgsql<PostgresDbContext>(connectionString: connectionstring);
builder.Services.AddMollieApi(options => 
{
    options.ApiKey = Environment.GetEnvironmentVariable("MollieApiKey") ?? string.Empty;
});
builder.Services.AddHostedService<PaymentSyncService>();
builder.Services.AddHttpClient("KeycloakAdmin", client =>
{
    var baseUri = Environment.GetEnvironmentVariable("KeycloakUrl") + "/admin/realms/" + Environment.GetEnvironmentVariable("KeycloakRealm");
    client.BaseAddress = new Uri(baseUri.EndsWith("/") ? baseUri : baseUri + "/");
});

if(!isGeneratingDocs)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(Environment.GetEnvironmentVariable("HostUrl")!)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
    });
}
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod
                            | HttpLoggingFields.RequestPath
                            | HttpLoggingFields.ResponseStatusCode
                            | HttpLoggingFields.Duration;
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<AuthOutboxWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AuthOutboxWorker>());
builder.Services.AddHostedService<AccountingToolOutboxWorker>();
builder.Services.AddSingleton<MailSubscriptionOutboxWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MailSubscriptionOutboxWorker>());

var awsOptions = builder.Configuration.GetAWSOptions();

builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var s3Config = new AmazonS3Config
    {
        ServiceURL = Environment.GetEnvironmentVariable("S3_SERVICE_URL") ?? "http://localstack:4566",
        ForcePathStyle = true,
        AuthenticationRegion = "us-east-1"
    };

    return new AmazonS3Client(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"), Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"), s3Config);
});

builder.Services.AddScoped<IStorageService, S3StorageService>();
builder.Services.AddScoped<IFileCompressService, FileCompressService>();
builder.Services.AddScoped<IPaymentValidationService, PaymentValidationService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

string? mailProvider = Environment.GetEnvironmentVariable("MAIL_SERVICE");

switch(mailProvider)
{
    case "MAILGUN":
        builder.Services.AddScoped<AbstractMailService, MailgunService>();
        break;
    case "SMTP":
        builder.Services.AddScoped<AbstractMailService, SMTPMailService>();
        break;
    default:
        break;
}

string? accountingTool = Environment.GetEnvironmentVariable("ACCOUNTING_SERVICE");

switch(accountingTool)
{
    case "EXACT":
        builder.Services.AddScoped<IAccountingToolService, ExactService>();
        break;
    default:
        break;
}

string? mailSubscriptionService = Environment.GetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE");
switch(mailSubscriptionService)
{
    case "MAILCHIMP":
        builder.Services.AddHttpClient<IMailSubscriptionService, MailChimpSubscriptionService>(client =>
        {
            var apiKey = Environment.GetEnvironmentVariable("MAILCHIMP_API_KEY") ?? throw new InvalidOperationException("MAILCHIMP_API_KEY environment variable is not set.");
            var dataCenter = apiKey.Split('-')[1];
            
            client.BaseAddress = new Uri($"https://{dataCenter}.api.mailchimp.com/3.0/");
            
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"anyuser:{apiKey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        });
        builder.Services.AddScoped<IMailSubscriptionService, MailChimpSubscriptionService>();
        break;
    default:
        break;
}

builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IGroupMembershipService, GroupMembershipService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IProfilePictureService, ProfilePictureService>();
builder.Services.AddScoped<IRoleAliasService, RoleAliasService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IStudyEnrollmentService, StudyEnrollmentService>();
builder.Services.AddScoped<IStudyService, StudyService>();
builder.Services.AddScoped<ISpecificationAnswerService, SpecificationAnswerService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IMailinglistService, MailinglistService>();

builder.Services.AddScoped<ICreateNewBoardService, CreateNewBoardService>();
builder.Services.AddHostedService<DatabaseSeeder>();

if(!isGeneratingDocs)
{
    builder.Services.AddHangfire(config => config
        .UsePostgreSqlStorage(options => 
        {
            options.UseNpgsqlConnection(connectionstring);
        })
        .UseRecommendedSerializerSettings());

    builder.Services.AddHangfireServer();
}

WebApplication app = builder.Build();
app.Logger.LogInformation("Starting Tavern backend. Environment: {EnvironmentName}", app.Environment.EnvironmentName);

app.UseForwardedHeaders();
app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || isGeneratingDocs)
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if(!isGeneratingDocs)
{
    app.UseHangfireDashboard();

    using (var scope = app.Services.CreateScope())
    {
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

        recurringJobManager.AddOrUpdate<ICreateNewBoardService>(
            "annual-board-rotation",
            service => service.PromoteCandidateBoardToBoardAsync(),
            "0 0 1 8 *", // 1 Augustus
            recurringJobOptions
        );
    }

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        await db.Database.MigrateAsync();
    }
}

app.Run();

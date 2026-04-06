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

Env.Load();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

builder.Services.AddScoped<KeycloakAPIService>();

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.AddSwaggerGen(c =>
{
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
});
builder.Services.AddNpgsql<PostgresDbContext>(connectionString: builder.Configuration.GetConnectionString("Postgresql"));
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddMollieApi(options => 
{
    options.ApiKey = Environment.GetEnvironmentVariable("MollieApiKey") ?? throw new Exception("No Mollie Key initialized");
});
builder.Services.AddHostedService<PaymentSyncService>();
builder.Services.AddHttpClient("KeycloakAdmin", client =>
{
    var baseUri = Environment.GetEnvironmentVariable("KeycloakUrl") + "/admin/realms/" + Environment.GetEnvironmentVariable("KeycloakRealm");
    client.BaseAddress = new Uri(baseUri.EndsWith("/") ? baseUri : baseUri + "/");
});

builder.Services.AddCors(options =>
   {
       options.AddDefaultPolicy(policy =>
           policy.WithOrigins(Environment.GetEnvironmentVariable("HostUrl")!)
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials());
});

builder.Services.AddHostedService<GroupInitializer>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<KeycloakAPIService>();
builder.Services.AddHostedService<KeycloakOutboxWorker>();
builder.Services.AddHostedService<ExactOutboxWorker>();

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

    return new AmazonS3Client("test", "test", s3Config);
});

builder.Services.AddScoped<IStorageService, S3StorageService>();
builder.Services.AddScoped<IFileCompressor, FileCompressor>();
builder.Services.AddScoped<IPaymentValidationService, PaymentValidationService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IExactService, ExactService>();

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

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

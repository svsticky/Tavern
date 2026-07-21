using Backend;
using DotNetEnv;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpLogging;
using System.IdentityModel.Tokens.Jwt;
using Hangfire;
using System.Diagnostics.CodeAnalysis;

Env.Load();

bool isGeneratingDocs = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "docs";
var authSystem = (Environment.GetEnvironmentVariable("AUTH_SYSTEM") ?? "keycloak").Trim().ToLowerInvariant();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthAndAuthorization(authSystem);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    });

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.AddSwaggerDocumentation();

string connectionstring = Environment.GetEnvironmentVariable("PostgresqlConnectionString") ?? string.Empty;
builder.Services.AddInfrastructureServices(connectionstring, isGeneratingDocs, builder.Configuration);
builder.Services.AddThirdPartyIntegrations();
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

builder.Services.AddHttpClient("KeycloakAdmin", client =>
{
    var baseUri = Environment.GetEnvironmentVariable("KeycloakUrl") + "/admin/realms/" + Environment.GetEnvironmentVariable("KeycloakRealm");
    client.BaseAddress = new Uri(baseUri.EndsWith("/") ? baseUri : baseUri + "/");
});

if (!isGeneratingDocs)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(Environment.GetEnvironmentVariable("HostUrl")!)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());

        options.AddPolicy("PublicCorsPolicy", policy =>
            policy.SetIsOriginAllowed(origin => true)
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

if (!isGeneratingDocs)
{
    await app.MigrateDatabaseAsync();
    app.UseHangfireDashboard();
    app.ConfigureHangfireJobs();
}

app.Run();

/// <summary>
/// Entry point for the application.
/// </summary>
[ExcludeFromCodeCoverage]
internal partial class Program { }

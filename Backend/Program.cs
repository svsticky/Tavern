using Backend.Database;
using Backend.Services;
using DotNetEnv;
using Mollie.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

Env.Load();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("KeycloakAuthority"); 
        options.Audience = $"{Environment.GetEnvironmentVariable("KeycloakClientId")}"; 
        options.RequireHttpsMetadata = false;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = Environment.GetEnvironmentVariable("KeycloakAuthority")
        };
    });

builder.Services.AddHttpClient("KeycloakAdmin", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("KeycloakAuthority")!.Replace("/realms/", "/admin/realms/"));
});

builder.Services.AddScoped<KeycloakAPIService>();

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.AddSwaggerGen();
builder.Services.AddNpgsql<PostgresDbContext>(connectionString: builder.Configuration.GetConnectionString("Postgresql"));
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddMollieApi(options => 
{
    options.ApiKey = Environment.GetEnvironmentVariable("MollieApiKey") ?? throw new Exception("No Mollie Key initialized");
});
builder.Services.AddHostedService<PaymentSyncService>();
builder.Services.AddHttpClient("KeycloakAdmin", client =>
{
    var baseUri = Environment.GetEnvironmentVariable("KeycloakAuthority")!
        .Replace("/realms/", "/admin/realms/");
    client.BaseAddress = new Uri(baseUri.EndsWith("/") ? baseUri : baseUri + "/");
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<KeycloakAPIService>();
builder.Services.AddHostedService<KeycloakOutboxWorker>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();



/* Example program.cs file with many security features we should consider at some point, according to Claude
 *
 * using Backend.Database;
   using Microsoft.AspNetCore.RateLimiting;

   WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

   // Controllers
   builder.Services.AddControllers();
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddRouting(options => options.LowercaseUrls = true);
   builder.Services.AddSwaggerGen();

   // Database
   builder.Services.AddNpgsql<PostgresDbContext>(
       connectionString: builder.Configuration.GetConnectionString("Postgresql"));

   // Security
   builder.Services.AddCors(options =>
   {
       options.AddDefaultPolicy(policy =>
           policy.WithOrigins(builder.Configuration["AllowedOrigins"]!)
                 .AllowAnyHeader()
                 .AllowAnyMethod());
   });

   builder.Services.AddRateLimiter(options =>
   {
       options.AddFixedWindowLimiter("api", opt =>
       {
           opt.Window = TimeSpan.FromMinutes(1);
           opt.PermitLimit = 100;
       });
   });

   // Authentication (add your auth scheme here)
   // builder.Services.AddAuthentication()...
   // builder.Services.AddAuthorization();

   WebApplication app = builder.Build();

   // Middleware order matters!
   if (app.Environment.IsDevelopment())
   {
       app.UseSwagger();
       app.UseSwaggerUI();
   }
   else
   {
       app.UseHsts();
   }

   app.UseHttpsRedirection();

   // Security headers
   app.Use(async (context, next) =>
   {
       context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
       context.Response.Headers.Append("X-Frame-Options", "DENY");
       context.Response.Headers.Append("Referrer-Policy", "no-referrer");
       await next();
   });

   app.UseCors();
   app.UseRateLimiter();

   // app.UseAuthentication();
   // app.UseAuthorization();

   app.MapControllers()
      .RequireRateLimiting("api");

   app.Run();
*/

using Backend.Database;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.AddSwaggerGen();
builder.Services.AddNpgsql<PostgresDbContext>(connectionString: builder.Configuration.GetConnectionString("Postgresql"));

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
    await dbContext.Database.MigrateAsync(); // Applies pending migrations
}

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

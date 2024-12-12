using Backend.Database;
using Backend.Models;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddNpgsql<PostgresDbContext>(connectionString: builder.Configuration.GetConnectionString("Postgresql"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/activity", async (PostgresDbContext db, uint activityId) =>
    {
        // Simply fetch and return
        Activity? activity = await db.Activities.FindAsync(activityId);
        return activity != null ? Results.Ok(activity) : Results.NotFound();
    })
	// .WithName("GetWeatherForecast")
	.WithOpenApi();

app.MapPost("/activity", async (PostgresDbContext db, Activity activity) =>
    {
        // First, ensure activity with same id does not exist yet
        Activity? currentActivity = await db.Activities.FindAsync(activity.Id);
        if (currentActivity != null)
            return Results.BadRequest("Activity already exists with this Id.");

        // Activity does not exist yet, create it
        db.Activities.Add(activity);
        await db.SaveChangesAsync();
        return Results.Ok(activity.Id);
    })
	.WithOpenApi();

app.MapDelete("/activity", async (PostgresDbContext db, uint activityId) =>
    {
        // First, check if activity exists
        Activity? activity = await db.Activities.FindAsync(activityId);
        if (activity == null)
            return Results.NotFound();

        // Remove activity
        db.Activities.Remove(activity);
        await db.SaveChangesAsync();
        return Results.Ok();
    })
	.WithOpenApi();

app.Run();

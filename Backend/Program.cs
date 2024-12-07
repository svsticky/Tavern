using Backend.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/activity", (uint activityId) =>
    {
        return new Activity()
        {
            Id = activityId,
            Name = "Some activity",
            Description = "Some activity description",
            DateTimeStart = DateTimeOffset.Now,
        };
    })
	.WithName("GetWeatherForecast")
	.WithOpenApi();

app.Run();

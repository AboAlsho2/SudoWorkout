
using Microsoft.EntityFrameworkCore;
using TrainingService.Endpoints;
using TrainingService.Integrations.WorkoutService;
using TrainingService.Persistence;
using TrainingService.Services;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddHttpClient<WorkoutServiceClient>(client =>
{
    var baseUrl = builder.Configuration["Services:WorkoutService:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException(
            "Workout Service BaseUrl is not configured.");
    }

    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<TrainingSessionService>();


builder.Services.AddDbContext<TrainingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TrainingDatabase")));



var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    Service = "TrainingService",
    Message = "Training Service is running!"
}));

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy"
}));

app.MapTrainingSessionEndpoints();

app.Run();

public partial class Program
{
}

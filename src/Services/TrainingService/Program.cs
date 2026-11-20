
using Microsoft.EntityFrameworkCore;
using TrainingService.Endpoints;
using TrainingService.Integrations.WorkoutService;
using TrainingService.Messaging;
using TrainingService.Persistence;
using TrainingService.Services;
using TrainingService.Configuration;

var builder = WebApplication.CreateBuilder(args);



builder.Services
    .AddOptions<RabbitMQOptions>()
    .Bind(builder.Configuration.GetSection(
        RabbitMQOptions.SectionName))
    .Validate(options =>
        !string.IsNullOrWhiteSpace(options.Host) &&
        !string.IsNullOrWhiteSpace(options.Username) &&
        !string.IsNullOrWhiteSpace(options.Password) &&
        options.Port is > 0 and <= 65535,
        "RabbitMQ configuration is invalid.")
    .ValidateOnStart();

builder.Services.AddSingleton<
    ITrainingEventPublisher,
    RabbitMQTrainingEventPublisher>();

builder.Services.AddHostedService<
    OutboxPublisherBackgroundService>();

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

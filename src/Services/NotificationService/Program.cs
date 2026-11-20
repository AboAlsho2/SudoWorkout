using NotificationService.Configuration;
using NotificationService.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<RabbitMQOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMQOptions.SectionName))
    .Validate(options =>
        !string.IsNullOrWhiteSpace(options.Host) &&
        options.Port > 0 &&
        options.Port <= 65535 &&
        !string.IsNullOrWhiteSpace(options.Username) &&
        !string.IsNullOrWhiteSpace(options.Password),
        "RabbitMQ configuration is invalid.")
    .ValidateOnStart();

builder.Services.AddHostedService<RabbitMQConsumer>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();

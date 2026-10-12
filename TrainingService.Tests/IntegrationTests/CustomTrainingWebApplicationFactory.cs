
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TrainingService.Integrations.WorkoutService;
using TrainingService.Persistence;
using TrainingService.Tests.IntegrationTests.Infrastructure;

namespace TrainingService.Tests.IntegrationTests;

public sealed class CustomTrainingWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    public FakeWorkoutServiceResponses WorkoutResponses { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TrainingDbContext>>();
            services.RemoveAll<TrainingDbContext>();

            services.AddDbContext<TrainingDbContext>(options =>
                options.UseSqlite(_connection));

            services.RemoveAll<WorkoutServiceClient>();

            services.AddHttpClient<WorkoutServiceClient>(client =>
            {
                client.BaseAddress = new Uri("http://workout-service/");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
                new FakeWorkoutServiceHandler(WorkoutResponses));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}


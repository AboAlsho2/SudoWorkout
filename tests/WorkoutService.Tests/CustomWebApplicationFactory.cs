using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutService.Persistence;

namespace WorkoutService.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the original SQL Server registration.
            var descriptors = services
                .Where(d =>
                    d.ServiceType ==
                        typeof(DbContextOptions<SudoWorkoutDbContext>)
                    || d.ServiceType ==
                        typeof(SudoWorkoutDbContext))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Create an in-memory SQLite database connection.
            _connection = new SqliteConnection(
                "Data Source=:memory:");

            _connection.Open();

            // Register SQLite instead of SQL Server.
            services.AddDbContext<SudoWorkoutDbContext>(
                options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
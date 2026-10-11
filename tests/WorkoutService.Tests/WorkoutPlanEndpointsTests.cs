
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutService.Persistence;

namespace WorkoutService.Tests;

public class WorkoutPlanEndpointsTests
{
    [Fact]
    public async Task CreateWorkoutPlan_WithValidData_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/workout-plans",
            new
            {
                name = "Beginner Plan",
                description = "A beginner workout plan"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var plan = await response.Content
            .ReadFromJsonAsync<WorkoutPlanResponse>();

        Assert.NotNull(plan);
        Assert.True(plan.Id > 0);
        Assert.Equal("Beginner Plan", plan.Name);
        Assert.Equal(
            "A beginner workout plan",
            plan.Description);
    }

    [Fact]
    public async Task CreateWorkoutPlan_WithEmptyName_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/workout-plans",
            new
            {
                name = "",
                description = "Invalid plan"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetWorkoutPlans_ReturnsPlans()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        await CreateWorkoutPlanAsync(client, "Strength Plan");

        var response = await client.GetAsync("/api/workout-plans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plans = await response.Content
            .ReadFromJsonAsync<List<WorkoutPlanResponse>>();

        Assert.NotNull(plans);
        Assert.Contains(plans, p => p.Name == "Strength Plan");
    }

    [Fact]
    public async Task GetWorkoutPlan_WithExistingId_ReturnsPlan()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var created = await CreateWorkoutPlanAsync(
            client,
            "Full Body Plan");

        var response = await client.GetAsync(
            $"/api/workout-plans/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plan = await response.Content
            .ReadFromJsonAsync<WorkoutPlanResponse>();

        Assert.NotNull(plan);
        Assert.Equal(created.Id, plan.Id);
        Assert.Equal("Full Body Plan", plan.Name);
    }

    [Fact]
    public async Task GetWorkoutPlan_WithNonExistingId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.GetAsync(
            "/api/workout-plans/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkoutPlan_WithValidData_UpdatesPlan()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var created = await CreateWorkoutPlanAsync(
            client,
            "Old Plan Name");

        var response = await client.PutAsJsonAsync(
            $"/api/workout-plans/{created.Id}",
            new
            {
                name = "Updated Plan Name",
                description = "Updated description"
            });

        Assert.True(
            response.StatusCode == HttpStatusCode.OK
            || response.StatusCode == HttpStatusCode.NoContent,
            $"Unexpected status code: {(int)response.StatusCode}");

        var getResponse = await client.GetAsync(
            $"/api/workout-plans/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updated = await getResponse.Content
            .ReadFromJsonAsync<WorkoutPlanResponse>();

        Assert.NotNull(updated);
        Assert.Equal("Updated Plan Name", updated.Name);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public async Task UpdateWorkoutPlan_WithEmptyName_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var created = await CreateWorkoutPlanAsync(
            client,
            "Valid Plan");

        var response = await client.PutAsJsonAsync(
            $"/api/workout-plans/{created.Id}",
            new
            {
                name = "",
                description = "Updated description"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkoutPlan_WithNonExistingId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.PutAsJsonAsync(
            "/api/workout-plans/999999",
            new
            {
                name = "Updated Plan",
                description = "Updated description"
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkoutPlan_WithExistingId_ReturnsNoContent()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var created = await CreateWorkoutPlanAsync(
            client,
            "Plan To Delete");

        var response = await client.DeleteAsync(
            $"/api/workout-plans/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/workout-plans/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkoutPlan_WithNonExistingId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.DeleteAsync(
            "/api/workout-plans/999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private static async Task EnsureDatabaseCreatedAsync(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<SudoWorkoutDbContext>();

        await db.Database.EnsureCreatedAsync();
    }

    private static async Task<WorkoutPlanResponse>
        CreateWorkoutPlanAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/workout-plans",
            new
            {
                name,
                description = "Test description"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var plan = await response.Content
            .ReadFromJsonAsync<WorkoutPlanResponse>();

        Assert.NotNull(plan);

        return plan;
    }

    private sealed record WorkoutPlanResponse(
        int Id,
        string Name,
        string? Description);
}
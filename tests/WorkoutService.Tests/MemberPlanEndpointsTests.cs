
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace WorkoutService.Tests;

public class MemberPlanEndpointsTests
{
    private static async Task EnsureDatabaseCreatedAsync(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<WorkoutService.Persistence.SudoWorkoutDbContext>();

        await db.Database.EnsureCreatedAsync();
    }

    private static async Task<int> CreatePlanAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/workout-plans",
            new
            {
                Name = "Strength Plan",
                Description = "A strength training plan"
            });

        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task AssignPlan_ValidRequest_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var planId = await CreatePlanAsync(client);
        var memberId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = planId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var result = json.RootElement;

        Assert.True(result.GetProperty("id").GetInt32() > 0);
        Assert.Equal(
            memberId,
            result.GetProperty("memberId").GetGuid());
        Assert.Equal(
            planId,
            result.GetProperty("workoutPlanId").GetInt32());
    }

    [Fact]
    public async Task AssignPlan_NonexistentPlan_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var memberId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = 99999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignPlan_MemberAlreadyHasPlan_ReturnsConflict()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var planId = await CreatePlanAsync(client);
        var memberId = Guid.NewGuid();

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = planId });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = planId });

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetPlan_MemberHasAssignment_ReturnsPlanDetails()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var planId = await CreatePlanAsync(client);
        var memberId = Guid.NewGuid();

        var assignResponse = await client.PostAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = planId });

        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);

        var response = await client.GetAsync(
            $"/api/members/{memberId}/plan");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var result = json.RootElement;

        Assert.Equal(
            memberId,
            result.GetProperty("memberId").GetGuid());
        Assert.Equal(
            planId,
            result.GetProperty("workoutPlanId").GetInt32());
        Assert.Equal(
            "Strength Plan",
            result.GetProperty("workoutPlanName").GetString());
    }

    [Fact]
    public async Task GetPlan_MemberHasNoAssignment_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.GetAsync(
            $"/api/members/{Guid.NewGuid()}/plan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlan_ValidRequest_ReturnsUpdatedAssignment()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var originalPlanId = await CreatePlanAsync(client);

        var createAnotherPlanResponse = await client.PostAsJsonAsync(
            "/api/workout-plans",
            new
            {
                Name = "Cardio Plan",
                Description = "A cardio training plan"
            });

        createAnotherPlanResponse.EnsureSuccessStatusCode();

        using var secondPlanJson = JsonDocument.Parse(
            await createAnotherPlanResponse.Content.ReadAsStringAsync());

        var newPlanId = secondPlanJson.RootElement
            .GetProperty("id").GetInt32();

        var memberId = Guid.NewGuid();

        var assignResponse = await client.PostAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = originalPlanId });

        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);

        var response = await client.PutAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = newPlanId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            newPlanId,
            json.RootElement.GetProperty("workoutPlanId").GetInt32());

        // Verify the change persisted.
        var getResponse = await client.GetAsync(
            $"/api/members/{memberId}/plan");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var getJson = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            newPlanId,
            getJson.RootElement.GetProperty("workoutPlanId").GetInt32());
        Assert.Equal(
            "Cardio Plan",
            getJson.RootElement.GetProperty("workoutPlanName").GetString());
    }

    [Fact]
    public async Task UpdatePlan_MemberHasNoAssignment_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var planId = await CreatePlanAsync(client);
        var memberId = Guid.NewGuid();

        var response = await client.PutAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = planId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlan_NonexistentPlan_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var planId = await CreatePlanAsync(client);
        var memberId = Guid.NewGuid();

        var assignResponse = await client.PostAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = planId });

        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);

        var response = await client.PutAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = 99999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // The original assignment should remain unchanged.
        var getResponse = await client.GetAsync(
            $"/api/members/{memberId}/plan");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var json = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            planId,
            json.RootElement.GetProperty("workoutPlanId").GetInt32());
    }

    [Fact]
    public async Task DeletePlan_ExistingAssignment_ReturnsNoContent()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var planId = await CreatePlanAsync(client);
        var memberId = Guid.NewGuid();

        var assignResponse = await client.PostAsJsonAsync(
            $"/api/members/{memberId}/plan",
            new { WorkoutPlanId = planId });

        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync(
            $"/api/members/{memberId}/plan");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/members/{memberId}/plan");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePlan_NoAssignment_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.DeleteAsync(
            $"/api/members/{Guid.NewGuid()}/plan");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
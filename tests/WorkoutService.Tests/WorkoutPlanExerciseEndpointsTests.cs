
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutService.Persistence;

namespace WorkoutService.Tests;

public class WorkoutPlanExerciseEndpointsTests
{
    [Fact]
    public async Task AddExerciseToPlan_WithValidData_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);
        var exercise = await CreateExerciseAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises",
            new
            {
                exerciseId = exercise.Id,
                sets = 3,
                reps = 12
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PlanExerciseResponse>();

        Assert.NotNull(result);
        Assert.Equal(plan.Id, result.WorkoutPlanId);
        Assert.Equal(exercise.Id, result.ExerciseId);
        Assert.Equal(3, result.Sets);
        Assert.Equal(12, result.Reps);
    }

    [Fact]
    public async Task AddExerciseToPlan_WithNonExistingPlan_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var exercise = await CreateExerciseAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/workout-plans/999999/exercises",
            new
            {
                exerciseId = exercise.Id,
                sets = 3,
                reps = 12
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddExerciseToPlan_WithNonExistingExercise_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises",
            new
            {
                exerciseId = 999999,
                sets = 3,
                reps = 12
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(-1, 12)]
    [InlineData(3, 0)]
    [InlineData(3, -1)]
    public async Task AddExerciseToPlan_WithInvalidSetsOrReps_ReturnsBadRequest(
        int sets,
        int reps)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);
        var exercise = await CreateExerciseAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises",
            new
            {
                exerciseId = exercise.Id,
                sets,
                reps
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddExerciseToPlan_WhenExerciseAlreadyAdded_ReturnsConflict()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);
        var exercise = await CreateExerciseAsync(client);

        var request = new
        {
            exerciseId = exercise.Id,
            sets = 3,
            reps = 12
        };

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises",
            request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises",
            request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetPlanExercises_ReturnsExercises()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);
        var exercise = await CreateExerciseAsync(client);

        var addResponse = await client.PostAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises",
            new
            {
                exerciseId = exercise.Id,
                sets = 4,
                reps = 10
            });

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var response = await client.GetAsync(
            $"/api/workout-plans/{plan.Id}/exercises");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var exercises = await response.Content
            .ReadFromJsonAsync<List<PlanExerciseDetailsResponse>>();

        Assert.NotNull(exercises);

        var result = Assert.Single(exercises);

        Assert.Equal(exercise.Id, result.ExerciseId);
        Assert.Equal("Test Exercise", result.ExerciseName);
        Assert.Equal(4, result.Sets);
        Assert.Equal(10, result.Reps);
    }

    [Fact]
    public async Task GetPlanExercises_WithNonExistingPlan_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.GetAsync(
            "/api/workout-plans/999999/exercises");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlanExercise_WithValidData_UpdatesSetsAndReps()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);
        var exercise = await CreateExerciseAsync(client);

        var addResponse = await client.PostAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises",
            new
            {
                exerciseId = exercise.Id,
                sets = 3,
                reps = 10
            });

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var response = await client.PutAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises/{exercise.Id}",
            new
            {
                sets = 5,
                reps = 8
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content
            .ReadFromJsonAsync<PlanExerciseResponse>();

        Assert.NotNull(updated);
        Assert.Equal(5, updated.Sets);
        Assert.Equal(8, updated.Reps);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(3, 0)]
    [InlineData(3, -1)]
    public async Task UpdatePlanExercise_WithInvalidSetsOrReps_ReturnsBadRequest(
        int sets,
        int reps)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);
        var exercise = await CreateExerciseAsync(client);

        var addResponse = await client.PostAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises",
            new
            {
                exerciseId = exercise.Id,
                sets = 3,
                reps = 10
            });

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var response = await client.PutAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises/{exercise.Id}",
            new
            {
                sets,
                reps
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlanExercise_WhenNotLinkedToPlan_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);
        var exercise = await CreateExerciseAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises/{exercise.Id}",
            new
            {
                sets = 4,
                reps = 12
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletePlanExercise_WithExistingLink_ReturnsNoContent()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);
        var exercise = await CreateExerciseAsync(client);

        var addResponse = await client.PostAsJsonAsync(
            $"/api/workout-plans/{plan.Id}/exercises",
            new
            {
                exerciseId = exercise.Id,
                sets = 3,
                reps = 10
            });

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var response = await client.DeleteAsync(
            $"/api/workout-plans/{plan.Id}/exercises/{exercise.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // The link should be deleted, but the Exercise itself should remain.
        var listResponse = await client.GetAsync(
            $"/api/workout-plans/{plan.Id}/exercises");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var exercises = await listResponse.Content
            .ReadFromJsonAsync<List<PlanExerciseDetailsResponse>>();

        Assert.NotNull(exercises);
        Assert.Empty(exercises);

        var exerciseResponse = await client.GetAsync(
            $"/api/exercises/{exercise.Id}");

        Assert.Equal(HttpStatusCode.OK, exerciseResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePlanExercise_WhenNotLinkedToPlan_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var plan = await CreatePlanAsync(client);
        var exercise = await CreateExerciseAsync(client);

        var response = await client.DeleteAsync(
            $"/api/workout-plans/{plan.Id}/exercises/{exercise.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task EnsureDatabaseCreatedAsync(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<SudoWorkoutDbContext>();

        await db.Database.EnsureCreatedAsync();
    }

    private static async Task<PlanResponse> CreatePlanAsync(
        HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/workout-plans",
            new
            {
                name = "Test Plan",
                description = "Plan for integration tests"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var plan = await response.Content
            .ReadFromJsonAsync<PlanResponse>();

        Assert.NotNull(plan);

        return plan;
    }

    private static async Task<ExerciseResponse> CreateExerciseAsync(
        HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/exercises",
            new
            {
                name = "Test Exercise",
                description = "Exercise for integration tests"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var exercise = await response.Content
            .ReadFromJsonAsync<ExerciseResponse>();

        Assert.NotNull(exercise);

        return exercise;
    }

    private sealed record PlanResponse(
        int Id,
        string Name,
        string? Description);

    private sealed record ExerciseResponse(
        int Id,
        string Name,
        string? Description);

    private sealed record PlanExerciseResponse(
        int WorkoutPlanId,
        int ExerciseId,
        int Sets,
        int Reps);

    private sealed record PlanExerciseDetailsResponse(
        int WorkoutPlanId,
        int ExerciseId,
        string ExerciseName,
        int Sets,
        int Reps);
}
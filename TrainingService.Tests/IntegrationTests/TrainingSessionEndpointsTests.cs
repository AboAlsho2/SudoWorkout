
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TrainingService.Contracts.WorkoutService;
using TrainingService.Domain.Entities;

namespace TrainingService.Tests.IntegrationTests;

public class TrainingSessionEndpointsTests
    : IClassFixture<CustomTrainingWebApplicationFactory>
{
    private readonly CustomTrainingWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly Guid MemberId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public TrainingSessionEndpointsTests(
        CustomTrainingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TrainingService.Persistence.TrainingDbContext>();

        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task CreateSession_WhenPlanExists_ReturnsCreated()
    {
        // Arrange
        ConfigureSuccessfulWorkoutResponses();

        // Act
        var response = await _client.PostAsync(
            $"/api/members/{MemberId}/sessions",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<SessionResponse>();

        Assert.NotNull(body);
        Assert.Equal(MemberId, body.MemberId);
        Assert.Equal(7, body.WorkoutPlanId);
        Assert.Equal("Beginner Full Body", body.WorkoutPlanName);
        Assert.Equal("InProgress", body.Status);
        Assert.Single(body.Exercises);
    }

    [Fact]
    public async Task CreateSession_WhenMemberHasNoPlan_ReturnsNotFound()
    {
        // Arrange
        _factory.WorkoutResponses.MemberPlanStatus = HttpStatusCode.NotFound;
        _factory.WorkoutResponses.MemberPlan = null;

        // Act
        var response = await _client.PostAsync(
            $"/api/members/{MemberId}/sessions",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_WhenPlanIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        ConfigureSuccessfulWorkoutResponses();

        _factory.WorkoutResponses.Exercises =
            new List<WorkoutPlanExerciseResponse>();

        // Act
        var response = await _client.PostAsync(
            $"/api/members/{MemberId}/sessions",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_WhenExercisesEndpointReturnsNotFound_ReturnsBadGateway()
    {
        // Arrange
        ConfigureSuccessfulWorkoutResponses();

        _factory.WorkoutResponses.ExercisesStatus =
            HttpStatusCode.NotFound;

        // Act
        var response = await _client.PostAsync(
            $"/api/members/{MemberId}/sessions",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task GetSession_WhenSessionDoesNotExist_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/sessions/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteSession_WhenSessionDoesNotExist_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync(
            "/api/sessions/999/complete",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private void ConfigureSuccessfulWorkoutResponses()
    {
        _factory.WorkoutResponses.MemberPlanStatus = HttpStatusCode.OK;

        _factory.WorkoutResponses.MemberPlan =
            new MemberPlanResponse(
                1,
                MemberId,
                7,
                "Beginner Full Body");

        _factory.WorkoutResponses.ExercisesStatus = HttpStatusCode.OK;

        _factory.WorkoutResponses.Exercises =
            new List<WorkoutPlanExerciseResponse>
            {
                new(7, 10, "Push Up", 3, 12)
            };
    }

    private sealed record SessionResponse(
        int Id,
        Guid MemberId,
        int WorkoutPlanId,
        string WorkoutPlanName,
        DateTimeOffset StartedAtUtc,
        string Status,
        List<ExerciseResponse> Exercises);

    private sealed record ExerciseResponse(
        int Id,
        int ExerciseId,
        string ExerciseName,
        int PlannedSets,
        int PlannedReps);
}

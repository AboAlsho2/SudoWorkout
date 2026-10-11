
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutService.Persistence;

namespace WorkoutService.Tests;

public class ExerciseEndpointsTests
{
    [Fact]
    public async Task CreateExercise_WithValidData_ReturnsCreated()
    {

        //Arrange -> 2 Steps (prepare the test environment,Prepare the dataBase)

        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<SudoWorkoutDbContext>();
            await db.Database.EnsureCreatedAsync();
        }


        //Act -> Create Request & Response 

        var request = new
        {
            name = "Push Ups",
            description = "Basic chest exercise"
        };

        var response = await client.PostAsJsonAsync(
            "/api/exercises", request);


        //Assert : Verify the API Response 

        Assert.Equal(HttpStatusCode.Created,
            response.StatusCode);

        var exercise = await response.Content.ReadFromJsonAsync<CreatedExerciseResponse>();

        Assert.NotNull(exercise);
        Assert.True(exercise.Id > 0);
        Assert.Equal("Push Ups", exercise.Name);
        Assert.Equal("Basic chest exercise", exercise.Description);

    }



    [Fact]
    public async Task CreateExercise_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<SudoWorkoutDbContext>();

            await db.Database.EnsureCreatedAsync();
        }

        var request = new
        {
            name = "",
            description = "Exercise without a name"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/exercises",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }


    [Fact]
    public async Task CreateExercise_WithWhitespaceName_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/exercises",
            new
            {
                name = "   ",
                description = "Invalid exercise"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetExercises_ReturnsExercises()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        await CreateExerciseAsync(client, "Push Ups");

        var response = await client.GetAsync("/api/exercises");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var exercises = await response.Content
            .ReadFromJsonAsync<List<CreatedExerciseResponse>>();

        Assert.NotNull(exercises);
        Assert.Contains(exercises, e => e.Name == "Push Ups");
    }

    [Fact]
    public async Task GetExercise_WithExistingId_ReturnsExercise()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var created = await CreateExerciseAsync(client, "Squats");

        var response = await client.GetAsync(
            $"/api/exercises/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var exercise = await response.Content
            .ReadFromJsonAsync<CreatedExerciseResponse>();

        Assert.NotNull(exercise);
        Assert.Equal(created.Id, exercise.Id);
        Assert.Equal("Squats", exercise.Name);
    }

    [Fact]
    public async Task GetExercise_WithNonExistingId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.GetAsync("/api/exercises/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExercise_WithValidData_ReturnsExpectedStatusCode()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var created = await CreateExerciseAsync(client, "Old Name");

        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{created.Id}",
            new
            {
                name = "Updated Name",
                description = "Updated description"
            });

        // Change this assertion to match the status code
        // intentionally returned by your update endpoint.
        Assert.True(
            response.StatusCode == HttpStatusCode.OK
            || response.StatusCode == HttpStatusCode.NoContent,
            $"Unexpected status code: {(int)response.StatusCode}");

        var getResponse = await client.GetAsync(
            $"/api/exercises/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updated = await getResponse.Content
            .ReadFromJsonAsync<CreatedExerciseResponse>();

        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public async Task UpdateExercise_WithEmptyName_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var created = await CreateExerciseAsync(client, "Original Name");

        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{created.Id}",
            new
            {
                name = "",
                description = "Updated description"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExercise_WithNonExistingId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.PutAsJsonAsync(
            "/api/exercises/999999",
            new
            {
                name = "Updated Name",
                description = "Updated description"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteExercise_WithExistingId_ReturnsNoContent()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var created = await CreateExerciseAsync(client, "Exercise To Delete");

        var response = await client.DeleteAsync(
            $"/api/exercises/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/exercises/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteExercise_WithNonExistingId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await EnsureDatabaseCreatedAsync(factory);

        var response = await client.DeleteAsync("/api/exercises/999999");

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

    private static async Task<CreatedExerciseResponse> CreateExerciseAsync(
        HttpClient client,
        string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/exercises",
            new
            {
                name,
                description = "Test description"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var exercise = await response.Content
            .ReadFromJsonAsync<CreatedExerciseResponse>();

        Assert.NotNull(exercise);

        return exercise;
    }





    private sealed record CreatedExerciseResponse(
            int Id,
            string Name , 
            string? Description 
            );


    
}
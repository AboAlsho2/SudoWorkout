
using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrainingService.Contracts.WorkoutService;
using TrainingService.Domain.Entities;
using TrainingService.Integrations.WorkoutService;
using TrainingService.Persistence;
using TrainingService.Services;

namespace TrainingService.Tests.UnitTests;

public class TrainingSessionServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TrainingDbContext _dbContext;
    private readonly FakeHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly TrainingSessionService _sut;

    public TrainingSessionServiceTests()
    {
        // Keep the in-memory SQLite database alive for this test.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TrainingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TrainingDbContext(options);
        _dbContext.Database.EnsureCreated();

        // Fake HTTP responses instead of calling the real Workout Service.
        _handler = new FakeHttpMessageHandler();

        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var workoutServiceClient = new WorkoutServiceClient(_httpClient);

        _sut = new TrainingSessionService(
            _dbContext,
            workoutServiceClient);
    }

    // --------------------------------------------------
    // CreateAsync
    // --------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenMemberHasAssignedPlan_CreatesSessionWithExerciseSnapshots()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        var memberPlan = new MemberPlanResponse(
            1,
            memberId,
            7,
            "Beginner Full Body");

        var exercises = new List<WorkoutPlanExerciseResponse>
        {
            new(7, 10, "Push Up", 3, 12),
            new(7, 11, "Squat", 4, 10)
        };

        ConfigureWorkoutResponses(memberPlan, exercises);

        var before = DateTimeOffset.UtcNow;

        // Act
        var result = await _sut.CreateAsync(memberId);

        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(CreateTrainingSessionFailure.None, result.Failure);
        Assert.NotNull(result.Session);

        var session = result.Session;

        Assert.True(session.Id > 0);
        Assert.Equal(memberId, session.MemberId);
        Assert.Equal(7, session.WorkoutPlanId);
        Assert.Equal("Beginner Full Body", session.WorkoutPlanName);
        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.InRange(session.StartedAtUtc, before, after);

        Assert.Equal(2, session.Exercises.Count);

        var pushUp = Assert.Single(
            session.Exercises.Where(x => x.ExerciseId == 10));

        Assert.Equal("Push Up", pushUp.ExerciseName);
        Assert.Equal(3, pushUp.PlannedSets);
        Assert.Equal(12, pushUp.PlannedReps);

        var squat = Assert.Single(
            session.Exercises.Where(x => x.ExerciseId == 11));

        Assert.Equal("Squat", squat.ExerciseName);
        Assert.Equal(4, squat.PlannedSets);
        Assert.Equal(10, squat.PlannedReps);

        // Verify that the session was actually saved to the database.
        var savedSession = await _dbContext.TrainingSessions
            .Include(x => x.Exercises)
            .SingleAsync(x => x.Id == session.Id);

        Assert.Equal(2, savedSession.Exercises.Count);
    }

    [Fact]
    public async Task CreateAsync_WhenMemberHasNoAssignedPlan_ReturnsNoAssignedPlan()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        ConfigureWorkoutResponses(
            memberPlan: null,
            exercises: null);

        // Act
        var result = await _sut.CreateAsync(memberId);

        // Assert
        Assert.Null(result.Session);

        Assert.Equal(
            CreateTrainingSessionFailure.NoAssignedPlan,
            result.Failure);

        Assert.Equal(0, await _dbContext.TrainingSessions.CountAsync());

        // The exercises endpoint should not be called.
        Assert.Single(_handler.RequestedPaths);
    }

    [Fact]
    public async Task CreateAsync_WhenWorkoutPlanDoesNotExist_ReturnsWorkoutPlanNotFound()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        var memberPlan = new MemberPlanResponse(
            1,
            memberId,
            7,
            "Missing Plan");

        ConfigureWorkoutResponses(
            memberPlan,
            exercises: null);

        // Act
        var result = await _sut.CreateAsync(memberId);

        // Assert
        Assert.Null(result.Session);

        Assert.Equal(
            CreateTrainingSessionFailure.WorkoutPlanNotFound,
            result.Failure);

        Assert.Equal(0, await _dbContext.TrainingSessions.CountAsync());

        Assert.Equal(2, _handler.RequestedPaths.Count);
    }

    [Fact]
    public async Task CreateAsync_WhenWorkoutPlanHasNoExercises_ReturnsEmptyWorkoutPlan()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        var memberPlan = new MemberPlanResponse(
            1,
            memberId,
            7,
            "Empty Plan");

        ConfigureWorkoutResponses(
            memberPlan,
            new List<WorkoutPlanExerciseResponse>());

        // Act
        var result = await _sut.CreateAsync(memberId);

        // Assert
        Assert.Null(result.Session);

        Assert.Equal(
            CreateTrainingSessionFailure.EmptyWorkoutPlan,
            result.Failure);

        Assert.Equal(0, await _dbContext.TrainingSessions.CountAsync());
    }

    // --------------------------------------------------
    // GetByIdAsync
    // --------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenSessionExists_ReturnsSessionWithExercises()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        var session = await AddSessionAsync(memberId);

        // Act
        var result = await _sut.GetByIdAsync(session.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(session.Id, result.Id);
        Assert.Equal(memberId, result.MemberId);
        Assert.Single(result.Exercises);
        Assert.Equal("Push Up", result.Exercises.First().ExerciseName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSessionDoesNotExist_ReturnsNull()
    {
        // Arrange
        const int nonexistentSessionId = 999;

        // Act
        var result = await _sut.GetByIdAsync(nonexistentSessionId);

        // Assert
        Assert.Null(result);
    }

    // --------------------------------------------------
    // GetMemberHistoryAsync
    // --------------------------------------------------

    [Fact]
    public async Task GetMemberHistoryAsync_ReturnsOnlyMemberSessionsInDescendingOrder()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var anotherMemberId = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;

        var oldest = await AddSessionAsync(
            memberId,
            now.AddDays(-3));

        var newest = await AddSessionAsync(
            memberId,
            now.AddDays(-1));

        // This session is newer but belongs to another member.
        await AddSessionAsync(
            anotherMemberId,
            now);

        // Act
        var history = await _dbContext.TrainingSessions
            .AsNoTracking()
            .Include(session => session.Exercises)
            .Where(session => session.MemberId == memberId)
            .ToListAsync();

        history = history
            .OrderByDescending(session => session.StartedAtUtc)
            .ToList();
        // Assert
        Assert.Equal(2, history.Count);

        Assert.Equal(
            new[] { newest.Id, oldest.Id },
            history.Select(x => x.Id).ToArray());

        Assert.All(
            history,
            session => Assert.Equal(memberId, session.MemberId));

        Assert.All(
            history,
            session => Assert.Single(session.Exercises));
    }

    [Fact]
    public async Task GetMemberHistoryAsync_WhenMemberHasNoSessions_ReturnsEmptyList()
    {
        // Arrange
        var memberId = Guid.NewGuid();

        // Act
        var history = await _dbContext.TrainingSessions
            .AsNoTracking()
            .Include(session => session.Exercises)
            .Where(session => session.MemberId == memberId)
            .ToListAsync();

        history = history
            .OrderByDescending(session => session.StartedAtUtc)
            .ToList();
        // Assert
        Assert.NotNull(history);
        Assert.Empty(history);
    }

    // --------------------------------------------------
    // CompleteAsync
    // --------------------------------------------------

    [Fact]
    public async Task CompleteAsync_WhenSessionIsInProgress_CompletesAndPersistsIt()
    {
        // Arrange
        var session = await AddSessionAsync(Guid.NewGuid());

        Assert.Equal(SessionStatus.InProgress, session.Status);

        // Act
        var result = await _sut.CompleteAsync(session.Id);

        // Assert
        Assert.True(result);

        var persistedSession = await _dbContext.TrainingSessions
            .AsNoTracking()
            .SingleAsync(x => x.Id == session.Id);

        Assert.Equal(SessionStatus.Completed, persistedSession.Status);
    }

    [Fact]
    public async Task CompleteAsync_WhenSessionDoesNotExist_ReturnsFalse()
    {
        // Arrange
        const int nonexistentSessionId = 999;

        // Act
        var result = await _sut.CompleteAsync(nonexistentSessionId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CompleteAsync_WhenSessionIsAlreadyCompleted_ReturnsFalse()
    {
        // Arrange
        var session = await AddSessionAsync(
            Guid.NewGuid(),
            status: SessionStatus.Completed);

        // Act
        var result = await _sut.CompleteAsync(session.Id);

        // Assert
        Assert.False(result);

        var persistedSession = await _dbContext.TrainingSessions
            .AsNoTracking()
            .SingleAsync(x => x.Id == session.Id);

        Assert.Equal(SessionStatus.Completed, persistedSession.Status);
    }

    // --------------------------------------------------
    // Test helpers
    // --------------------------------------------------


    private void ConfigureWorkoutResponses(
        MemberPlanResponse? memberPlan,
        List<WorkoutPlanExerciseResponse>? exercises)
    {
        _handler.ResponseFactory = request =>
        {
            var path = request.RequestUri!.AbsolutePath;

            if (path.StartsWith(
                "/api/members/",
                StringComparison.OrdinalIgnoreCase))
            {
                if (memberPlan is null)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(memberPlan)
                };
            }

            if (path.StartsWith(
                "/api/workout-plans/",
                StringComparison.OrdinalIgnoreCase))
            {
                if (exercises is null)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(exercises)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };
    }

    private async Task<TrainingSession> AddSessionAsync(
        Guid memberId,
        DateTimeOffset? startedAtUtc = null,
        SessionStatus status = SessionStatus.InProgress)
    {
        var session = new TrainingSession
        {
            MemberId = memberId,
            WorkoutPlanId = 7,
            WorkoutPlanName = "Beginner Full Body",
            StartedAtUtc = startedAtUtc ?? DateTimeOffset.UtcNow,
            Status = status,
            Exercises = new List<TrainingSessionExercise>
            {
                new()
                {
                    ExerciseId = 10,
                    ExerciseName = "Push Up",
                    PlannedSets = 3,
                    PlannedReps = 12
                }
            }
        };

        _dbContext.TrainingSessions.Add(session);

        await _dbContext.SaveChangesAsync();

        return session;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _dbContext.Dispose();
        _connection.Dispose();
    }
}

// Fake HTTP handler used by WorkoutServiceClient.
// It returns predefined responses without calling a real service.
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<string> RequestedPaths { get; } = new();

    public Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory
    {
        get;
        set;
    } = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestedPaths.Add(request.RequestUri!.AbsolutePath);

        var response = ResponseFactory(request);

        return Task.FromResult(response);
    }
}

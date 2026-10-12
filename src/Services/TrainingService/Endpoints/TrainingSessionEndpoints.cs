
using System.Text.Json;
using TrainingService.Domain.Entities;
using TrainingService.Services;

namespace TrainingService.Endpoints;

public static class TrainingSessionEndpoints
{
    public static IEndpointRouteBuilder MapTrainingSessionEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/members/{memberId:guid}/sessions",
            async (
                Guid memberId,
                TrainingSessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await sessionService.CreateAsync(
                        memberId,
                        cancellationToken);

                    if (result.Session is null)
                    {
                        return result.Failure switch
                        {
                            CreateTrainingSessionFailure.NoAssignedPlan =>
                                Results.NotFound(new
                                {
                                    Message =
                                        "Member has no assigned workout plan."
                                }),

                            CreateTrainingSessionFailure.WorkoutPlanNotFound =>
                                Results.Problem(
                                    title: "Workout plan exercises were not found.",
                                    statusCode: StatusCodes.Status502BadGateway),

                            CreateTrainingSessionFailure.EmptyWorkoutPlan =>
                                Results.BadRequest(new
                                {
                                    Message =
                                        "Cannot start a session with an empty workout plan."
                                }),

                            _ => Results.Problem(
                                title: "Failed to create training session.",
                                statusCode: StatusCodes.Status500InternalServerError)
                        };
                    }

                    var session = result.Session;

                    var response = new
                    {
                        session.Id,
                        session.MemberId,
                        session.WorkoutPlanId,
                        session.WorkoutPlanName,
                        session.StartedAtUtc,
                        Status = session.Status.ToString(),
                        Exercises = session.Exercises.Select(exercise => new
                        {
                            exercise.Id,
                            exercise.ExerciseId,
                            exercise.ExerciseName,
                            exercise.PlannedSets,
                            exercise.PlannedReps
                        })
                    };

                    return Results.Created(
                        $"/api/sessions/{session.Id}",
                        response);
                }
                catch (HttpRequestException)
                {
                    return Results.Problem(
                        title: "Workout Service could not be reached or returned an unsuccessful response.",
                        statusCode: StatusCodes.Status502BadGateway);
                }
                catch (JsonException)
                {
                    return Results.Problem(
                        title: "Workout Service returned an invalid JSON response.",
                        statusCode: StatusCodes.Status502BadGateway);
                }
            });



        app.MapGet(
            "/api/sessions/{sessionId:int}",
            async (
                int sessionId,
                TrainingSessionService service,
                CancellationToken cancellationToken) =>
            {
                var session = await service.GetByIdAsync(
                    sessionId,
                    cancellationToken);

                if (session is null)
                {
                    return Results.NotFound(new
                    {
                        Message = "Training session was not found."
                    });
                }

                return Results.Ok(new
                {
                    session.Id,
                    session.MemberId,
                    session.WorkoutPlanId,
                    session.WorkoutPlanName,
                    session.StartedAtUtc,
                    Status = session.Status.ToString(),
                    Exercises = session.Exercises.Select(exercise => new
                    {
                        exercise.Id,
                        exercise.ExerciseId,
                        exercise.ExerciseName,
                        exercise.PlannedSets,
                        exercise.PlannedReps
                    })
                });
            });

        app.MapGet(
            "/api/members/{memberId:guid}/sessions",
            async (
                Guid memberId,
                TrainingSessionService service,
                CancellationToken cancellationToken) =>
            {
                var sessions = await service.GetMemberHistoryAsync(
                    memberId,
                    cancellationToken);

                return Results.Ok(sessions.Select(session => new
                {
                    session.Id,
                    session.MemberId,
                    session.WorkoutPlanId,
                    session.WorkoutPlanName,
                    session.StartedAtUtc,
                    Status = session.Status.ToString(),
                    Exercises = session.Exercises.Select(exercise => new
                    {
                        exercise.Id,
                        exercise.ExerciseId,
                        exercise.ExerciseName,
                        exercise.PlannedSets,
                        exercise.PlannedReps
                    })
                }));
            });

        app.MapPost(
            "/api/sessions/{sessionId:int}/complete",
            async (
                int sessionId,
                TrainingSessionService service,
                CancellationToken cancellationToken) =>
            {
                var session = await service.GetByIdAsync(
                    sessionId,
                    cancellationToken);

                if (session is null)
                {
                    return Results.NotFound(new
                    {
                        Message = "Training session was not found."
                    });
                }

                if (session.Status == SessionStatus.Completed)
                {
                    return Results.Conflict(new
                    {
                        Message = "Training session is already completed."
                    });
                }

                await service.CompleteAsync(
                    sessionId,
                    cancellationToken);

                return Results.Ok(new
                {
                    session.Id,
                    Status = SessionStatus.Completed.ToString()
                });
            });


        return app;
    }
}

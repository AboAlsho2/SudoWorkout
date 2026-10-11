
using Microsoft.EntityFrameworkCore;
using WorkoutService.Contracts;
using WorkoutService.Domain.Entities;
using WorkoutService.Persistence;

namespace WorkoutService.Endpoints;

public static class ExerciseEndpoints
{
    public static IEndpointRouteBuilder MapExerciseEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exercises");

        // GET: /api/exercises
        group.MapGet("/", async (SudoWorkoutDbContext db) =>
        {
            var exercises = await db.Exercises
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Description
                })
                .ToListAsync();

            return Results.Ok(exercises);
        });

        // GET: /api/exercises/{id}
        group.MapGet("/{id:int}", async (
            int id,
            SudoWorkoutDbContext db) =>
        {
            var exercise = await db.Exercises
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Description
                })
                .FirstOrDefaultAsync();

            return exercise is null
                ? Results.NotFound()
                : Results.Ok(exercise);
        });

        // POST: /api/exercises
        group.MapPost("/", async (
            CreateExerciseRequest request,
            SudoWorkoutDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new
                {
                    message = "Exercise name is required."
                });
            }

            var exercise = new Exercise
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(
                    request.Description)
                    ? null
                    : request.Description.Trim()
            };

            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/exercises/{exercise.Id}",
                new
                {
                    exercise.Id,
                    exercise.Name,
                    exercise.Description
                });
        });

        // PUT: /api/exercises/{id}
        group.MapPut("/{id:int}", async (
            int id,
            UpdateExerciseRequest request,
            SudoWorkoutDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new
                {
                    message = "Exercise name is required."
                });
            }

            var exercise = await db.Exercises.FindAsync(id);

            if (exercise is null)
            {
                return Results.NotFound();
            }

            exercise.Name = request.Name.Trim();
            exercise.Description =
                string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim();

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                exercise.Id,
                exercise.Name,
                exercise.Description
            });
        });

        // DELETE: /api/exercises/{id}
        group.MapDelete("/{id:int}", async (
            int id,
            SudoWorkoutDbContext db) =>
        {
            var exercise = await db.Exercises.FindAsync(id);

            if (exercise is null)
            {
                return Results.NotFound();
            }

            db.Exercises.Remove(exercise);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }
}
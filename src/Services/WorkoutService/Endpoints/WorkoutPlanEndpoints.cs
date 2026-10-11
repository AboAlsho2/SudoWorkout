
using Microsoft.EntityFrameworkCore;
using WorkoutService.Contracts;
using WorkoutService.Domain.Entities;
using WorkoutService.Persistence;

namespace WorkoutService.Endpoints;

public static class WorkoutPlanEndpoints
{
    public static IEndpointRouteBuilder MapWorkoutPlanEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workout-plans");

        // GET: /api/workout-plans
        group.MapGet("/", async (SudoWorkoutDbContext db) =>
        {
            var plans = await db.WorkoutPlans
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Description
                })
                .ToListAsync();

            return Results.Ok(plans);
        });

        // GET: /api/workout-plans/{id}
        group.MapGet("/{id:int}", async (
            int id,
            SudoWorkoutDbContext db) =>
        {
            var plan = await db.WorkoutPlans
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Description
                })
                .FirstOrDefaultAsync();

            return plan is null
                ? Results.NotFound()
                : Results.Ok(plan);
        });

        // POST: /api/workout-plans
        group.MapPost("/", async (
            CreateWorkoutPlanRequest request,
            SudoWorkoutDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new
                {
                    message = "Workout plan name is required."
                });
            }

            var plan = new WorkoutPlan
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(
                    request.Description)
                    ? null
                    : request.Description.Trim()
            };

            db.WorkoutPlans.Add(plan);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/workout-plans/{plan.Id}",
                new
                {
                    plan.Id,
                    plan.Name,
                    plan.Description
                });
        });

        // PUT: /api/workout-plans/{id}
        group.MapPut("/{id:int}", async (
            int id,
            UpdateWorkoutPlanRequest request,
            SudoWorkoutDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new
                {
                    message = "Workout plan name is required."
                });
            }

            var plan = await db.WorkoutPlans.FindAsync(id);

            if (plan is null)
            {
                return Results.NotFound();
            }

            plan.Name = request.Name.Trim();
            plan.Description = string.IsNullOrWhiteSpace(
                request.Description)
                ? null
                : request.Description.Trim();

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                plan.Id,
                plan.Name,
                plan.Description
            });
        });

        // DELETE: /api/workout-plans/{id}
        group.MapDelete("/{id:int}", async (
            int id,
            SudoWorkoutDbContext db) =>
        {
            var plan = await db.WorkoutPlans.FindAsync(id);

            if (plan is null)
            {
                return Results.NotFound();
            }

            db.WorkoutPlans.Remove(plan);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });


        // POST: /api/workout-plans/{planId}/exercises
        group.MapPost("/{planId:int}/exercises", async (
            int planId,
            AddExerciseToPlanRequest request,
            SudoWorkoutDbContext db) =>
        {
            if (request.Sets <= 0 || request.Reps <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "Sets and Reps must be greater than zero."
                });
            }

            var planExists = await db.WorkoutPlans
                .AnyAsync(x => x.Id == planId);

            if (!planExists)
            {
                return Results.NotFound(new
                {
                    message = "Workout plan not found."
                });
            }

            var exerciseExists = await db.Exercises
                .AnyAsync(x => x.Id == request.ExerciseId);

            if (!exerciseExists)
            {
                return Results.NotFound(new
                {
                    message = "Exercise not found."
                });
            }

            var alreadyAdded = await db.WorkoutPlanExercises
                .AnyAsync(x =>
                    x.WorkoutPlanId == planId &&
                    x.ExerciseId == request.ExerciseId);

            if (alreadyAdded)
            {
                return Results.Conflict(new
                {
                    message = "This exercise is already in the workout plan."
                });
            }

            var planExercise = new WorkoutPlanExercise
            {
                WorkoutPlanId = planId,
                ExerciseId = request.ExerciseId,
                Sets = request.Sets,
                Reps = request.Reps
            };

            db.WorkoutPlanExercises.Add(planExercise);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/workout-plans/{planId}/exercises/{request.ExerciseId}",
                new
                {
                    planExercise.WorkoutPlanId,
                    planExercise.ExerciseId,
                    planExercise.Sets,
                    planExercise.Reps
                });
        });


        // GET: /api/workout-plans/{planId}/exercises
        group.MapGet("/{planId:int}/exercises", async (
            int planId,
            SudoWorkoutDbContext db) =>
        {
            var planExists = await db.WorkoutPlans
                .AnyAsync(x => x.Id == planId);

            if (!planExists)
            {
                return Results.NotFound(new
                {
                    message = "Workout plan not found."
                });
            }

            var exercises = await db.WorkoutPlanExercises
                .AsNoTracking()
                .Where(x => x.WorkoutPlanId == planId)
                .Select(x => new
                {
                    x.WorkoutPlanId,
                    x.ExerciseId,
                    ExerciseName = x.Exercise.Name,
                    x.Sets,
                    x.Reps
                })
                .ToListAsync();

            return Results.Ok(exercises);
        });


        // PUT: /api/workout-plans/{planId}/exercises/{exerciseId}
        group.MapPut("/{planId:int}/exercises/{exerciseId:int}",
            async (
                int planId,
                int exerciseId,
                UpdatePlanExerciseRequest request,
                SudoWorkoutDbContext db) =>
            {
                if (request.Sets <= 0 || request.Reps <= 0)
                {
                    return Results.BadRequest(new
                    {
                        message = "Sets and Reps must be greater than zero."
                    });
                }

                var planExercise = await db.WorkoutPlanExercises
                    .FirstOrDefaultAsync(x =>
                        x.WorkoutPlanId == planId &&
                        x.ExerciseId == exerciseId);

                if (planExercise is null)
                {
                    return Results.NotFound();
                }

                planExercise.Sets = request.Sets;
                planExercise.Reps = request.Reps;

                await db.SaveChangesAsync();

                return Results.Ok(new
                {
                    planExercise.WorkoutPlanId,
                    planExercise.ExerciseId,
                    planExercise.Sets,
                    planExercise.Reps
                });
            });


        // DELETE: /api/workout-plans/{planId}/exercises/{exerciseId}
        group.MapDelete("/{planId:int}/exercises/{exerciseId:int}",
            async (
                int planId,
                int exerciseId,
                SudoWorkoutDbContext db) =>
            {
                var planExercise = await db.WorkoutPlanExercises
                    .FirstOrDefaultAsync(x =>
                        x.WorkoutPlanId == planId &&
                        x.ExerciseId == exerciseId);

                if (planExercise is null)
                {
                    return Results.NotFound();
                }

                db.WorkoutPlanExercises.Remove(planExercise);
                await db.SaveChangesAsync();

                return Results.NoContent();
            });

        return app;
    }
}
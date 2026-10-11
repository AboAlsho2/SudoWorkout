
using Microsoft.EntityFrameworkCore;
using WorkoutService.Contracts;
using WorkoutService.Domain.Entities;
using WorkoutService.Persistence;

namespace WorkoutService.Endpoints;

public static class MemberPlanEndpoints
{
    public static IEndpointRouteBuilder MapMemberPlanEndpoints(
        this IEndpointRouteBuilder app)
    {
        // POST: /api/members/{memberId}/plan
        app.MapPost("/api/members/{memberId:guid}/plan", async (
            Guid memberId,
            AssignWorkoutPlanRequest request,
            SudoWorkoutDbContext db) =>
        {
            var planExists = await db.WorkoutPlans
                .AnyAsync(x => x.Id == request.WorkoutPlanId);

            if (!planExists)
            {
                return Results.NotFound(new
                {
                    message = "Workout plan not found."
                });
            }

            var existingAssignment = await db.PlanAssignments
                .FirstOrDefaultAsync(x => x.MemberId == memberId);

            if (existingAssignment is not null)
            {
                return Results.Conflict(new
                {
                    message = "This member already has a workout plan."
                });
            }

            var assignment = new PlanAssignment
            {
                MemberId = memberId,
                WorkoutPlanId = request.WorkoutPlanId
            };

            db.PlanAssignments.Add(assignment);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/members/{memberId}/plan",
                new
                {
                    assignment.Id,
                    assignment.MemberId,
                    assignment.WorkoutPlanId
                });
        });

        // GET: /api/members/{memberId}/plan
        app.MapGet("/api/members/{memberId:guid}/plan", async (
            Guid memberId,
            SudoWorkoutDbContext db) =>
        {
            var assignment = await db.PlanAssignments
                .AsNoTracking()
                .Where(x => x.MemberId == memberId)
                .Select(x => new
                {
                    x.Id,
                    x.MemberId,
                    x.WorkoutPlanId,
                    WorkoutPlanName = x.WorkoutPlan.Name
                })
                .FirstOrDefaultAsync();

            return assignment is null
                ? Results.NotFound()
                : Results.Ok(assignment);
        });

        // PUT: /api/members/{memberId}/plan
        app.MapPut("/api/members/{memberId:guid}/plan", async (
            Guid memberId,
            AssignWorkoutPlanRequest request,
            SudoWorkoutDbContext db) =>
        {
            var assignment = await db.PlanAssignments
                .FirstOrDefaultAsync(x => x.MemberId == memberId);

            if (assignment is null)
            {
                return Results.NotFound(new
                {
                    message = "Member has no assigned workout plan."
                });
            }

            var planExists = await db.WorkoutPlans
                .AnyAsync(x => x.Id == request.WorkoutPlanId);

            if (!planExists)
            {
                return Results.NotFound(new
                {
                    message = "Workout plan not found."
                });
            }

            assignment.WorkoutPlanId = request.WorkoutPlanId;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                assignment.Id,
                assignment.MemberId,
                assignment.WorkoutPlanId
            });
        });

        // DELETE: /api/members/{memberId}/plan
        app.MapDelete("/api/members/{memberId:guid}/plan", async (
            Guid memberId,
            SudoWorkoutDbContext db) =>
        {
            var assignment = await db.PlanAssignments
                .FirstOrDefaultAsync(x => x.MemberId == memberId);

            if (assignment is null)
            {
                return Results.NotFound();
            }

            db.PlanAssignments.Remove(assignment);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }
}
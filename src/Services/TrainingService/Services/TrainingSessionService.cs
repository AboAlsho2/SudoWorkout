
using Microsoft.EntityFrameworkCore;
using TrainingService.Contracts.WorkoutService;
using TrainingService.Domain.Entities;
using TrainingService.Integrations.WorkoutService;
using TrainingService.Persistence;

namespace TrainingService.Services;



public sealed class TrainingSessionService(
    TrainingDbContext dbContext,
    WorkoutServiceClient workoutServiceClient)
{
    public async Task<CreateTrainingSessionResult> CreateAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var memberPlan = await workoutServiceClient.GetMemberPlanAsync(
            memberId,
            cancellationToken);

        if (memberPlan is null)
        {
            return new(
                null,
                CreateTrainingSessionFailure.NoAssignedPlan);
        }

        var exercises =
            await workoutServiceClient.GetWorkoutPlanExercisesAsync(
                memberPlan.WorkoutPlanId,
                cancellationToken);

        if (exercises is null)
        {
            return new(
                null,
                CreateTrainingSessionFailure.WorkoutPlanNotFound);
        }

        if (exercises.Count == 0)
        {
            return new(
                null,
                CreateTrainingSessionFailure.EmptyWorkoutPlan);
        }

        var session = new TrainingSession
        {
            MemberId = memberId,
            WorkoutPlanId = memberPlan.WorkoutPlanId,
            WorkoutPlanName = memberPlan.WorkoutPlanName,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = SessionStatus.InProgress,
            Exercises = exercises.Select(exercise =>
                new TrainingSessionExercise
                {
                    ExerciseId = exercise.ExerciseId,
                    ExerciseName = exercise.ExerciseName,
                    PlannedSets = exercise.Sets,
                    PlannedReps = exercise.Reps
                }).ToList()
        };

        dbContext.TrainingSessions.Add(session);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new(session, CreateTrainingSessionFailure.None);
    }



    public async Task<TrainingSession?> GetByIdAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TrainingSessions
            .Include(session => session.Exercises)
            .FirstOrDefaultAsync(
                session => session.Id == sessionId,
                cancellationToken);
    }

    public async Task<List<TrainingSession>> GetMemberHistoryAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TrainingSessions
            .AsNoTracking()
            .Include(session => session.Exercises)
            .Where(session => session.MemberId == memberId)
            .OrderByDescending(session => session.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CompleteAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await dbContext.TrainingSessions
            .FirstOrDefaultAsync(
                session => session.Id == sessionId,
                cancellationToken);

        if (session is null || session.Status == SessionStatus.Completed)
        {
            return false;
        }

        session.Status = SessionStatus.Completed;

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }


}

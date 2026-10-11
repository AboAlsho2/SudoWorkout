
namespace WorkoutService.Contracts;

public record CreateWorkoutPlanRequest(
    string Name,
    string? Description);

public record UpdateWorkoutPlanRequest(
    string Name,
    string? Description);

public record AddExerciseToPlanRequest(
    int ExerciseId,
    int Sets,
    int Reps);

public record UpdatePlanExerciseRequest(
    int Sets,
    int Reps);

public record AssignWorkoutPlanRequest(int WorkoutPlanId);

namespace TrainingService.Contracts.WorkoutService;

public sealed record WorkoutPlanExerciseResponse(
    int WorkoutPlanId,
    int ExerciseId,
    string ExerciseName,
    int Sets,
    int Reps);

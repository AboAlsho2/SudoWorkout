namespace WorkoutService.Domain.Entities;

public class WorkoutPlanExercise
{
    public int WorkoutPlanId { get; set; }

    public int ExerciseId { get; set; }

    public int Sets { get; set; }

    public int Reps { get; set; }

    public WorkoutPlan WorkoutPlan { get; set; } = null!;

    public Exercise Exercise { get; set; } = null!;
}
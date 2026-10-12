
namespace TrainingService.Domain.Entities;

public class TrainingSessionExercise
{
    public int Id { get; set; }

    public int TrainingSessionId { get; set; }

    public int ExerciseId { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public int PlannedSets { get; set; }

    public int PlannedReps { get; set; }

    public TrainingSession TrainingSession { get; set; } = null!;
}

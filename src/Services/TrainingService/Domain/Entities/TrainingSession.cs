
namespace TrainingService.Domain.Entities;

public class TrainingSession
{
    public int Id { get; set; }

    public Guid MemberId { get; set; }

    public int WorkoutPlanId { get; set; }

    public string WorkoutPlanName { get; set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; set; }

    public SessionStatus Status { get; set; } =
        SessionStatus.InProgress;

    public ICollection<TrainingSessionExercise> Exercises { get; set; }
        = new List<TrainingSessionExercise>();
}

namespace WorkoutService.Domain.Entities;

public class WorkoutPlan
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<WorkoutPlanExercise> Exercises { get; set; }
        = new List<WorkoutPlanExercise>();

    public ICollection<PlanAssignment> Assignments { get; set; }
        = new List<PlanAssignment>();
}
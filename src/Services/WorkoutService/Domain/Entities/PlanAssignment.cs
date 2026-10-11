namespace WorkoutService.Domain.Entities;

public class PlanAssignment
{
    public int Id { get; set; }

    public int WorkoutPlanId { get; set; }

    public Guid MemberId { get; set; }

    public WorkoutPlan WorkoutPlan { get; set; } = null!;
}
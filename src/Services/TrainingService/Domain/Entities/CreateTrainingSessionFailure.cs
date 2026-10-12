namespace TrainingService.Domain.Entities
{
    public enum CreateTrainingSessionFailure
    {
        None,
        NoAssignedPlan,
        WorkoutPlanNotFound,
        EmptyWorkoutPlan
    }
}
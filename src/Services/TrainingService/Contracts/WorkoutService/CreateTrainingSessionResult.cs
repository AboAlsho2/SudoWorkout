using TrainingService.Domain.Entities;

namespace TrainingService.Contracts.WorkoutService
{
    public sealed record CreateTrainingSessionResult(
        TrainingSession? Session,
        CreateTrainingSessionFailure Failure);

}

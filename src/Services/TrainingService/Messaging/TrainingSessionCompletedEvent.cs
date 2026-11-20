
namespace TrainingService.Messaging;

public sealed record TrainingSessionCompletedEvent(
    Guid EventId,
    int TrainingSessionId,
    Guid MemberId,
    int WorkoutPlanId,
    DateTimeOffset CompletedAtUtc);

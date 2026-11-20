
namespace TrainingService.Messaging;

public interface ITrainingEventPublisher
{
    Task PublishSessionCompletedAsync(
        TrainingSessionCompletedEvent message,
        CancellationToken cancellationToken = default);
}


namespace TrainingService.Domain.Entities;

public class OutboxMessage
{
    // Same ID as the event being published.
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    // Serialized event payload.
    public string Content { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }
}

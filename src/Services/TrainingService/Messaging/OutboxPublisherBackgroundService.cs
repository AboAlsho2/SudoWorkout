
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrainingService.Domain.Entities;
using TrainingService.Persistence;

namespace TrainingService.Messaging;

public sealed class OutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    ITrainingEventPublisher eventPublisher,
    ILogger<OutboxPublisherBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Outbox Publisher Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "An error occurred while processing Outbox messages.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation(
            "Outbox Publisher Background Service stopped.");
    }

    private async Task ProcessPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<TrainingDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var outboxMessage in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TrainingSessionCompletedEvent message;

            try
            {
                if (outboxMessage.Type !=
                    nameof(TrainingSessionCompletedEvent))
                {
                    throw new InvalidOperationException(
                        $"Unsupported Outbox message type: " +
                        $"{outboxMessage.Type}");
                }

                message =
                    JsonSerializer.Deserialize
                        <TrainingSessionCompletedEvent>(
                            outboxMessage.Content)
                    ?? throw new JsonException(
                        "Outbox message content is empty or invalid.");
            }
            catch (Exception ex)
                when (ex is JsonException
                    or InvalidOperationException)
            {
                await RecordFailureAsync(
                    dbContext,
                    outboxMessage,
                    ex,
                    cancellationToken);

                continue;
            }

            try
            {
                await eventPublisher.PublishSessionCompletedAsync(
                    message,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await RecordFailureAsync(
                    dbContext,
                    outboxMessage,
                    ex,
                    cancellationToken);

                continue;
            }

            outboxMessage.ProcessedAtUtc = DateTimeOffset.UtcNow;
            outboxMessage.LastError = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Outbox message published successfully. " +
                "EventId: {EventId}",
                outboxMessage.Id);
        }
    }

    private async Task RecordFailureAsync(
        TrainingDbContext dbContext,
        OutboxMessage outboxMessage,
        Exception exception,
        CancellationToken cancellationToken)
    {
        outboxMessage.RetryCount++;

        outboxMessage.LastError =
            exception.Message.Length > 2000
                ? exception.Message[..2000]
                : exception.Message;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogError(
            exception,
            "Failed to publish Outbox message {EventId}. " +
            "Attempt: {RetryCount}",
            outboxMessage.Id,
            outboxMessage.RetryCount);
    }
}

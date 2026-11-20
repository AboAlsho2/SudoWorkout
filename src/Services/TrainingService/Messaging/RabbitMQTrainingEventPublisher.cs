
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TrainingService.Configuration;

namespace TrainingService.Messaging;

public sealed class RabbitMQTrainingEventPublisher(
    IOptions<RabbitMQOptions> options,
    ILogger<RabbitMQTrainingEventPublisher> logger)
    : ITrainingEventPublisher, IAsyncDisposable
{
    private readonly RabbitMQOptions _options = options.Value;

    private IConnection? _connection;
    private IChannel? _channel;

    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    public async Task PublishSessionCompletedAsync(
        TrainingSessionCompletedEvent message,
        CancellationToken cancellationToken = default)
    {
        await _publishLock.WaitAsync(cancellationToken);

        try
        {
            await EnsureInitializedAsync(cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = message.EventId.ToString(),
                Type = nameof(TrainingSessionCompletedEvent)
            };

            await _channel!.BasicPublishAsync(
                exchange: "sudo-workout.events",
                routingKey: "training.session.completed",
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "RabbitMQ confirmed publishing {EventType}. EventId: {EventId}",
                nameof(TrainingSessionCompletedEvent),
                message.EventId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish training session completion event. EventId: {EventId}",
                message.EventId);

            throw;
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task EnsureInitializedAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);

        try
        {
            if (_channel is { IsOpen: true })
            {
                return;
            }

            // Dispose the old channel before replacing it.
            if (_channel is not null)
            {
                await _channel.DisposeAsync();
                _channel = null;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password
            };

            try
            {
                _connection =
                    await factory.CreateConnectionAsync(cancellationToken);

                var channelOptions = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true);

                _channel = await _connection.CreateChannelAsync(
                    channelOptions,
                    cancellationToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: "sudo-workout.events",
                    type: ExchangeType.Direct,
                    durable: true,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                if (_channel is not null)
                {
                    await _channel.DisposeAsync();
                    _channel = null;
                }

                if (_connection is not null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                throw;
            }
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _publishLock.Dispose();
        _initializationLock.Dispose();
    }
}

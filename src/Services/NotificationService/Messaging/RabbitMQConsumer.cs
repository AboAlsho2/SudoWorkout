
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Messaging;

public sealed class RabbitMQConsumer(
    IOptions<RabbitMQOptions> options,
    ILogger<RabbitMQConsumer> logger)
    : BackgroundService
{
    private const string MainExchange = "sudo-workout.events";
    private const string MainQueue = "notification.training-session-completed";
    private const string RoutingKey = "training.session.completed";

    private const string RetryExchange = "sudo-workout.retry";
    private const string RetryQueue = "notification.training-session-completed.retry";
    private const string RetryRoutingKey = "training.session.completed.retry";

    private const string DeadLetterExchange = "sudo-workout.dead-letter";
    private const string DeadLetterQueue = "notification.training-session-completed.dlq";
    private const string DeadLetterRoutingKey = "training.session.completed.dead";

    private const string RetryCountHeader = "x-retry-count";
    private const int MaxRetries = 3;
    private const int RetryDelayMilliseconds = 10_000;
    private const ushort PrefetchCount = 10;

    private readonly RabbitMQOptions _options = options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password
        };

        await using var connection =
            await factory.CreateConnectionAsync(stoppingToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        await using var channel =
            await connection.CreateChannelAsync(
                channelOptions,
                stoppingToken);

        await DeclareTopologyAsync(channel, stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                // Validate that the message contains valid JSON.


                using var document = JsonDocument.Parse(args.Body);

                var message = Encoding.UTF8.GetString(
                    args.Body.ToArray());

                logger.LogInformation(
                    "Received RabbitMQ message: {Message}",
                    message);


                await channel.BasicAckAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Leave the delivery unacknowledged.
                // RabbitMQ can requeue it when the channel closes.
            }
            catch (Exception processingException)
            {
                logger.LogError(
                    processingException,
                    "Message processing failed.");

                try
                {
                    var headers = args.BasicProperties.Headers is null
                        ? new Dictionary<string, object?>()
                        : new Dictionary<string, object?>(
                            args.BasicProperties.Headers);

                    var retryCount = GetRetryCount(headers);

                    var destinationExchange = retryCount < MaxRetries
                        ? RetryExchange
                        : DeadLetterExchange;

                    var destinationRoutingKey = retryCount < MaxRetries
                        ? RetryRoutingKey
                        : DeadLetterRoutingKey;

                    if (retryCount < MaxRetries)
                    {
                        headers[RetryCountHeader] = retryCount + 1;
                    }

                    var properties = new BasicProperties
                    {
                        ContentType =
                            args.BasicProperties.ContentType
                            ?? "application/json",
                        DeliveryMode = DeliveryModes.Persistent,
                        MessageId = args.BasicProperties.MessageId
                            ?? Guid.NewGuid().ToString(),
                        Type = args.BasicProperties.Type,
                        Headers = headers
                    };

                    // Publish the replacement before acknowledging
                    // the original delivery.
                    await channel.BasicPublishAsync(
                        exchange: destinationExchange,
                        routingKey: destinationRoutingKey,
                        mandatory: true,
                        basicProperties: properties,
                        body: args.Body,
                        cancellationToken: stoppingToken);

                    await channel.BasicAckAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    if (retryCount < MaxRetries)
                    {
                        logger.LogWarning(
                            "Message scheduled for retry {Retry}/{MaxRetries}.",
                            retryCount + 1,
                            MaxRetries);
                    }
                    else
                    {
                        logger.LogError(
                            "Message moved to the dead-letter queue after {MaxRetries} retries.",
                            MaxRetries);
                    }
                }
                catch (Exception publishException)
                {
                    logger.LogError(
                        publishException,
                        "Could not forward failed message. " +
                        "Leaving the original delivery unacknowledged.");

                    // Do not ACK the original if forwarding failed.
                    // Closing the channel allows RabbitMQ to requeue
                    // any unacknowledged deliveries.
                    throw;
                }
            }
        };

        await channel.BasicConsumeAsync(
            queue: MainQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "RabbitMQ consumer is listening. Prefetch: {PrefetchCount}",
            PrefetchCount);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "RabbitMQ consumer is shutting down.");
        }
    }

    private static int GetRetryCount(
        IDictionary<string, object?> headers)
    {
        if (!headers.TryGetValue(RetryCountHeader, out var value)
            || value is null)
        {
            return 0;
        }

        return value switch
        {
            byte number => number,
            short number => number,
            int number => number,
            long number when number <= int.MaxValue
                => (int)number,
            _ when int.TryParse(
                Convert.ToString(value, CultureInfo.InvariantCulture),
                out var number) => number,
            _ => 0
        };
    }

    private static async Task DeclareTopologyAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: MainExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: MainQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: MainQueue,
            exchange: MainExchange,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: RetryExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        var retryArguments = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = RetryDelayMilliseconds,
            ["x-dead-letter-exchange"] = MainExchange,
            ["x-dead-letter-routing-key"] = RoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: RetryQueue,
            exchange: RetryExchange,
            routingKey: RetryRoutingKey,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: DeadLetterRoutingKey,
            cancellationToken: cancellationToken);
    }
}

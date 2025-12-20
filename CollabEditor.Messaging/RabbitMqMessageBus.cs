using System.Text;
using System.Text.Json;
using CollabEditor.Application.Interfaces;
using CollabEditor.Messaging.Configuration;
using CollabEditor.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CollabEditor.Messaging;

public sealed class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    
    private readonly RabbitMqOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly AsyncLock _channelLock = new();

    public RabbitMqMessageBus(
        RabbitMqOptions options,
        IConnection connection,
        IChannel channel,
        ILogger<RabbitMqMessageBus> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _logger.LogInformation(
            "RabbitMQ MessageBus created. Exchange: {ExchangeName}, Type: {ExchangeType}",
            _options.ExchangeName,
            _options.ExchangeType);
    }

    public async Task PublishAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            using (await _channelLock.LockAsync())
            {
                await _channel.BasicPublishAsync(
                    exchange: _options.ExchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cancellationToken
                );
            }

            _logger.LogDebug(
                "Published message to {RoutingKey}: {MessageType}",
                routingKey,
                typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to {RoutingKey}", routingKey);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(
        string routingPattern,
        Func<T, Task> handler,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var queueName = $"{_options.ExchangeName}.{routingPattern}.{Guid.NewGuid()}";
            
            using (await _channelLock.LockAsync())
            {
                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: false,
                    exclusive: true,
                    autoDelete: true,
                    arguments: null,
                    cancellationToken: cancellationToken
                );

                await _channel.QueueBindAsync(
                    queue: queueName,
                    exchange: _options.ExchangeName,
                    routingKey: routingPattern,
                    arguments: null,
                    cancellationToken: cancellationToken
                );

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += (_, ea) =>
                    HandleReceivedMessageAsync(ea, handler, cancellationToken);

                await _channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: cancellationToken
                );
            }

            _logger.LogInformation(
                "Subscribed to {RoutingPattern} (queue: {QueueName})",
                routingPattern,
                queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to {RoutingPattern}", routingPattern);
            throw;
        }
    }

    private async Task HandleReceivedMessageAsync<T>(
        BasicDeliverEventArgs eventArgs,
        Func<T, Task> handler,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            var body = eventArgs.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<T>(json, _jsonOptions);

            if (message is not null)
            {
                _logger.LogDebug(
                    "Received message from {RoutingKey}: {MessageType}",
                    eventArgs.RoutingKey,
                    typeof(T).Name);

                await handler(message);
                await AcknowledgeMessageAsync(eventArgs.DeliveryTag, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing message from {RoutingKey}",
                eventArgs.RoutingKey);

            await RejectMessageAsync(eventArgs.DeliveryTag, cancellationToken);
        }
    }

    private async Task AcknowledgeMessageAsync(ulong deliveryTag, CancellationToken cancellationToken)
    {
        using (await _channelLock.LockAsync())
        {
            await _channel.BasicAckAsync(
                deliveryTag: deliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);
        }
    }

    private async Task RejectMessageAsync(ulong deliveryTag, CancellationToken cancellationToken)
    {
        using (await _channelLock.LockAsync())
        {
            await _channel.BasicNackAsync(
                deliveryTag: deliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _channelLock.Dispose();
        _logger.LogInformation("RabbitMQ MessageBus disposed");
    }
}
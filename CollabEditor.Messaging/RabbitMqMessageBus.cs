using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using CollabEditor.Application.Interfaces;
using CollabEditor.Messaging.Configuration;
using CollabEditor.Messaging.Policies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CollabEditor.Messaging;

public sealed class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private readonly IConnection _connection;
    
    private readonly RabbitMqOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private readonly ObjectPool<IChannel> _publishChannelPool;
    private readonly ConcurrentBag<IChannel> _consumerChannels = [];
    private bool _disposed;

    public RabbitMqMessageBus(
        IOptions<RabbitMqOptions> options,
        IConnection connection,
        ILogger<RabbitMqMessageBus> logger)
    {
        _connection = connection;
        _logger = logger;
        _options = options.Value;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        var poolPolicy = new ChannelPoolPolicy(_connection, logger);
        _publishChannelPool = new DefaultObjectPool<IChannel>(poolPolicy, 10);
        
        _logger.LogInformation("RabbitMQ MessageBus initialized with channel pooling (10 channels)");
    }

    /// <summary>
    /// Channel pool is used for message publishing which reuses channels
    /// instead of having costly create channel operations everytime
    /// </summary>
    public async Task PublishAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default) where T : IMessage
    {
        var channel = _publishChannelPool.Get();

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

            await channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to {RoutingKey}", routingKey);
            throw;
        }
        finally
        {
            _publishChannelPool.Return(channel);
        }
    }

    /// <summary>
    /// Handlers are long-lived so for each handler we create a separate Channel.
    /// </summary>
    public async Task SubscribeAsync<T>(
        string routingPattern,
        Func<T, Task> handler,
        CancellationToken cancellationToken = default) where T : IMessage
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        _consumerChannels.Add(channel);
        
        try
        {
            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: cancellationToken);
            
            var processId = Environment.ProcessId;
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var queueName = $"{_options.ExchangeName}.{routingPattern}.{processId}-{uniqueId}";
            
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null,
                cancellationToken: cancellationToken
            );

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: _options.ExchangeName,
                routingKey: routingPattern,
                arguments: null,
                cancellationToken: cancellationToken
            );

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, ea) =>
                HandleReceivedMessageAsync(channel, ea, handler, cancellationToken);

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken
            );
            
            _logger.LogInformation(
                "Subscribed to {RoutingPattern} (queue: {QueueName})",
                routingPattern,
                queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to {RoutingPattern}", routingPattern);
            
            await channel.CloseAsync(cancellationToken: cancellationToken);
            await channel.DisposeAsync();
            
            _consumerChannels.TryTake(out _);
            throw;
        }
    }

    private async Task HandleReceivedMessageAsync<T>(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        Func<T, Task> handler,
        CancellationToken cancellationToken) where T : IMessage
    {
        try
        {
            var body = eventArgs.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<T>(json, _jsonOptions);

            if (message is not null)
            {
                await handler(message);
                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await channel.BasicNackAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: cancellationToken);
            _logger.LogError(ex, "Error processing message from {RoutingKey}", eventArgs.RoutingKey);
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        
        foreach (var channel in _consumerChannels)
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync();
                }
                
                await channel.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing consumer channel");
            }
        }

        _logger.LogInformation("RabbitMQ MessageBus disposed");
    }
}
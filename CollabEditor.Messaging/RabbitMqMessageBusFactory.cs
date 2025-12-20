using CollabEditor.Application.Interfaces;
using CollabEditor.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CollabEditor.Messaging;

public sealed class RabbitMqMessageBusFactory(
    IOptions<RabbitMqOptions> settings,
    ILogger<RabbitMqMessageBus> logger) : IMessageBusFactory
{
    private readonly RabbitMqOptions _options = settings.Value;
    
    public async Task<IMessageBus> CreateAsync(CancellationToken cancellationToken = default)
    {
        var connection = await CreateConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await DeclareExchangeAsync(channel, cancellationToken);
        return new RabbitMqMessageBus(
            _options,
            connection,
            channel,
            logger);
    }
    
    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            
            // Connection reliability settings
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            
            // Performance settings
            RequestedChannelMax = 2047,
            RequestedFrameMax = 131072
        };

        try
        {
            var connection = await factory.CreateConnectionAsync(
                "CollabEditorService", 
                cancellationToken);
            
            logger.LogInformation(
                "Connected to RabbitMQ at {HostName}:{Port}",
                _options.HostName,
                _options.Port);
            
            return connection;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to RabbitMQ at {HostName}:{Port}",
                _options.HostName,
                _options.Port);
            throw;
        }
    }
    
    private async Task DeclareExchangeAsync(IChannel channel, CancellationToken cancellationToken)
    {
        try
        {
            await channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: _options.ExchangeType,
                durable: true,          // Survives broker restart
                autoDelete: false,      // Don't delete when unused
                arguments: null,
                cancellationToken: cancellationToken
            );

            logger.LogInformation(
                "Declared exchange: {ExchangeName} (type: {ExchangeType})",
                _options.ExchangeName,
                _options.ExchangeType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to declare exchange {ExchangeName}",
                _options.ExchangeName);
            throw;
        }
    }
}
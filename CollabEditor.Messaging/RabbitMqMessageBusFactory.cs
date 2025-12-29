using CollabEditor.Application.Interfaces;
using CollabEditor.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CollabEditor.Messaging;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
public sealed class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    
    public RabbitMqConnectionFactory(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConnectionFactory> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating RabbitMQ connection to {HostName}:{Port}...",
            _options.HostName,
            _options.Port);

        var factory = CreateConnectionFactory();

        try
        {
            var connection = await factory.CreateConnectionAsync(
                "CollabEditorService",
                cancellationToken);

            _logger.LogInformation(
                "Successfully connected to RabbitMQ at {HostName}:{Port} (VHost: {VirtualHost})",
                _options.HostName,
                _options.Port,
                _options.VirtualHost);

            // Set up connection event handlers for monitoring
            ConfigureConnectionEvents(connection);

            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to connect to RabbitMQ at {HostName}:{Port}",
                _options.HostName,
                _options.Port);
            throw new InvalidOperationException(
                $"Failed to establish RabbitMQ connection to {_options.HostName}:{_options.Port}",
                ex);
        }
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            TopologyRecoveryEnabled = true,

            // Heartbeat to detect dead connections
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            
            RequestedChannelMax = 2047,   
            RequestedFrameMax = 131072,   

            // Timeouts
            ContinuationTimeout = TimeSpan.FromSeconds(20),
            HandshakeContinuationTimeout = TimeSpan.FromSeconds(10),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
            SocketReadTimeout = TimeSpan.FromSeconds(30),
            SocketWriteTimeout = TimeSpan.FromSeconds(30)
        };
    }

    private void ConfigureConnectionEvents(IConnection connection)
    {
        connection.ConnectionShutdownAsync += (sender, args) =>
        {
            _logger.LogWarning(
                "RabbitMQ connection shutdown: {Reason}",
                args.ReplyText);
            return Task.CompletedTask;
        };

        connection.CallbackExceptionAsync += (sender, args) =>
        {
            _logger.LogError(args.Exception,
                "RabbitMQ connection callback exception");
            return Task.CompletedTask;
        };

        connection.ConnectionBlockedAsync += (sender, args) =>
        {
            _logger.LogWarning(
                "RabbitMQ connection blocked: {Reason}",
                args.Reason);
            return Task.CompletedTask;
        };

        connection.ConnectionUnblockedAsync += (sender, args) =>
        {
            _logger.LogInformation("RabbitMQ connection unblocked");
            return Task.CompletedTask;
        };

        connection.RecoverySucceededAsync += (sender, args) =>
        {
            _logger.LogInformation("RabbitMQ connection recovery succeeded");
            return Task.CompletedTask;
        };

        connection.ConnectionRecoveryErrorAsync += (sender, args) =>
        {
            _logger.LogError(args.Exception,
                "RabbitMQ connection recovery error");
            return Task.CompletedTask;
        };
    }
}
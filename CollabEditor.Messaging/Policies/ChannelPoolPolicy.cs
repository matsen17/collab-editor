using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;

namespace CollabEditor.Messaging.Policies;

public sealed class ChannelPoolPolicy : IPooledObjectPolicy<IChannel>
{
    private readonly IConnection _connection;
    private readonly ILogger _logger;

    public ChannelPoolPolicy(IConnection connection, ILogger logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Called by the pool when it needs to create a new channel.
    /// </summary>
    public IChannel Create()
    {
        try
        {
            // This is the expensive operation we want to avoid repeating!
            var channel = _connection.CreateChannelAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            
            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ channel");
            throw;
        }
    }

    /// <summary>
    /// Called by the pool when a channel is returned.
    /// Return true to keep in pool, false to dispose it.
    /// </summary>
    public bool Return(IChannel obj)
    {
        if (obj.IsOpen)
        {
            _logger.LogTrace("Channel returned to pool (healthy)");
            return true;
        }
        
        _logger.LogWarning("Channel is closed/broken, removing from pool");
        TryRemoveFromPool(obj);
        
        return false;
    }

    private void TryRemoveFromPool(IChannel obj)
    {
        try
        {
            obj.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            obj.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error disposing broken channel (ignored)");
        }
    }
}
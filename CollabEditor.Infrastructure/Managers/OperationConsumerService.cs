using System.Text.Json;
using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Infrastructure.WebSockets;
using CollabEditor.Infrastructure.WebSockets.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OperationMessage = CollabEditor.Application.Messages.OperationMessage;

namespace CollabEditor.Infrastructure.Managers;

public sealed class OperationConsumerService : BackgroundService
{
    private readonly ILogger<OperationConsumerService> _logger;
    private readonly IMessageBus _messageBus;
    private readonly IWebSocketConnectionManager _connectionManager;
    
    private Task? _executingTask;
    private CancellationTokenSource? _stoppingCts;

    public OperationConsumerService(
        IMessageBus messageBus, 
        IWebSocketConnectionManager connectionManager, 
        ILogger<OperationConsumerService> logger)
    {
        _messageBus = messageBus;
        _connectionManager = connectionManager;
        _logger = logger;
    }
    
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Operation Consumer Manager starting...");

        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_stoppingCts.Token);

        return _executingTask.IsCompleted 
            ? _executingTask 
            : Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _messageBus.SubscribeAsync<OperationMessage>(
                "session.operations",
                HandleOperationMessageAsync,
                stoppingToken);

            _logger.LogInformation("Operation Consumer Manager started and listening for messages");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation Consumer Manager is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation Consumer Manager encountered an error");
            throw;
        }
    }
    
    private async Task HandleOperationMessageAsync(OperationMessage message)
    {
        try
        {
            _logger.LogDebug(
                "Received operation message for session {SessionId}, version {Version}",
                message.SessionId,
                message.Operation.Version);

            var sessionId = SessionId.From(message.SessionId);
            var connectionCount = _connectionManager.GetConnectionCount(sessionId);

            if (connectionCount == 0)
            {
                _logger.LogDebug(
                    "No active connections for session {SessionId}, skipping broadcast",
                    message.SessionId);
                return;
            }

            var broadcastMessage = new OperationAppliedMessage
            {
                SessionId = message.SessionId,
                Operation = new OperationDto
                {
                    Type = message.Operation.Type,
                    Position = message.Operation.Position,
                    Text = message.Operation.Text,
                    Length = message.Operation.Length,
                    Version = message.Operation.Version,
                    AuthorId = message.Operation.AuthorId
                }
            };

            await _connectionManager.BroadcastToSessionAsync(
                sessionId,
                broadcastMessage);

            _logger.LogDebug(
                "Broadcasted operation to {ConnectionCount} connections for session {SessionId}",
                connectionCount,
                message.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling operation message for session {SessionId}",
                message.SessionId);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Operation Consumer Manager stopping");
        await base.StopAsync(cancellationToken);
    }
}
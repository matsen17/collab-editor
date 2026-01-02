using System.Text.Json;
using CollabEditor.Application.Commands;
using CollabEditor.Application.Interfaces;
using CollabEditor.Application.Models;
using CollabEditor.Application.Queries;
using CollabEditor.Domain.ValueObjects;
using CollabEditor.Infrastructure.WebSockets.Models;
using CollabEditor.Utilities.Results;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using InfrastructureMessages = CollabEditor.Infrastructure.Messages;

namespace CollabEditor.Infrastructure.WebSockets;

public sealed class WebSocketMessageHandler : IWebSocketMessageHandler
{
    private readonly ILogger<WebSocketMessageHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IWebSocketConnectionManager _connectionManager;
    
    private readonly JsonSerializerOptions _jsonOptions;

    public WebSocketMessageHandler(IMediator mediator,
        IWebSocketConnectionManager connectionManager,
        ILogger<WebSocketMessageHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
        _connectionManager = connectionManager;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    public async Task HandleMessageAsync(string messageJson, ParticipantId participantId)
    {
        try
        {
            using var doc = JsonDocument.Parse(messageJson);
            if (!doc.RootElement.TryGetProperty("type", out var typeElement))
            {
                await SendErrorAsync(participantId, "Message type is required");
                return;
            }

            var messageType = typeElement.GetString();

            switch (messageType)
            {
                case "join":
                    await HandleJoinAsync(messageJson, participantId);
                    break;
                
                case "leave":
                    await HandleLeaveAsync(messageJson, participantId);
                    break;
                
                case "ping":
                    await HandlePingAsync(participantId);
                    break;

                case "operation":
                    await HandleOperationAsync(messageJson, participantId);
                    break;

                default:
                    await SendErrorAsync(participantId, $"Unknown message type: {messageType}");
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON message from participant {ParticipantId}", participantId);
            await SendErrorAsync(participantId, "Invalid message format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from participant {ParticipantId}", participantId);
            await SendErrorAsync(participantId, "Internal server error");
        }
    }
    
    private async Task HandleJoinAsync(string messageJson, ParticipantId participantId)
    {
        var message = JsonSerializer.Deserialize<JoinMessage>(messageJson, _jsonOptions);
        
        if (message is null)
        {
            await SendErrorAsync(participantId, "Invalid join message");
            return;
        }

        var command = new JoinSessionCommand
        {
            SessionId = SessionId.From(message.SessionId),
            ParticipantId = participantId,
            Name = message.Name
        };

        var result = await _mediator.Send(command);
        await result.OnFailureAsync(async errors =>
        {
            await SendErrorAsync(participantId, errors.First());
            await _connectionManager.RemoveConnectionAsync(participantId);
        });
    }

    private async Task HandleOperationAsync(string messageJson, ParticipantId participantId)
    {
        var message = JsonSerializer.Deserialize<OperationMessage>(messageJson, _jsonOptions);
        
        if (message is null)
        {
            await SendErrorAsync(participantId, "Invalid operation message");
            return;
        }
        
        var operation = message.Operation.Type.ToLowerInvariant() switch
        {
            "insert" => TextOperation.Insert(
                message.Operation.Position,
                message.Operation.Text ?? string.Empty,
                message.Operation.Version,
                participantId),
            "delete" => TextOperation.Delete(
                message.Operation.Position,
                message.Operation.Length ?? 0,
                message.Operation.Version,
                participantId),
            _ => null
        };

        if (operation is null)
        {
            await SendErrorAsync(participantId, "Invalid operation type");
            return;
        }

        var result = await _mediator.Send(new ApplyOperationCommand
        {
            SessionId = SessionId.From(message.SessionId),
            Operation = operation
        });
        
        await result
            .OnFailureAsync(async errors => await SendErrorAsync(participantId, errors.First()));
    }

    private async Task HandleLeaveAsync(string messageJson, ParticipantId participantId)
    {
        var message = JsonSerializer.Deserialize<LeaveMessage>(messageJson, _jsonOptions);
        
        if (message is null)
        {
            return;
        }
        
        var result = await _mediator.Send(new LeaveSessionCommand
        {
            SessionId = SessionId.From(message.SessionId),
            ParticipantId = participantId
        });
        
        result.OnFailure(_ =>
        {
            _logger.LogWarning(
                "Failed to remove participant {ParticipantId} from session {SessionId}: {Error}",
                participantId, message.SessionId, result.Errors[0].Message);
        });
    }

    private async Task HandlePingAsync(ParticipantId participantId) =>
        await _connectionManager.SendToParticipantAsync(participantId, new PongMessage());

    private async Task SendErrorAsync(ParticipantId participantId, IError error)
    {
        var errorCode = error.Metadata.TryGetValue("ErrorCode", out var code) 
            ? code?.ToString() 
            : null;
        
        var errorMessage = new ErrorMessage
        {
            Error = error.Message,
            ErrorCode = errorCode
        };

        await _connectionManager.SendToParticipantAsync(participantId, errorMessage);
    }
    
    private async Task SendErrorAsync(ParticipantId participantId, string errorString)
    {
        var errorMessage = new ErrorMessage
        {
            Error = errorString,
            ErrorCode = string.Empty
        };

        await _connectionManager.SendToParticipantAsync(participantId, errorMessage);
    }
}
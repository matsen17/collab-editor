using CollabEditor.Application.Interfaces;
using CollabEditor.Infrastructure.Managers;
using CollabEditor.Infrastructure.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OperationAppliedMessage = CollabEditor.Infrastructure.Messages.OperationAppliedMessage;

namespace CollabEditor.Infrastructure.Services;

public sealed class MessageHandlerSubscriptionService : BackgroundService
{
    private readonly ILogger<MessageHandlerSubscriptionService> _logger;
    private readonly IMessageBus _messageBus;
    private readonly IServiceProvider _serviceProvider;

    public MessageHandlerSubscriptionService(
        IMessageBus messageBus, 
        IServiceProvider serviceProvider,
        ILogger<MessageHandlerSubscriptionService> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // TODO: This is currently manual. Future improvement idea: make this register all handlers automatically.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message Handler Subscription Service starting...");

        try
        {
            await SubscribeHandler<OperationAppliedMessage, OperationFlowManager>(stoppingToken);
            
            await SubscribeHandler<ParticipantJoinedMessage, SessionFlowManager>(stoppingToken);
            await SubscribeHandler<ParticipantLeftMessage, SessionFlowManager>(stoppingToken);
            
            _logger.LogInformation("All message handlers subscribed successfully");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message Handler Subscription Service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message Handler Subscription Service encountered an error");
            throw;
        }
    }
    
    private async Task SubscribeHandler<TMessage, THandler>(CancellationToken cancellationToken)
        where TMessage : IMessage
        where THandler : IHandle<TMessage>
    {
        var routingKey = TMessage.RoutingKey;

        await _messageBus.SubscribeAsync<TMessage>(
            routingKey,
            async message =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                
                await handler.HandleAsync(message, cancellationToken);
            },
            cancellationToken);

        _logger.LogInformation("Subscribed {HandlerType} to {RoutingKey}", typeof(THandler).Name, routingKey);
    }
}
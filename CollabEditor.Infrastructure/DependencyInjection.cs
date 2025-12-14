using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Services;
using CollabEditor.Infrastructure.Repositories;
using CollabEditor.Infrastructure.Services;
using CollabEditor.Infrastructure.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace CollabEditor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Repositories
        services.AddSingleton<IEditSessionRepository, InMemoryEditSessionRepository>();
        
        // Domain Services
        services.AddSingleton<IOperationalTransformer, SimpleOperationalTransformer>();
        
        // WebSocket Services
        services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
        services.AddScoped<IWebSocketMessageHandler, WebSocketMessageHandler>();
        
        return services;
    }
}
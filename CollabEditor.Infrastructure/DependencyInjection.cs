using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Services;
using CollabEditor.Infrastructure.Managers;
using CollabEditor.Infrastructure.Persistence;
using CollabEditor.Infrastructure.Repositories;
using CollabEditor.Infrastructure.Services;
using CollabEditor.Infrastructure.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CollabEditor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextFactory<CollabEditorDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("CollabEditor.Infrastructure")));

        // Repositories
        services.AddScoped<IEditSessionRepository, SqlEditSessionRepository>();
        
        // Domain Services
        services.AddSingleton<IOperationalTransformer, SimpleOperationalTransformer>();
        
        // WebSocket Services
        services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
        services.AddScoped<IWebSocketMessageHandler, WebSocketMessageHandler>();
        
        // Hosted Services
        services.AddHostedService<OperationConsumerService>();
        
        return services;
    }
}
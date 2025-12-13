using CollabEditor.Application.Interfaces;
using CollabEditor.Domain.Services;
using CollabEditor.Infrastructure.Repositories;
using CollabEditor.Infrastructure.Services;
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
        
        return services;
    }
}
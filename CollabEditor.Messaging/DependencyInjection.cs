using CollabEditor.Application.Interfaces;
using CollabEditor.Messaging.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CollabEditor.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOptions<RabbitMqOptions>>(serviceProvider =>
        {
            var settings = new RabbitMqOptions();
            configuration.GetSection(RabbitMqOptions.SectionName).Bind(settings);
            return Options.Create(settings);
        });
        
        services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        
        services.AddSingleton<IConnection>(sp =>
        {
            var factory = sp.GetRequiredService<IRabbitMqConnectionFactory>();
            return factory.CreateConnectionAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        });

        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

        return services;
    }
}
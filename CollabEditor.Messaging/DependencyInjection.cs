using CollabEditor.Application.Interfaces;
using CollabEditor.Messaging.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        
        services.AddSingleton<IMessageBusFactory, RabbitMqMessageBusFactory>();

        // Lazy init. Creates on first use.
        services.AddSingleton(serviceProvider =>
        {
            var factory = serviceProvider.GetRequiredService<IMessageBusFactory>();
            return new Lazy<Task<IMessageBus>>(async () => await factory.CreateAsync());
        });

        services.AddSingleton<IMessageBus>(serviceProvider =>
        {
            var lazyMessageBus = serviceProvider.GetRequiredService<Lazy<Task<IMessageBus>>>();
            return lazyMessageBus.Value.GetAwaiter().GetResult();
        });

        return services;
    }
}
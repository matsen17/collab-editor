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
            var connection = factory.CreateConnectionAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            // Ensure exchange exists (idempotent - safe to call even if exists)
            var logger = sp.GetRequiredService<ILogger<IConnection>>();
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            EnsureExchangeExists(connection, options, logger).GetAwaiter().GetResult();

            return connection;
        });

        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

        return services;
    }

    private static async Task EnsureExchangeExists(
        IConnection connection,
        RabbitMqOptions options,
        ILogger logger)
    {
        try
        {
            logger.LogInformation(
                "Ensuring RabbitMQ exchange '{ExchangeName}' exists...",
                options.ExchangeName);

            await using var channel = await connection.CreateChannelAsync();

            // Declare exchange (idempotent - safe to call even if it exists)
            await channel.ExchangeDeclareAsync(
                exchange: options.ExchangeName,
                type: options.ExchangeType,
                durable: true,
                autoDelete: false,
                arguments: null
            );

            logger.LogInformation(
                "RabbitMQ exchange '{ExchangeName}' (type: {ExchangeType}) is ready",
                options.ExchangeName,
                options.ExchangeType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to declare RabbitMQ exchange '{ExchangeName}'",
                options.ExchangeName);
            throw;
        }
    }
}
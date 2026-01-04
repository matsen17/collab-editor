namespace CollabEditor.API.HealthChecks;

public static class RegistrationExtensions
{
    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "postgres",
                tags: ["ready", "db"])
            .AddRabbitMQ(
                name: "rabbitmq",
                tags: ["ready", "messaging"]);

        return services;
    }
    
    public static void MapAppHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false  // Liveness only checks if the process is running, no dependencies
        });
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventBus.RabbitMQ;

/// <summary>
/// Extension methods for registering EventBus with DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQEventBus(
        this IServiceCollection services,
        string hostname,
        string exchangeName = "microbank_events")
    {
        services.AddSingleton<IEventBus>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMQEventBus>>();
            return new RabbitMQEventBus(hostname, exchangeName, sp, logger);
        });

        return services;
    }
}

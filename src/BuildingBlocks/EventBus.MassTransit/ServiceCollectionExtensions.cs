using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.MassTransit;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MassTransit with RabbitMQ transport
    /// </summary>
    public static IServiceCollection AddMassTransitWithRabbitMQ(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddMassTransit(x =>
        {
            // Register consumers if provided
            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
                
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                // Configure retry policy (with interval for concurrency scenarios)
                cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromMilliseconds(200)));

                // Configure error handling
                cfg.UseInMemoryOutbox();

                // Auto-configure endpoints for all registered consumers
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}

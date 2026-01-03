using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Common.Logging;

/// <summary>
/// Extension methods for configuring Serilog
/// </summary>
public static class LoggingExtensions
{
    public static IHostBuilder UseCustomSerilog(this IHostBuilder builder, string serviceName)
    {
        return builder.UseSerilog((context, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}");
        });
    }
}

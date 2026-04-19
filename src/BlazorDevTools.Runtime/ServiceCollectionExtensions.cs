using BlazorDevTools.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorDevTools.Runtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorDevToolsRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var version = typeof(ServiceCollectionExtensions).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        services.TryAddSingleton(new DevToolsHandshake(version));
        services.TryAddScoped<ComponentTracker>();
        services.TryAddScoped<DevToolsSnapshotBridge>();
        services.TryAddScoped<DevToolsAutoRefreshScheduler>();

        return services;
    }
}

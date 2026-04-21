using BlazorDevTools.Protocol;
using Microsoft.AspNetCore.Components;
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
        services.TryAddSingleton<IDevToolsComponentProxyRegistry, DevToolsComponentProxyRegistry>();
        services.TryAddScoped<ComponentTracker>();
        services.TryAddScoped<DevToolsSnapshotBridge>();
        services.TryAddScoped<DevToolsAutoRefreshScheduler>();
        services.TryAddScoped<IDevToolsExternalComponentTracker, DevToolsExternalComponentTracker>();
        services.Replace(ServiceDescriptor.Singleton<IComponentActivator, DevToolsComponentActivator>());

        return services;
    }

    public static IServiceCollection AddBlazorDevToolsComponentProxy<TComponent, TProxy>(this IServiceCollection services)
        where TComponent : IComponent
        where TProxy : class, IComponent, IDevToolsComponentProxy
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!typeof(TComponent).IsAssignableFrom(typeof(TProxy)))
        {
            throw new InvalidOperationException($"Proxy type '{typeof(TProxy).FullName}' must inherit from '{typeof(TComponent).FullName}'.");
        }

        services.AddSingleton(new DevToolsComponentProxyRegistration(typeof(TComponent), typeof(TProxy)));

        return services;
    }
}

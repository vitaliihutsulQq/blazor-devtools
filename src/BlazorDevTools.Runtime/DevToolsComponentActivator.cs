using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorDevTools.Runtime;

public sealed class DevToolsComponentActivator : IComponentActivator
{
    private readonly IServiceProvider serviceProvider;
    private readonly IDevToolsComponentProxyRegistry proxyRegistry;

    public DevToolsComponentActivator(IServiceProvider serviceProvider, IDevToolsComponentProxyRegistry proxyRegistry)
    {
        this.serviceProvider = serviceProvider;
        this.proxyRegistry = proxyRegistry;
    }

    public IComponent CreateInstance(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var resolvedType = proxyRegistry.TryGetProxyType(componentType, out var proxyType)
            ? proxyType
            : componentType;

        return (IComponent)ActivatorUtilities.CreateInstance(serviceProvider, resolvedType);
    }
}

using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BlazorDevTools.Runtime.Tests;

public class DevToolsComponentActivatorTests
{
    [Test]
    public void CreateInstance_returns_registered_proxy_when_mapping_exists()
    {
        var services = new ServiceCollection();
        services.AddScoped<IJSRuntime, TestJsRuntime>();
        services.AddBlazorDevToolsRuntime();
        services.AddBlazorDevToolsComponentProxy<TestComponent, TestComponentProxy>();

        using var provider = services.BuildServiceProvider();
        var activator = provider.GetRequiredService<IComponentActivator>();

        var component = activator.CreateInstance(typeof(TestComponent));

        Assert.That(component, Is.TypeOf<TestComponentProxy>());
    }

    [Test]
    public void CreateInstance_returns_original_component_when_no_mapping_exists()
    {
        var services = new ServiceCollection();
        services.AddScoped<IJSRuntime, TestJsRuntime>();
        services.AddBlazorDevToolsRuntime();

        using var provider = services.BuildServiceProvider();
        var activator = provider.GetRequiredService<IComponentActivator>();

        var component = activator.CreateInstance(typeof(PlainComponent));

        Assert.That(component, Is.TypeOf<PlainComponent>());
    }

    [Test]
    public void CreateInstance_uses_generated_manifest_mapping_when_available()
    {
        var services = new ServiceCollection();
        services.AddScoped<IJSRuntime, TestJsRuntime>();
        services.AddBlazorDevToolsRuntime();

        using var provider = services.BuildServiceProvider();
        var activator = provider.GetRequiredService<IComponentActivator>();

        var component = activator.CreateInstance(typeof(GeneratedComponent));

        Assert.That(component, Is.TypeOf<GeneratedComponentProxy>());
    }

    private class TestComponent : ComponentBase;

    private sealed class TestComponentProxy : TestComponent, IDevToolsComponentProxy
    {
        public Type DevToolsOriginalComponentType => typeof(TestComponent);

        public void Dispose()
        {
        }
    }

    private sealed class PlainComponent : ComponentBase;

    private class GeneratedComponent : ComponentBase;

    private sealed class GeneratedComponentProxy : GeneratedComponent, IDevToolsComponentProxy
    {
        public Type DevToolsOriginalComponentType => typeof(GeneratedComponent);

        public void Dispose()
        {
        }
    }

    public sealed class TestGeneratedManifest : IDevToolsComponentProxyManifest
    {
        public IReadOnlyList<DevToolsComponentProxyRegistration> GetRegistrations()
        {
            return [new(typeof(GeneratedComponent), typeof(GeneratedComponentProxy))];
        }
    }

    private sealed class TestJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}

using BlazorDevTools.Protocol;
using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BlazorDevTools.Runtime.Tests;

public class RuntimeRegistrationTests
{
    [Test]
    public void AddBlazorDevToolsRuntime_registers_runtime_services()
    {
        var services = new ServiceCollection();
        services.AddScoped<IJSRuntime, TestJsRuntime>();

        services.AddBlazorDevToolsRuntime();

        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<DevToolsHandshake>().RuntimeVersion, Is.Not.Empty);
        Assert.That(provider.GetRequiredService<ComponentTracker>(), Is.Not.Null);
        Assert.That(provider.GetRequiredService<DevToolsSnapshotBridge>(), Is.Not.Null);
        Assert.That(provider.GetRequiredService<DevToolsAutoRefreshScheduler>(), Is.Not.Null);
        Assert.That(provider.GetRequiredService<IComponentActivator>(), Is.Not.Null);
        Assert.That(provider.GetRequiredService<IDevToolsComponentProxyRegistry>(), Is.Not.Null);
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

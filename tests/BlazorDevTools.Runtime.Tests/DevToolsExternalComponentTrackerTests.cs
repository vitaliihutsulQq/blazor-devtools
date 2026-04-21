using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Radzen;

namespace BlazorDevTools.Runtime.Tests;

public class DevToolsExternalComponentTrackerTests
{
    [Test]
    public void Radzen_component_dialogs_register_synthetic_root_and_attach_component_as_child()
    {
        var services = new ServiceCollection();
        services.AddScoped<NavigationManager, TestNavigationManager>();
        services.AddScoped<IJSRuntime, TestJsRuntime>();
        services.AddScoped<ComponentTracker>();
        services.AddScoped<DevToolsSnapshotBridge>();
        services.AddScoped<DevToolsAutoRefreshScheduler>();
        services.AddScoped<IDevToolsExternalComponentTracker, DevToolsExternalComponentTracker>();
        services.AddRadzenComponents();

        using var provider = services.BuildServiceProvider();
        var tracker = provider.GetRequiredService<ComponentTracker>();
        var scheduler = provider.GetRequiredService<DevToolsAutoRefreshScheduler>();
        var externalTracker = provider.GetRequiredService<IDevToolsExternalComponentTracker>();
        var dialogService = provider.GetRequiredService<DialogService>();

        externalTracker.EnsureInitialized();
        dialogService.Open("Matter Search", typeof(TestDialogComponent), new Dictionary<string, object?> { ["CaseId"] = 42 }, new DialogOptions());

        var lifecycle = new DevToolsTrackedComponentLifecycle(tracker, scheduler, externalTracker);
        lifecycle.ApplySnapshot(typeof(TestDialogComponent), parentComponentId: null, [new("CaseId", "42")]);

        var snapshot = tracker.BuildSnapshot();

        Assert.That(snapshot.Roots, Has.Count.EqualTo(1));
        Assert.That(snapshot.Roots[0].Name, Is.EqualTo("RadzenDialog"));
        Assert.That(snapshot.Roots[0].Parameters.Any(parameter => parameter.Name == "Title" && parameter.Value == "Matter Search"), Is.True);
        Assert.That(snapshot.Roots[0].Children.Select(child => child.Name), Is.EqualTo(new[] { nameof(TestDialogComponent) }));
    }

    [Test]
    public void Radzen_renderfragment_dialogs_register_synthetic_root_and_attach_first_detached_component()
    {
        var services = new ServiceCollection();
        services.AddScoped<NavigationManager, TestNavigationManager>();
        services.AddScoped<IJSRuntime, TestJsRuntime>();
        services.AddScoped<ComponentTracker>();
        services.AddScoped<DevToolsSnapshotBridge>();
        services.AddScoped<DevToolsAutoRefreshScheduler>();
        services.AddScoped<IDevToolsExternalComponentTracker, DevToolsExternalComponentTracker>();
        services.AddRadzenComponents();

        using var provider = services.BuildServiceProvider();
        var tracker = provider.GetRequiredService<ComponentTracker>();
        var scheduler = provider.GetRequiredService<DevToolsAutoRefreshScheduler>();
        var externalTracker = provider.GetRequiredService<IDevToolsExternalComponentTracker>();
        var dialogService = provider.GetRequiredService<DialogService>();

        externalTracker.EnsureInitialized();
        _ = dialogService.OpenAsync("Inline dialog", _ => builder => builder.AddMarkupContent(0, "<div>Inline</div>"), new DialogOptions());

        var lifecycle = new DevToolsTrackedComponentLifecycle(tracker, scheduler, externalTracker);
        lifecycle.ApplySnapshot(typeof(TestInlineChildComponent), parentComponentId: null, [new("Count", "7")]);

        var snapshot = tracker.BuildSnapshot();

        Assert.That(snapshot.Roots, Has.Count.EqualTo(1));
        Assert.That(snapshot.Roots[0].Name, Is.EqualTo("RadzenDialog"));
        Assert.That(snapshot.Roots[0].Parameters.Any(parameter => parameter.Name == "ContentComponentType" && parameter.Value == "<render-fragment>"), Is.True);
        Assert.That(snapshot.Roots[0].Children.Select(child => child.Name), Is.EqualTo(new[] { nameof(TestInlineChildComponent) }));
    }

    private sealed class TestDialogComponent : ComponentBase;

    private sealed class TestInlineChildComponent : ComponentBase;

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://localhost/", "https://localhost/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
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

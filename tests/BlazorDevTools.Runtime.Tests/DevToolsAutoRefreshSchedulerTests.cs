using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorDevTools.Runtime.Tests;

public class DevToolsAutoRefreshSchedulerTests
{
    [Test]
    public async Task RequestRefresh_debounces_multiple_rapid_requests()
    {
        var tracker = new ComponentTracker();
        tracker.RegisterComponent("root", typeof(FakeComponent));

        var jsRuntime = new RecordingJsRuntime();
        var bridge = new DevToolsSnapshotBridge(tracker, jsRuntime);
        using var scheduler = new DevToolsAutoRefreshScheduler(bridge);

        scheduler.RequestRefresh();
        scheduler.RequestRefresh();
        scheduler.RequestRefresh();

        await Task.Delay(300);

        Assert.That(jsRuntime.InvocationCount, Is.EqualTo(1));
    }

    [Test]
    public async Task RequestRefresh_publishes_again_after_debounce_window()
    {
        var tracker = new ComponentTracker();
        tracker.RegisterComponent("root", typeof(FakeComponent));

        var jsRuntime = new RecordingJsRuntime();
        var bridge = new DevToolsSnapshotBridge(tracker, jsRuntime);
        using var scheduler = new DevToolsAutoRefreshScheduler(bridge);

        scheduler.RequestRefresh();
        await Task.Delay(300);

        scheduler.RequestRefresh();
        await Task.Delay(300);

        Assert.That(jsRuntime.InvocationCount, Is.EqualTo(2));
    }

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public int InvocationCount { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            InvocationCount++;
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            InvocationCount++;
            return ValueTask.FromResult(default(TValue)!);
        }
    }

    private sealed class FakeComponent : ComponentBase;
}

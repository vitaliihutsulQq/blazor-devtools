using System.Threading;
using BlazorDevTools.Runtime;

namespace BlazorDevTools.Runtime.Tests;

public class ComponentLifecycleMetricsCollectorTests
{
    [Test]
    public void BuildSnapshot_includes_render_and_lifecycle_metrics()
    {
        var collector = new ComponentLifecycleMetricsCollector();

        collector.MarkTrackingStarted();
        Thread.Sleep(5);
        collector.RecordOnInitialized(TimeSpan.FromMilliseconds(0.3));
        collector.RecordOnInitializedAsync(TimeSpan.FromMilliseconds(2.4));
        collector.RecordOnParametersSet(TimeSpan.FromMilliseconds(1.2));
        collector.RecordStateHasChanged();
        collector.RecordRender(TimeSpan.FromMilliseconds(3.5));
        collector.RecordOnAfterRender(TimeSpan.FromMilliseconds(0.4));
        collector.RecordStateHasChanged();
        collector.RecordRender(TimeSpan.FromMilliseconds(2.5));

        var snapshot = collector.BuildSnapshot();

        Assert.That(snapshot.TimeToFirstRenderMs, Is.Not.Null.And.GreaterThan(0));
        Assert.That(snapshot.RenderCount, Is.EqualTo(2));
        Assert.That(snapshot.AverageRenderTimeMs, Is.EqualTo(3.0).Within(0.0001));
        Assert.That(snapshot.StateHasChangedCount, Is.EqualTo(2));
        Assert.That(snapshot.OnInitializedTimeMs, Is.EqualTo(0.3).Within(0.0001));
        Assert.That(snapshot.OnInitializedAsyncTimeMs, Is.EqualTo(2.4).Within(0.0001));
        Assert.That(snapshot.OnParametersSetTimeMs, Is.EqualTo(1.2).Within(0.0001));
        Assert.That(snapshot.OnAfterRenderTimeMs, Is.EqualTo(0.4).Within(0.0001));
        Assert.That(snapshot.TotalRenderTimeMs, Is.EqualTo(6.0).Within(0.0001));
    }
}

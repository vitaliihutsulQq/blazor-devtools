using BlazorDevTools.Runtime;

namespace BlazorDevTools.Runtime.Tests;

public class ComponentRenderCauseCollectorTests
{
    [Test]
    public void RecordRender_tracks_first_render_and_parameter_changes()
    {
        var collector = new ComponentRenderCauseCollector();

        collector.ObserveParameters([new("Count", "1")], isRegistered: false);
        collector.RecordRender(isFirstRender: true);

        collector.ObserveParameters([new("Count", "2")], isRegistered: true);
        collector.RecordRender(isFirstRender: false);

        var snapshot = collector.BuildSnapshot();

        Assert.That(snapshot.LatestRenderCause?.Cause, Is.EqualTo("Parameters changed"));
        Assert.That(snapshot.RecentRenderCauses.Select(cause => cause.Cause), Is.EqualTo(new[] { "First render", "Parameters changed" }));
    }

    [Test]
    public void MarkStateHasChanged_sets_direct_render_cause()
    {
        var collector = new ComponentRenderCauseCollector();

        collector.ObserveParameters([new("Count", "1")], isRegistered: false);
        collector.RecordRender(isFirstRender: true);
        collector.MarkStateHasChanged();
        collector.RecordRender(isFirstRender: false);

        var snapshot = collector.BuildSnapshot();

        Assert.That(snapshot.LatestRenderCause?.Cause, Is.EqualTo("StateHasChanged invoked"));
        Assert.That(snapshot.LatestRenderCause?.IsApproximate, Is.False);
    }

    [Test]
    public void RecordRender_uses_parent_render_fallback_when_no_direct_cause_exists()
    {
        var collector = new ComponentRenderCauseCollector();

        collector.ObserveParameters([new("Count", "1")], isRegistered: false);
        collector.RecordRender(isFirstRender: true);
        collector.RecordRender(isFirstRender: false);

        var snapshot = collector.BuildSnapshot();

        Assert.That(snapshot.LatestRenderCause?.Cause, Is.EqualTo("Parent rendered / framework-triggered render"));
        Assert.That(snapshot.LatestRenderCause?.IsApproximate, Is.True);
    }
}

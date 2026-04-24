using BlazorDevTools.Protocol;
using BlazorDevTools.Runtime;

namespace BlazorDevTools.Runtime.Tests;

public class ComponentRenderDiffCollectorTests
{
    [Test]
    public void First_render_has_no_previous_snapshot()
    {
        var collector = CreateCollectorWithTimestamps();

        collector.ObserveParameters([new("Title", "Dashboard")]);
        collector.RecordRender();

        var snapshot = collector.BuildSnapshot();

        Assert.That(snapshot.LatestRenderDiff, Is.Not.Null);
        Assert.That(snapshot.LatestRenderDiff!.HasPreviousSnapshot, Is.False);
        Assert.That(snapshot.LatestRenderDiff.RecordedAt, Is.EqualTo(RenderTimestamps[0]));
        Assert.That(snapshot.LatestRenderDiff.ParameterChanges, Is.Empty);
    }

    [Test]
    public void Subsequent_render_includes_parameter_diffs()
    {
        var collector = CreateCollectorWithTimestamps();

        collector.ObserveParameters(
            [
                new("Title", "Dashboard"),
                new("Count", "1")
            ]);
        collector.RecordRender();

        collector.ObserveParameters(
            [
                new("Title", "Weather"),
                new("Count", "2"),
                new("Filter", "Active")
            ]);
        collector.RecordRender();

        var diff = collector.BuildSnapshot().LatestRenderDiff;

        Assert.That(diff, Is.Not.Null);
        Assert.That(diff!.HasPreviousSnapshot, Is.True);
        Assert.That(diff.RecordedAt, Is.EqualTo(RenderTimestamps[1]));
        Assert.That(diff.ParameterChanges.Select(change => change.Name), Is.EqualTo(new[] { "Title", "Count", "Filter" }));
        Assert.That(diff.ParameterChanges[0].PreviousValue, Is.EqualTo("Dashboard"));
        Assert.That(diff.ParameterChanges[0].CurrentValue, Is.EqualTo("Weather"));
        Assert.That(diff.ParameterChanges[2].PreviousValue, Is.EqualTo("<not supplied>"));
        Assert.That(diff.ParameterChanges[2].CurrentValue, Is.EqualTo("Active"));
    }

    [Test]
    public void Render_without_parameter_update_records_empty_diff()
    {
        var collector = CreateCollectorWithTimestamps();

        collector.ObserveParameters([new("Count", "1")]);
        collector.RecordRender();
        collector.RecordRender();

        var diff = collector.BuildSnapshot().LatestRenderDiff;

        Assert.That(diff, Is.Not.Null);
        Assert.That(diff!.HasPreviousSnapshot, Is.True);
        Assert.That(diff.RecordedAt, Is.EqualTo(RenderTimestamps[1]));
        Assert.That(diff.ParameterChanges, Is.Empty);
    }

    [Test]
    public void Collector_keeps_bounded_history()
    {
        var collector = CreateCollectorWithTimestamps();

        collector.ObserveParameters([new("Count", "0")]);
        collector.RecordRender();

        for (var index = 1; index <= 6; index++)
        {
            collector.ObserveParameters([new("Count", index.ToString())]);
            collector.RecordRender();
        }

        var snapshot = collector.BuildSnapshot();

        Assert.That(snapshot.RecentRenderDiffs, Has.Count.EqualTo(5));
        Assert.That(snapshot.RecentRenderDiffs.Select(diff => diff.RenderSequence), Is.EqualTo(new[] { 3, 4, 5, 6, 7 }));
        Assert.That(snapshot.RecentRenderDiffs.Select(diff => diff.RecordedAt), Is.EqualTo(RenderTimestamps.Skip(2).Take(5).ToArray()));
    }

    private static readonly DateTimeOffset[] RenderTimestamps =
    [
        new(2026, 04, 25, 14, 03, 10, 125, TimeSpan.Zero),
        new(2026, 04, 25, 14, 03, 11, 973, TimeSpan.Zero),
        new(2026, 04, 25, 14, 03, 12, 184, TimeSpan.Zero),
        new(2026, 04, 25, 14, 03, 13, 204, TimeSpan.Zero),
        new(2026, 04, 25, 14, 03, 14, 224, TimeSpan.Zero),
        new(2026, 04, 25, 14, 03, 15, 244, TimeSpan.Zero),
        new(2026, 04, 25, 14, 03, 16, 264, TimeSpan.Zero)
    ];

    private static ComponentRenderDiffCollector CreateCollectorWithTimestamps()
    {
        var index = 0;
        return new ComponentRenderDiffCollector(() => RenderTimestamps[index++]);
    }
}

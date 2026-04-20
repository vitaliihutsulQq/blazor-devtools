using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.Runtime.Tests;

public class DevtoolsComponentBaseTests
{
    [Test]
    public async Task SetParametersAsync_helpers_capture_parameters_before_async_boundary()
    {
        var parameterView = ParameterView.FromDictionary(
            new Dictionary<string, object?>
            {
                [nameof(TestComponent.Title)] = "Weather",
                [nameof(TestComponent.Count)] = 5
            });

        var snapshots = DevToolsParameterSnapshotFactory.Create(parameterView);

        await Task.Yield();

        Assert.That(snapshots.Select(parameter => parameter.Name), Is.EqualTo(new[] { nameof(TestComponent.Title), nameof(TestComponent.Count) }));
        Assert.That(snapshots.Select(parameter => parameter.Value), Is.EqualTo(new[] { "Weather", "5" }));
    }

    private sealed class TestComponent
    {
        public string Title { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}

using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.Runtime.Tests;

public class ComponentParameterFormatterTests
{
    [Test]
    public void Format_returns_scalar_values_without_throwing()
    {
        Assert.That(ComponentParameterFormatter.Format("hello"), Is.EqualTo("hello"));
        Assert.That(ComponentParameterFormatter.Format(42), Is.EqualTo("42"));
        Assert.That(ComponentParameterFormatter.Format(true), Is.EqualTo("true"));
    }

    [Test]
    public void Format_returns_safe_summary_for_complex_object()
    {
        var formatted = ComponentParameterFormatter.Format(new SampleModel());

        Assert.That(formatted, Is.EqualTo($"<{typeof(SampleModel).FullName}>"));
    }

    [Test]
    public void Format_returns_collection_summary_for_enumerables()
    {
        var formatted = ComponentParameterFormatter.Format(new[] { 1, 2, 3 });

        Assert.That(formatted, Does.Contain("Count = 3"));
    }

    [Test]
    public void Format_returns_marker_for_render_fragments()
    {
        RenderFragment fragment = builder => builder.AddContent(0, "hello");

        Assert.That(ComponentParameterFormatter.Format(fragment), Is.EqualTo("<render-fragment>"));
    }

    private sealed class SampleModel;
}

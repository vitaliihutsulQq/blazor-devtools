using System.Reflection;
using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace BlazorDevTools.Runtime.Tests;

public class DevToolsComponentRenderWrapperTests
{
    [Test]
    public void DevtoolsComponentBase_wraps_render_output_with_automatic_dom_anchor()
    {
        var component = new InspectableSimpleComponent();
        var renderFragment = (RenderFragment?)typeof(ComponentBase)
            .GetField("_renderFragment", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(component);

        Assert.That(renderFragment, Is.Not.Null);

        var builder = new RenderTreeBuilder();
        renderFragment!(builder);
        var frames = builder.GetFrames().Array;

        Assert.That(frames.Any(frame => frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "blazor-devtools-anchor"), Is.True);
    }

    private sealed class InspectableSimpleComponent : DevtoolsComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "section");
            builder.AddContent(1, "Tracked content");
            builder.CloseElement();
        }
    }
}

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorDevTools.Runtime;

internal static class DevToolsComponentRenderWrapper
{
    private static readonly FieldInfo? RenderFragmentField = typeof(ComponentBase)
        .GetField("_renderFragment", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? typeof(ComponentBase)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(field => field.FieldType == typeof(RenderFragment));

    public static void TryWrap(ComponentBase component, Action<RenderTreeBuilder, RenderFragment> wrapper)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(wrapper);

        if (RenderFragmentField is null)
        {
            return;
        }

        if (RenderFragmentField.GetValue(component) is not RenderFragment original)
        {
            return;
        }

        RenderFragmentField.SetValue(component, (RenderFragment)(builder => wrapper(builder, original)));
    }
}

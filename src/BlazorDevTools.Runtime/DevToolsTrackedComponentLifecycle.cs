using BlazorDevTools.Protocol;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorDevTools.Runtime;

public sealed class DevToolsTrackedComponentLifecycle
{
    private readonly ComponentTracker componentTracker;
    private readonly DevToolsAutoRefreshScheduler autoRefreshScheduler;
    private bool isRegistered;

    public DevToolsTrackedComponentLifecycle(ComponentTracker componentTracker, DevToolsAutoRefreshScheduler autoRefreshScheduler)
    {
        this.componentTracker = componentTracker;
        this.autoRefreshScheduler = autoRefreshScheduler;
    }

    public string ComponentId { get; } = $"component-{Guid.NewGuid():N}";

    public void ApplySnapshot(Type componentType, string? parentComponentId, IReadOnlyList<ComponentParameterSnapshot> parameters, string? domMarkerId = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(parameters);

        componentTracker.RegisterComponent(ComponentId, componentType, parentComponentId);

        if (domMarkerId is not null)
        {
            componentTracker.SetDomMarker(ComponentId, domMarkerId);
        }

        componentTracker.UpdateParameters(ComponentId, parameters);
        autoRefreshScheduler.RequestRefresh();
        isRegistered = true;
    }

    public void OnAfterRender()
    {
        if (!isRegistered)
        {
            return;
        }

        componentTracker.IncrementRenderCount(ComponentId);
        autoRefreshScheduler.RequestRefresh();
    }

    public void Dispose()
    {
        if (!isRegistered)
        {
            return;
        }

        componentTracker.UnregisterComponent(ComponentId);
        autoRefreshScheduler.RequestRefresh();
        isRegistered = false;
    }

    public void RenderWithParentScope(RenderTreeBuilder builder, RenderFragment childContent)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(childContent);

        builder.OpenComponent<CascadingValue<string>>(0);
        builder.AddAttribute(1, "Name", DevtoolsComponentBase.ParentComponentIdCascadeName);
        builder.AddAttribute(2, "Value", ComponentId);
        builder.AddAttribute(3, "IsFixed", true);
        builder.AddAttribute(4, "ChildContent", childContent);
        builder.CloseComponent();
    }
}

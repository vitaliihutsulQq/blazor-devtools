using BlazorDevTools.Protocol;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorDevTools.Runtime;

public sealed class DevToolsTrackedComponentLifecycle
{
    private readonly ComponentTracker componentTracker;
    private readonly DevToolsAutoRefreshScheduler autoRefreshScheduler;
    private readonly IDevToolsExternalComponentTracker? externalComponentTracker;
    private bool isRegistered;
    private string? resolvedParentComponentId;

    public DevToolsTrackedComponentLifecycle(
        ComponentTracker componentTracker,
        DevToolsAutoRefreshScheduler autoRefreshScheduler,
        IDevToolsExternalComponentTracker? externalComponentTracker = null)
    {
        this.componentTracker = componentTracker;
        this.autoRefreshScheduler = autoRefreshScheduler;
        this.externalComponentTracker = externalComponentTracker;
    }

    public string ComponentId { get; } = $"component-{Guid.NewGuid():N}";

    public void ApplySnapshot(
        Type componentType,
        string? parentComponentId,
        IReadOnlyList<ComponentParameterSnapshot> parameters,
        IReadOnlyList<ComponentInjectedServiceSnapshot>? injectedServices = null,
        string? domMarkerId = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(parameters);

        externalComponentTracker?.EnsureInitialized();
        resolvedParentComponentId = parentComponentId ?? resolvedParentComponentId ?? externalComponentTracker?.ResolveParentComponentId(componentType);

        componentTracker.RegisterComponent(ComponentId, componentType, resolvedParentComponentId);

        if (domMarkerId is not null)
        {
            componentTracker.SetDomMarker(ComponentId, domMarkerId);
        }

        componentTracker.UpdateParameters(ComponentId, parameters);

        if (injectedServices is not null)
        {
            componentTracker.UpdateInjectedServices(ComponentId, injectedServices);
        }

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

    public void RenderWithParentScopeAndDomMarker(RenderTreeBuilder builder, RenderFragment childContent)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(childContent);

        builder.OpenElement(0, "blazor-devtools-anchor");
        builder.AddAttribute(1, DevToolsDomMarker.AttributeName, ComponentId);
        builder.AddAttribute(2, "style", "display: contents;");
        builder.OpenComponent<CascadingValue<string>>(3);
        builder.AddAttribute(4, "Name", DevtoolsComponentBase.ParentComponentIdCascadeName);
        builder.AddAttribute(5, "Value", ComponentId);
        builder.AddAttribute(6, "IsFixed", true);
        builder.AddAttribute(7, "ChildContent", childContent);
        builder.CloseComponent();
        builder.CloseElement();
    }
}

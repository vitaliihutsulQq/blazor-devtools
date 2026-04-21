using System.Diagnostics;
using BlazorDevTools.Protocol;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorDevTools.Runtime;

public sealed class DevToolsTrackedComponentLifecycle
{
    private readonly ComponentTracker componentTracker;
    private readonly DevToolsAutoRefreshScheduler autoRefreshScheduler;
    private readonly IDevToolsExternalComponentTracker? externalComponentTracker;
    private readonly ComponentLifecycleMetricsCollector lifecycleMetricsCollector = new();
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
        lifecycleMetricsCollector.MarkTrackingStarted();

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

        PublishMetricsIfRegistered();
        autoRefreshScheduler.RequestRefresh();
        isRegistered = true;
        PublishMetricsIfRegistered();
    }

    public void RecordOnInitialized(TimeSpan duration)
    {
        lifecycleMetricsCollector.RecordOnInitialized(duration);
        PublishMetricsIfRegistered();
    }

    public void RecordOnInitializedAsync(TimeSpan duration)
    {
        lifecycleMetricsCollector.RecordOnInitializedAsync(duration);
        PublishMetricsIfRegistered();
    }

    public void RecordOnParametersSet(TimeSpan duration)
    {
        lifecycleMetricsCollector.RecordOnParametersSet(duration);
        PublishMetricsIfRegistered();
    }

    public void OnAfterRender()
    {
        if (!isRegistered)
        {
            return;
        }

        PublishMetricsIfRegistered();
        autoRefreshScheduler.RequestRefresh();
    }

    public void RecordOnAfterRender(TimeSpan duration)
    {
        lifecycleMetricsCollector.RecordOnAfterRender(duration);
        PublishMetricsIfRegistered();
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

        RenderMeasured(builder, renderBuilder =>
        {
            renderBuilder.OpenComponent<CascadingValue<string>>(0);
            renderBuilder.AddAttribute(1, "Name", DevtoolsComponentBase.ParentComponentIdCascadeName);
            renderBuilder.AddAttribute(2, "Value", ComponentId);
            renderBuilder.AddAttribute(3, "IsFixed", true);
            renderBuilder.AddAttribute(4, "ChildContent", childContent);
            renderBuilder.CloseComponent();
        });
    }

    public void RenderWithParentScopeAndDomMarker(RenderTreeBuilder builder, RenderFragment childContent)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(childContent);

        RenderMeasured(builder, renderBuilder =>
        {
            renderBuilder.OpenElement(0, "blazor-devtools-anchor");
            renderBuilder.AddAttribute(1, DevToolsDomMarker.AttributeName, ComponentId);
            renderBuilder.AddAttribute(2, "style", "display: contents;");
            renderBuilder.OpenComponent<CascadingValue<string>>(3);
            renderBuilder.AddAttribute(4, "Name", DevtoolsComponentBase.ParentComponentIdCascadeName);
            renderBuilder.AddAttribute(5, "Value", ComponentId);
            renderBuilder.AddAttribute(6, "IsFixed", true);
            renderBuilder.AddAttribute(7, "ChildContent", childContent);
            renderBuilder.CloseComponent();
            renderBuilder.CloseElement();
        });
    }

    private void RenderMeasured(RenderTreeBuilder builder, Action<RenderTreeBuilder> renderContent)
    {
        var startedAt = Stopwatch.GetTimestamp();
        renderContent(builder);
        lifecycleMetricsCollector.RecordRender(Stopwatch.GetElapsedTime(startedAt));
        PublishMetricsIfRegistered();
    }

    private void PublishMetricsIfRegistered()
    {
        if (!isRegistered)
        {
            return;
        }

        componentTracker.UpdateLifecycleMetrics(ComponentId, lifecycleMetricsCollector.BuildSnapshot());
    }
}

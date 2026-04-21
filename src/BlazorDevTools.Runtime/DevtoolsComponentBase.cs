using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorDevTools.Runtime;

public abstract class DevtoolsComponentBase : ComponentBase, IDisposable
{
    protected DevtoolsComponentBase()
    {
        DevToolsComponentRenderWrapper.TryWrap(this, (builder, childContent) => TrackingLifecycle.RenderWithParentScopeAndDomMarker(builder, childContent));
    }

    public const string ParentComponentIdCascadeName = "BlazorDevTools.ParentComponentId";

    [Inject]
    private ComponentTracker ComponentTracker { get; set; } = default!;

    [Inject]
    private DevToolsAutoRefreshScheduler AutoRefreshScheduler { get; set; } = default!;

    [Inject]
    private IDevToolsExternalComponentTracker? ExternalComponentTracker { get; set; }

    [CascadingParameter(Name = ParentComponentIdCascadeName)]
    public string? ParentComponentId { get; set; }

    protected string DomMarkerId => ComponentId;

    protected string DomMarkerAttributeName => DevToolsDomMarker.AttributeName;

    protected IReadOnlyDictionary<string, object> DevToolsMarkerAttributes => devToolsMarkerAttributes ??=
        new Dictionary<string, object>
        {
            [DomMarkerAttributeName] = DomMarkerId
        };

    protected string ParentComponentCascadeName => ParentComponentIdCascadeName;

    private IReadOnlyDictionary<string, object>? devToolsMarkerAttributes;
    private DevToolsTrackedComponentLifecycle? trackingLifecycle;

    protected string ComponentId => TrackingLifecycle.ComponentId;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var capturedParameters = DevToolsParameterSnapshotFactory.Create(parameters, nameof(ParentComponentId));
        var injectedServices = ComponentInjectedServiceSnapshotFactory.Create(GetType());

        await base.SetParametersAsync(parameters);

        TrackingLifecycle.ApplySnapshot(GetType(), ParentComponentId, capturedParameters, injectedServices, DomMarkerId);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        trackingLifecycle?.OnAfterRender();
    }

    public void Dispose()
    {
        trackingLifecycle?.Dispose();
    }

    protected void RenderTrackedChildContent(RenderTreeBuilder builder, RenderFragment childContent)
    {
        TrackingLifecycle.RenderWithParentScope(builder, childContent);
    }

    private DevToolsTrackedComponentLifecycle TrackingLifecycle => trackingLifecycle ??= new DevToolsTrackedComponentLifecycle(ComponentTracker, AutoRefreshScheduler, ExternalComponentTracker);
}

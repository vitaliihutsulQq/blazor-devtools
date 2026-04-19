using Microsoft.AspNetCore.Components;
using BlazorDevTools.Protocol;

namespace BlazorDevTools.Runtime;

public abstract class DevtoolsComponentBase : ComponentBase, IDisposable
{
    public const string ParentComponentIdCascadeName = "BlazorDevTools.ParentComponentId";

    [Inject]
    private ComponentTracker ComponentTracker { get; set; } = default!;

    [Inject]
    private DevToolsAutoRefreshScheduler AutoRefreshScheduler { get; set; } = default!;

    [CascadingParameter(Name = ParentComponentIdCascadeName)]
    public string? ParentComponentId { get; set; }

    protected string ComponentId { get; } = $"component-{Guid.NewGuid():N}";

    protected string DomMarkerId => ComponentId;

    protected string DomMarkerAttributeName => DevToolsDomMarker.AttributeName;

    protected IReadOnlyDictionary<string, object> DevToolsMarkerAttributes => devToolsMarkerAttributes ??=
        new Dictionary<string, object>
        {
            [DomMarkerAttributeName] = DomMarkerId
        };

    protected string ParentComponentCascadeName => ParentComponentIdCascadeName;

    private bool isRegistered;
    private IReadOnlyDictionary<string, object>? devToolsMarkerAttributes;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var capturedParameters = CaptureParameterValues(parameters);

        await base.SetParametersAsync(parameters);

        if (!isRegistered)
        {
            ComponentTracker.RegisterComponent(ComponentId, GetType(), ParentComponentId);
            ComponentTracker.SetDomMarker(ComponentId, DomMarkerId);
            isRegistered = true;
        }

        ComponentTracker.UpdateParameters(ComponentId, FormatCapturedParameters(capturedParameters));
        AutoRefreshScheduler.RequestRefresh();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (isRegistered)
        {
            ComponentTracker.IncrementRenderCount(ComponentId);
            AutoRefreshScheduler.RequestRefresh();
        }
    }

    public void Dispose()
    {
        if (isRegistered)
        {
            ComponentTracker.UnregisterComponent(ComponentId);
            AutoRefreshScheduler.RequestRefresh();
        }
    }

    private static IReadOnlyList<CapturedParameterValue> CaptureParameterValues(ParameterView parameters)
    {
        var capturedParameters = new List<CapturedParameterValue>();

        foreach (var parameter in parameters)
        {
            if (parameter.Name == nameof(ParentComponentId))
            {
                continue;
            }

            capturedParameters.Add(new CapturedParameterValue(parameter.Name, parameter.Value));
        }

        return capturedParameters;
    }

    private static IReadOnlyList<ComponentParameterSnapshot> FormatCapturedParameters(IReadOnlyList<CapturedParameterValue> capturedParameters)
    {
        return capturedParameters
            .Select(parameter => new ComponentParameterSnapshot(parameter.Name, ComponentParameterFormatter.Format(parameter.Value)))
            .ToArray();
    }

    private sealed record CapturedParameterValue(string Name, object? Value);
}

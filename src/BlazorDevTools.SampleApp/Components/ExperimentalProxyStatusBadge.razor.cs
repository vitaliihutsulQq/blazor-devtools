using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.SampleApp.Components;

public partial class ExperimentalProxyStatusBadge : ComponentBase
{
    [Parameter]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public bool IsActive { get; set; }
}

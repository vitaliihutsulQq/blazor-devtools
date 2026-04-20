using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.SampleApp.Components;

public partial class ExperimentalProxyPanel : ComponentBase
{
    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public int Count { get; set; }

    [Parameter]
    public bool ShowDetails { get; set; }
}

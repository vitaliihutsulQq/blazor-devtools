using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.SampleApp.Components;

public partial class ExperimentalProxySummaryCard : ComponentBase
{
    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public int Count { get; set; }

    [Parameter]
    public string Status { get; set; } = string.Empty;

    [Parameter]
    public bool IsActive { get; set; }
}

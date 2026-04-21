using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.SampleApp.Components;

public partial class RadzenInlineDialogPanel : ComponentBase
{
    [Parameter]
    public int Count { get; set; }

    [Parameter]
    public string Emphasis { get; set; } = string.Empty;
}

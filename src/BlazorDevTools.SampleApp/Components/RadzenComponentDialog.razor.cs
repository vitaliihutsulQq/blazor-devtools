using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorDevTools.SampleApp.Components;

public partial class RadzenComponentDialog : ComponentBase
{
    [Inject]
    public DialogService DialogService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public int OrderCount { get; set; }

    [Parameter]
    public bool ShowDetails { get; set; }
}

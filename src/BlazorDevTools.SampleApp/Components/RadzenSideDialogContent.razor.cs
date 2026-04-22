using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorDevTools.SampleApp.Components;

public partial class RadzenSideDialogContent : ComponentBase
{
    [Inject]
    public DialogService DialogService { get; set; } = default!;

    [Parameter]
    public int OrderCount { get; set; }

    [Parameter]
    public bool ShowDetails { get; set; }
}

using BlazorDevTools.CompatibilityFixture.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.CompatibilityFixture.Components;

public partial class ActivityTimeline : ComponentBase
{
    [Parameter]
    public IEnumerable<CaseActivity> Activities { get; set; } = [];

    private IReadOnlyList<CaseActivity> activities = [];

    protected override async Task OnParametersSetAsync()
    {
        await Task.Delay(5);
        activities = Activities.ToArray();
    }
}

using BlazorDevTools.CompatibilityFixture.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.CompatibilityFixture.Components;

public partial class ActivityTimelineItem : ComponentBase
{
    [Parameter]
    public CaseActivity Activity { get; set; } = new(string.Empty, string.Empty, false);
}

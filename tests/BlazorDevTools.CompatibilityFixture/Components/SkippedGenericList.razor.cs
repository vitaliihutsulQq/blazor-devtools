using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.CompatibilityFixture.Components;

public partial class SkippedGenericList<TItem> : ComponentBase
{
    [Parameter]
    public IEnumerable<TItem> Items { get; set; } = [];
}

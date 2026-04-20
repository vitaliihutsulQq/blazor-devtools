using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.CompatibilityFixture.Components;

public sealed partial class SkippedSealedPanel : ComponentBase
{
    [Parameter]
    public string Label { get; set; } = string.Empty;
}

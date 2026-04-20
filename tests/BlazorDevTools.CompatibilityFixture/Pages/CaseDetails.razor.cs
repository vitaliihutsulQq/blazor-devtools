using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.CompatibilityFixture.Pages;

public partial class CaseDetails : ComponentBase
{
    [Parameter]
    public int CaseId { get; set; }

    protected readonly string[] Categories = ["Summary", "Evidence", "Timeline"];
    protected bool HighlightCriticalOnly { get; set; } = true;
}

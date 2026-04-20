using BlazorDevTools.CompatibilityFixture.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.CompatibilityFixture.Components;

public partial class DocumentList : ComponentBase
{
    [Parameter]
    public IReadOnlyList<CaseDocument> Documents { get; set; } = [];

    [Parameter]
    public bool IsPinned { get; set; }
}

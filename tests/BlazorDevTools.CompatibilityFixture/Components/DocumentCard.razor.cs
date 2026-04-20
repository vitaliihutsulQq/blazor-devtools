using BlazorDevTools.CompatibilityFixture.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.CompatibilityFixture.Components;

public partial class DocumentCard : ComponentBase
{
    [Parameter]
    public CaseDocument Document { get; set; } = new(string.Empty, string.Empty, 0);

    [Parameter]
    public bool IsPinned { get; set; }
}

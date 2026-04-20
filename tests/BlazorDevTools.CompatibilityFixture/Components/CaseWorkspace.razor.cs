using BlazorDevTools.CompatibilityFixture.Models;
using BlazorDevTools.CompatibilityFixture.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.CompatibilityFixture.Components;

public partial class CaseWorkspace : ComponentBase
{
    [Inject]
    public CaseWorkspaceService WorkspaceService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public int CaseId { get; set; }

    [Parameter]
    public bool HighlightCriticalOnly { get; set; }

    private CaseWorkspaceModel? workspace;

    protected IEnumerable<CaseActivity> FilteredActivities => workspace?.Activities.Where(activity => !HighlightCriticalOnly || activity.IsCritical)
        ?? [];

    protected override async Task OnParametersSetAsync()
    {
        workspace = await WorkspaceService.GetWorkspaceAsync(CaseId);
    }
}

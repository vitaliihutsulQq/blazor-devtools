using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.CompatibilityFixture.Pages;

public partial class ImplicitBaseTracking
{
    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    protected string StatusMessage { get; set; } = "Waiting for initialization.";

    protected override Task OnInitializedAsync()
    {
        StatusMessage = $"Initialized for {Navigation.BaseUri}";
        StateHasChanged();
        return Task.CompletedTask;
    }
}

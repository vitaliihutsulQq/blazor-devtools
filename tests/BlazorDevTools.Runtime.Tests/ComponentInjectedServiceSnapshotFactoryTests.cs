using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorDevTools.Runtime.Tests;

public class ComponentInjectedServiceSnapshotFactoryTests
{
    [Test]
    public void Create_discovers_injected_services_on_component_and_custom_base_types()
    {
        var snapshots = ComponentInjectedServiceSnapshotFactory.Create(typeof(DerivedComponent));

        Assert.That(snapshots.Select(snapshot => snapshot.PropertyName), Is.EqualTo(new[]
        {
            nameof(DerivedComponent.JsRuntime),
            "NavigationManager"
        }));

        Assert.That(snapshots.Select(snapshot => snapshot.ServiceTypeName), Does.Contain(nameof(IJSRuntime)).And.Contain(nameof(NavigationManager)));
    }

    [Test]
    public void Create_skips_runtime_infrastructure_in_devtools_component_base()
    {
        var snapshots = ComponentInjectedServiceSnapshotFactory.Create(typeof(RuntimeTrackedComponent));

        Assert.That(snapshots.Select(snapshot => snapshot.PropertyName), Is.EqualTo(new[] { nameof(RuntimeTrackedComponent.JsRuntime) }));
    }

    private abstract class CustomTrackedBase : DevtoolsComponentBase
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;
    }

    private sealed class DerivedComponent : CustomTrackedBase
    {
        [Inject]
        public IJSRuntime JsRuntime { get; set; } = default!;
    }

    private sealed class RuntimeTrackedComponent : DevtoolsComponentBase
    {
        [Inject]
        public IJSRuntime JsRuntime { get; set; } = default!;
    }
}

using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorDevTools.Runtime.Tests;

public class ComponentCascadingParameterSnapshotFactoryTests
{
    [Test]
    public void Create_discovers_cascading_parameters_on_component_and_custom_base_types()
    {
        var snapshots = ComponentCascadingParameterSnapshotFactory.Create(typeof(DerivedCascadingComponent));

        Assert.That(snapshots.Select(snapshot => snapshot.PropertyName), Is.EqualTo(new[]
        {
            nameof(DerivedCascadingComponent.EditContext),
            "ThemeName"
        }));

        Assert.That(snapshots.Select(snapshot => snapshot.ProviderHint), Is.EqualTo(new string?[] { null, "ThemeName" }));
        Assert.That(snapshots.Select(snapshot => snapshot.ValueTypeName), Does.Contain(nameof(EditContext)).And.Contain(nameof(String)));
    }

    [Test]
    public void Create_skips_runtime_parent_component_cascade_from_devtools_base()
    {
        var snapshots = ComponentCascadingParameterSnapshotFactory.Create(typeof(RuntimeTrackedCascadingComponent));

        Assert.That(snapshots.Select(snapshot => snapshot.PropertyName), Is.EqualTo(new[] { nameof(RuntimeTrackedCascadingComponent.EditContext) }));
    }

    private abstract class CustomCascadingBase : DevtoolsComponentBase
    {
        [CascadingParameter(Name = "ThemeName")]
        protected string? ThemeName { get; set; }
    }

    private sealed class DerivedCascadingComponent : CustomCascadingBase
    {
        [CascadingParameter]
        public EditContext? EditContext { get; set; }
    }

    private sealed class RuntimeTrackedCascadingComponent : DevtoolsComponentBase
    {
        [CascadingParameter]
        public EditContext? EditContext { get; set; }
    }
}

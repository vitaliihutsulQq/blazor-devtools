using System.Reflection;
using BlazorDevTools.CompatibilityFixture;
using BlazorDevTools.CompatibilityFixture.Components;
using BlazorDevTools.CompatibilityFixture.Pages;
using BlazorDevTools.Runtime;

namespace BlazorDevTools.CompatibilityFixture.Tests;

public class CompatibilityFixtureGeneratorTests
{
    private static readonly Assembly FixtureAssembly = typeof(CaseWorkspace).Assembly;

    [Test]
    public void Eligible_partial_componentbase_shapes_get_generated_proxies()
    {
        AssertGeneratedTypeExists(typeof(CaseDetails));
        AssertGeneratedTypeExists(typeof(CaseWorkspace));
        AssertGeneratedTypeExists(typeof(DocumentList));
        AssertGeneratedTypeExists(typeof(DocumentCard));
        AssertGeneratedTypeExists(typeof(ActivityTimeline));
        AssertGeneratedTypeExists(typeof(ActivityTimelineItem));
    }

    [Test]
    public void Intentionally_skipped_shapes_do_not_get_generated_proxies()
    {
        AssertGeneratedTypeMissing("BlazorDevTools.CompatibilityFixture.Components.SkippedSealedPanel__BlazorDevToolsProxy");
        AssertGeneratedTypeMissing("BlazorDevTools.CompatibilityFixture.Components.SkippedGenericList__BlazorDevToolsProxy");
        AssertGeneratedTypeMissing("BlazorDevTools.CompatibilityFixture.Components.AlreadyTrackedWidget__BlazorDevToolsProxy");
    }

    [Test]
    public void Generated_manifest_contains_only_eligible_fixture_components()
    {
        var manifestType = FixtureAssembly.GetType("BlazorDevTools.Generated.BlazorDevToolsGeneratedComponentProxyManifest_BlazorDevTools_CompatibilityFixture");
        Assert.That(manifestType, Is.Not.Null);

        var manifest = (IDevToolsComponentProxyManifest)Activator.CreateInstance(manifestType!)!;
        var registrationNames = manifest.GetRegistrations().Select(registration => registration.ComponentType.Name).OrderBy(name => name).ToArray();

        Assert.That(registrationNames, Is.EqualTo(new[]
        {
            nameof(ActivityTimeline),
            nameof(ActivityTimelineItem),
            nameof(CaseDetails),
            nameof(CaseWorkspace),
            nameof(DocumentCard),
            nameof(DocumentList)
        }));
    }

    [Test]
    public void Already_tracked_simple_mode_component_still_uses_inheritance_path()
    {
        Assert.That(typeof(AlreadyTrackedWidget).IsAssignableTo(typeof(DevtoolsComponentBase)), Is.True);
    }

    private static void AssertGeneratedTypeExists(Type originalType)
    {
        var generatedTypeName = $"{originalType.FullName}__BlazorDevToolsProxy";
        Assert.That(FixtureAssembly.GetType(generatedTypeName), Is.Not.Null, generatedTypeName);
    }

    private static void AssertGeneratedTypeMissing(string generatedTypeName)
    {
        Assert.That(FixtureAssembly.GetType(generatedTypeName), Is.Null, generatedTypeName);
    }
}

using BlazorDevTools.CompatibilityFixture.Components;
using BlazorDevTools.Runtime;

namespace BlazorDevTools.CompatibilityFixture.Tests;

public class GeneratorModeInspectorTests
{
    [Test]
    public void Generated_proxy_exposes_original_component_identity_for_inspector_metadata()
    {
        var proxy = new CaseWorkspace__BlazorDevToolsProxy();
        var proxyContract = (IDevToolsComponentProxy)proxy;
        var injectedServices = ComponentInjectedServiceSnapshotFactory.Create(proxyContract.DevToolsOriginalComponentType);

        Assert.That(proxyContract.DevToolsOriginalComponentType, Is.EqualTo(typeof(CaseWorkspace)));
        Assert.That(proxyContract.DevToolsOriginalComponentType.FullName, Does.Not.Contain("__BlazorDevToolsProxy"));
        Assert.That(injectedServices.Select(service => service.PropertyName), Is.EqualTo(new[] { nameof(CaseWorkspace.WorkspaceService), nameof(CaseWorkspace.NavigationManager) }));
    }
}

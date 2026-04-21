using BlazorDevTools.Protocol;
using BlazorDevTools.Runtime;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.Runtime.Tests;

public class ComponentTrackerTests
{
    [Test]
    public void RegisterComponent_adds_root_component_to_snapshot()
    {
        var tracker = new ComponentTracker();

        tracker.RegisterComponent("root", "RootComponent");

        var snapshot = tracker.BuildSnapshot();

        Assert.That(snapshot.Roots, Has.Count.EqualTo(1));
        Assert.That(snapshot.Roots[0].Id, Is.EqualTo("root"));
        Assert.That(snapshot.Roots[0].Name, Is.EqualTo("RootComponent"));
        Assert.That(snapshot.Roots[0].FullTypeName, Is.EqualTo("RootComponent"));
        Assert.That(snapshot.Roots[0].DomMarkerId, Is.Null);
        Assert.That(snapshot.Roots[0].Parameters, Is.Empty);
    }

    [Test]
    public void RegisterComponent_tracks_parent_child_relationships()
    {
        var tracker = new ComponentTracker();

        tracker.RegisterComponent("parent", "ParentComponent");
        tracker.RegisterComponent("child", "ChildComponent", "parent");

        var snapshot = tracker.BuildSnapshot();

        Assert.That(snapshot.Roots, Has.Count.EqualTo(1));
        Assert.That(snapshot.Roots[0].Children, Has.Count.EqualTo(1));
        Assert.That(snapshot.Roots[0].Children[0].Id, Is.EqualTo("child"));
    }

    [Test]
    public void UnregisterComponent_removes_component_from_parent_children()
    {
        var tracker = new ComponentTracker();

        tracker.RegisterComponent("parent", "ParentComponent");
        tracker.RegisterComponent("child", "ChildComponent", "parent");

        tracker.UnregisterComponent("child");

        var snapshot = tracker.BuildSnapshot();

        Assert.That(snapshot.Roots, Has.Count.EqualTo(1));
        Assert.That(snapshot.Roots[0].Children, Is.Empty);
    }

    [Test]
    public void BuildSnapshot_promotes_children_when_parent_is_removed()
    {
        var tracker = new ComponentTracker();

        tracker.RegisterComponent("parent", "ParentComponent");
        tracker.RegisterComponent("child", "ChildComponent", "parent");
        tracker.RegisterComponent("grandchild", "GrandChildComponent", "child");

        tracker.UnregisterComponent("parent");

        var snapshot = tracker.BuildSnapshot();

        Assert.That(snapshot.Roots.Select(root => root.Id), Is.EqualTo(new[] { "child" }));
        Assert.That(snapshot.Roots[0].Children.Select(child => child.Id), Is.EqualTo(new[] { "grandchild" }));
    }

    [Test]
    public void BuildSnapshot_includes_component_metadata_parameters_and_render_count()
    {
        var tracker = new ComponentTracker();

        tracker.RegisterComponent("root", typeof(FakeComponent));
        tracker.UpdateParameters(
            "root",
            [
                new("Title", "Dashboard"),
                new("ItemCount", "12")
            ]);
        tracker.UpdateInjectedServices(
            "root",
            [
                new("WorkspaceService", "CaseWorkspaceService", "TestApp.Services.CaseWorkspaceService")
            ]);
        tracker.UpdateLifecycleMetrics(
            "root",
            new ComponentLifecycleMetricsSnapshot(
                TimeToFirstRenderMs: 12.5,
                RenderCount: 2,
                AverageRenderTimeMs: 2.75,
                StateHasChangedCount: 2,
                OnInitializedTimeMs: 0.4,
                OnInitializedAsyncTimeMs: 4.2,
                OnParametersSetTimeMs: 1.1,
                OnAfterRenderTimeMs: 0.7,
                TotalRenderTimeMs: 5.5));
        tracker.SetDomMarker("root", "component-root");

        var snapshot = tracker.BuildSnapshot();
        var root = snapshot.Roots[0];

        Assert.That(root.FullTypeName, Is.EqualTo(typeof(FakeComponent).FullName));
        Assert.That(root.AssemblyName, Is.EqualTo(typeof(FakeComponent).Assembly.GetName().Name));
        Assert.That(root.DomMarkerId, Is.EqualTo("component-root"));
        Assert.That(root.InjectedServices.Select(service => service.PropertyName), Is.EqualTo(new[] { "WorkspaceService" }));
        Assert.That(root.LifecycleMetrics?.TimeToFirstRenderMs, Is.EqualTo(12.5));
        Assert.That(root.LifecycleMetrics?.AverageRenderTimeMs, Is.EqualTo(2.75));
        Assert.That(root.LifecycleMetrics?.StateHasChangedCount, Is.EqualTo(2));
        Assert.That(root.RenderCount, Is.EqualTo(2));
        Assert.That(root.Parameters.Select(parameter => parameter.Name), Is.EqualTo(new[] { "Title", "ItemCount" }));
    }

    private sealed class FakeComponent : ComponentBase;
}

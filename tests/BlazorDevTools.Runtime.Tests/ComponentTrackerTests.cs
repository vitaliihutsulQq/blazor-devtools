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
        tracker.UpdateCascadingParameters(
            "root",
            [
                new("EditContext", "EditContext", "Microsoft.AspNetCore.Components.Forms.EditContext", null),
                new("ThemeName", "String", "System.String", "ThemeName")
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
        tracker.UpdateRenderDiffInfo(
            "root",
            new ComponentRenderDiffInfoSnapshot(
                new ComponentRenderDiffSnapshot(
                    2,
                    new DateTimeOffset(2026, 04, 25, 14, 03, 11, TimeSpan.Zero).AddMilliseconds(973),
                    true,
                    [
                        new("Title", "Old title", "Dashboard")
                    ]),
                [
                    new ComponentRenderDiffSnapshot(
                        1,
                        new DateTimeOffset(2026, 04, 25, 14, 03, 10, TimeSpan.Zero).AddMilliseconds(125),
                        false,
                        []),
                    new ComponentRenderDiffSnapshot(
                        2,
                        new DateTimeOffset(2026, 04, 25, 14, 03, 11, TimeSpan.Zero).AddMilliseconds(973),
                        true,
                        [
                            new("Title", "Old title", "Dashboard")
                        ])
                ]));
        tracker.SetDomMarker("root", "component-root");

        var snapshot = tracker.BuildSnapshot();
        var root = snapshot.Roots[0];

        Assert.That(root.FullTypeName, Is.EqualTo(typeof(FakeComponent).FullName));
        Assert.That(root.AssemblyName, Is.EqualTo(typeof(FakeComponent).Assembly.GetName().Name));
        Assert.That(root.DomMarkerId, Is.EqualTo("component-root"));
        Assert.That(root.InjectedServices.Select(service => service.PropertyName), Is.EqualTo(new[] { "WorkspaceService" }));
        Assert.That(root.CascadingParameters.Select(parameter => parameter.PropertyName), Is.EqualTo(new[] { "EditContext", "ThemeName" }));
        Assert.That(root.LifecycleMetrics?.TimeToFirstRenderMs, Is.EqualTo(12.5));
        Assert.That(root.LifecycleMetrics?.AverageRenderTimeMs, Is.EqualTo(2.75));
        Assert.That(root.LifecycleMetrics?.StateHasChangedCount, Is.EqualTo(2));
        Assert.That(root.RenderDiffInfo?.LatestRenderDiff?.RecordedAt, Is.EqualTo(new DateTimeOffset(2026, 04, 25, 14, 03, 11, TimeSpan.Zero).AddMilliseconds(973)));
        Assert.That(root.RenderDiffInfo?.LatestRenderDiff?.ParameterChanges.Select(change => change.Name), Is.EqualTo(new[] { "Title" }));
        Assert.That(root.RenderCount, Is.EqualTo(2));
        Assert.That(root.Parameters.Select(parameter => parameter.Name), Is.EqualTo(new[] { "Title", "ItemCount" }));
        Assert.That(snapshot.DependencyGraph.Nodes.Select(graphNode => graphNode.ComponentId), Is.EqualTo(new[] { "root" }));
    }

    [Test]
    public void BuildSnapshot_includes_dependency_graph_edges_with_exact_and_inferred_relationships()
    {
        var tracker = new ComponentTracker();

        tracker.RegisterComponent("parent", "ParentComponent");
        tracker.RegisterComponent("child", "ChildComponent", "parent");
        tracker.UpdateParameters(
            "child",
            [
                new("Title", "Dashboard"),
                new("Count", "2")
            ]);
        tracker.UpdateCascadingParameters(
            "child",
            [
                new("Theme", "String", "System.String", "ThemeName")
            ]);

        var snapshot = tracker.BuildSnapshot();

        Assert.That(snapshot.DependencyGraph.Nodes.Select(node => node.ComponentId), Is.EqualTo(new[] { "parent", "child" }));

        var parentChildEdge = snapshot.DependencyGraph.Edges.Single(edge => edge.EdgeType == ComponentDependencyEdgeTypes.ParentChild);
        Assert.Multiple(() =>
        {
            Assert.That(parentChildEdge.SourceComponentId, Is.EqualTo("parent"));
            Assert.That(parentChildEdge.TargetComponentId, Is.EqualTo("child"));
            Assert.That(parentChildEdge.IsInferred, Is.False);
        });

        var parameterFlowEdge = snapshot.DependencyGraph.Edges.Single(edge => edge.EdgeType == ComponentDependencyEdgeTypes.ParameterFlow);
        Assert.Multiple(() =>
        {
            Assert.That(parameterFlowEdge.SourceComponentId, Is.EqualTo("parent"));
            Assert.That(parameterFlowEdge.TargetComponentId, Is.EqualTo("child"));
            Assert.That(parameterFlowEdge.RelatedValues, Is.EqualTo(new[] { "Title", "Count" }));
            Assert.That(parameterFlowEdge.IsInferred, Is.True);
        });

        var cascadingDependencyEdge = snapshot.DependencyGraph.Edges.Single(edge => edge.EdgeType == ComponentDependencyEdgeTypes.CascadingDependency);
        Assert.Multiple(() =>
        {
            Assert.That(cascadingDependencyEdge.SourceComponentId, Is.EqualTo("parent"));
            Assert.That(cascadingDependencyEdge.TargetComponentId, Is.EqualTo("child"));
            Assert.That(cascadingDependencyEdge.RelatedValues, Is.EqualTo(new[] { "Theme (Name: ThemeName)" }));
            Assert.That(cascadingDependencyEdge.IsInferred, Is.True);
            Assert.That(cascadingDependencyEdge.Details, Does.Contain("exact provider component is not currently proven"));
        });
    }

    [Test]
    public void BuildSnapshot_does_not_invent_cascading_provider_edge_without_tracked_ancestor_context()
    {
        var tracker = new ComponentTracker();

        tracker.RegisterComponent("root", "RootComponent");
        tracker.UpdateCascadingParameters(
            "root",
            [
                new("Theme", "String", "System.String", "ThemeName")
            ]);

        var snapshot = tracker.BuildSnapshot();

        Assert.That(snapshot.DependencyGraph.Edges.Any(edge => edge.EdgeType == ComponentDependencyEdgeTypes.CascadingDependency), Is.False);
    }

    private sealed class FakeComponent : ComponentBase;
}

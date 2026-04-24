using BlazorDevTools.Protocol;

namespace BlazorDevTools.Runtime;

public sealed class ComponentTracker
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, TrackedComponent> components = new();
    private long nextSequence;

    public void RegisterComponent(string componentId, string componentName, string? parentComponentId = null)
    {
        RegisterComponent(componentId, componentName, componentName, typeof(ComponentTracker).Assembly.GetName().Name ?? "Unknown", parentComponentId);
    }

    public void RegisterSyntheticComponent(
        string componentId,
        string componentName,
        string fullTypeName,
        string assemblyName,
        string? parentComponentId = null,
        IReadOnlyList<ComponentParameterSnapshot>? parameters = null)
    {
        RegisterComponent(componentId, componentName, fullTypeName, assemblyName, parentComponentId);

        if (parameters is not null)
        {
            UpdateParameters(componentId, parameters);
        }
    }

    public void RegisterComponent(string componentId, Type componentType, string? parentComponentId = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        RegisterComponent(
            componentId,
            componentType.Name,
            componentType.FullName ?? componentType.Name,
            componentType.Assembly.GetName().Name ?? "Unknown",
            parentComponentId);
    }

    public void UpdateParameters(string componentId, IReadOnlyList<ComponentParameterSnapshot> parameters)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);
        ArgumentNullException.ThrowIfNull(parameters);

        lock (syncRoot)
        {
            if (!components.TryGetValue(componentId, out var trackedComponent))
            {
                return;
            }

            trackedComponent.Parameters = parameters;
        }
    }

    public void UpdateInjectedServices(string componentId, IReadOnlyList<ComponentInjectedServiceSnapshot> injectedServices)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);
        ArgumentNullException.ThrowIfNull(injectedServices);

        lock (syncRoot)
        {
            if (!components.TryGetValue(componentId, out var trackedComponent))
            {
                return;
            }

            trackedComponent.InjectedServices = injectedServices;
        }
    }

    public void UpdateCascadingParameters(string componentId, IReadOnlyList<ComponentCascadingParameterSnapshot> cascadingParameters)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);
        ArgumentNullException.ThrowIfNull(cascadingParameters);

        lock (syncRoot)
        {
            if (!components.TryGetValue(componentId, out var trackedComponent))
            {
                return;
            }

            trackedComponent.CascadingParameters = cascadingParameters;
        }
    }

    public void UpdateLifecycleMetrics(string componentId, ComponentLifecycleMetricsSnapshot lifecycleMetrics)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);
        ArgumentNullException.ThrowIfNull(lifecycleMetrics);

        lock (syncRoot)
        {
            if (!components.TryGetValue(componentId, out var trackedComponent))
            {
                return;
            }

            trackedComponent.LifecycleMetrics = lifecycleMetrics;
            trackedComponent.RenderCount = lifecycleMetrics.RenderCount;
        }
    }

    public void UpdateRenderInfo(string componentId, ComponentRenderInfoSnapshot renderInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);
        ArgumentNullException.ThrowIfNull(renderInfo);

        lock (syncRoot)
        {
            if (!components.TryGetValue(componentId, out var trackedComponent))
            {
                return;
            }

            trackedComponent.RenderInfo = renderInfo;
        }
    }

    public void UpdateRenderDiffInfo(string componentId, ComponentRenderDiffInfoSnapshot renderDiffInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);
        ArgumentNullException.ThrowIfNull(renderDiffInfo);

        lock (syncRoot)
        {
            if (!components.TryGetValue(componentId, out var trackedComponent))
            {
                return;
            }

            trackedComponent.RenderDiffInfo = renderDiffInfo;
        }
    }

    public bool TryReparentDetachedComponent(Type componentType, string parentComponentId)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentException.ThrowIfNullOrEmpty(parentComponentId);

        lock (syncRoot)
        {
            if (!components.TryGetValue(parentComponentId, out var parentComponent))
            {
                return false;
            }

            var candidate = components.Values
                .Where(component =>
                    component.Id != parentComponentId &&
                    component.FullTypeName == (componentType.FullName ?? componentType.Name) &&
                    (component.ParentComponentId is null || !components.ContainsKey(component.ParentComponentId)))
                .OrderByDescending(component => component.Sequence)
                .FirstOrDefault();

            if (candidate is null)
            {
                return false;
            }

            candidate.ParentComponentId = parentComponentId;
            parentComponent.ChildComponentIds.Add(candidate.Id);
            return true;
        }
    }

    public void IncrementRenderCount(string componentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);

        lock (syncRoot)
        {
            if (!components.TryGetValue(componentId, out var trackedComponent))
            {
                return;
            }

            trackedComponent.RenderCount = (trackedComponent.RenderCount ?? 0) + 1;
        }
    }

    public void SetDomMarker(string componentId, string? domMarkerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);

        lock (syncRoot)
        {
            if (!components.TryGetValue(componentId, out var trackedComponent))
            {
                return;
            }

            trackedComponent.DomMarkerId = domMarkerId;
        }
    }

    private void RegisterComponent(
        string componentId,
        string componentName,
        string fullTypeName,
        string assemblyName,
        string? parentComponentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);
        ArgumentException.ThrowIfNullOrEmpty(componentName);
        ArgumentException.ThrowIfNullOrEmpty(fullTypeName);
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);

        lock (syncRoot)
        {
            if (components.TryGetValue(componentId, out var existing) &&
                existing.ParentComponentId is not null &&
                components.TryGetValue(existing.ParentComponentId, out var existingParent))
            {
                existingParent.ChildComponentIds.Remove(componentId);
            }

            var trackedComponent = existing ?? new TrackedComponent(componentId, nextSequence++);
            trackedComponent.Name = componentName;
            trackedComponent.FullTypeName = fullTypeName;
            trackedComponent.AssemblyName = assemblyName;
            trackedComponent.ParentComponentId = parentComponentId;

            components[componentId] = trackedComponent;

            if (parentComponentId is not null && components.TryGetValue(parentComponentId, out var parentComponent))
            {
                parentComponent.ChildComponentIds.Add(componentId);
            }
        }
    }

    public void UnregisterComponent(string componentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentId);

        lock (syncRoot)
        {
            if (!components.Remove(componentId, out var trackedComponent))
            {
                return;
            }

            if (trackedComponent.ParentComponentId is not null &&
                components.TryGetValue(trackedComponent.ParentComponentId, out var parentComponent))
            {
                parentComponent.ChildComponentIds.Remove(componentId);
            }

            foreach (var childComponentId in trackedComponent.ChildComponentIds)
            {
                if (components.TryGetValue(childComponentId, out var childComponent))
                {
                    childComponent.ParentComponentId = null;
                }
            }
        }
    }

    public ComponentTreeSnapshot BuildSnapshot()
    {
        lock (syncRoot)
        {
            var orderedComponents = components.Values
                .OrderBy(component => component.Sequence)
                .ToArray();

            var roots = orderedComponents
                .Where(component => component.ParentComponentId is null || !components.ContainsKey(component.ParentComponentId))
                .Select(BuildNode)
                .ToArray();

            var dependencyGraph = BuildDependencyGraph(orderedComponents);

            return new ComponentTreeSnapshot(DateTimeOffset.UtcNow, roots, dependencyGraph);
        }
    }

    private ComponentDependencyGraphSnapshot BuildDependencyGraph(IReadOnlyList<TrackedComponent> orderedComponents)
    {
        var nodes = orderedComponents
            .Select(component => new ComponentDependencyGraphNodeSnapshot(component.Id, component.Name, component.FullTypeName))
            .ToArray();

        var edges = new List<ComponentDependencyGraphEdgeSnapshot>();

        foreach (var component in orderedComponents)
        {
            if (component.ParentComponentId is null || !components.TryGetValue(component.ParentComponentId, out var parentComponent))
            {
                continue;
            }

            edges.Add(new ComponentDependencyGraphEdgeSnapshot(
                parentComponent.Id,
                component.Id,
                ComponentDependencyEdgeTypes.ParentChild,
                "Child",
                [],
                false,
                null));

            if (component.Parameters.Count > 0)
            {
                edges.Add(new ComponentDependencyGraphEdgeSnapshot(
                    parentComponent.Id,
                    component.Id,
                    ComponentDependencyEdgeTypes.ParameterFlow,
                    component.Parameters.Count == 1 ? "1 param" : $"{component.Parameters.Count} params",
                    component.Parameters.Select(parameter => parameter.Name).ToArray(),
                    true,
                    "Parameter flow is inferred from the parent-child relationship. The runtime does not yet trace the exact call site that assigned each parameter."));
            }

            if (component.CascadingParameters.Count > 0)
            {
                edges.Add(new ComponentDependencyGraphEdgeSnapshot(
                    parentComponent.Id,
                    component.Id,
                    ComponentDependencyEdgeTypes.CascadingDependency,
                    component.CascadingParameters.Count == 1 ? "1 cascading" : $"{component.CascadingParameters.Count} cascading",
                    component.CascadingParameters
                        .Select(parameter => parameter.ProviderHint is { Length: > 0 }
                            ? $"{parameter.PropertyName} (Name: {parameter.ProviderHint})"
                            : parameter.PropertyName)
                        .ToArray(),
                    true,
                    "The exact provider component is not currently proven. This edge is anchored to the nearest tracked parent/ancestor context."));
            }
        }

        return new ComponentDependencyGraphSnapshot(nodes, edges);
    }

    private ComponentNode BuildNode(TrackedComponent component)
    {
        var children = component.ChildComponentIds
            .Select(childComponentId => components.TryGetValue(childComponentId, out var childComponent) ? childComponent : null)
            .Where(childComponent => childComponent is not null)
            .OrderBy(childComponent => childComponent!.Sequence)
            .Select(childComponent => BuildNode(childComponent!))
            .ToArray();

        return new ComponentNode(
            component.Id,
            component.Name,
            component.FullTypeName,
            component.AssemblyName,
            component.DomMarkerId,
            component.Parameters,
            component.InjectedServices,
            component.CascadingParameters,
            component.LifecycleMetrics,
            component.RenderInfo,
            component.RenderDiffInfo,
            component.RenderCount,
            children);
    }

    private sealed class TrackedComponent
    {
        public TrackedComponent(string id, long sequence)
        {
            Id = id;
            Sequence = sequence;
        }

        public string Id { get; }

        public string Name { get; set; } = string.Empty;

        public string FullTypeName { get; set; } = string.Empty;

        public string AssemblyName { get; set; } = string.Empty;

        public string? DomMarkerId { get; set; }

        public string? ParentComponentId { get; set; }

        public IReadOnlyList<ComponentParameterSnapshot> Parameters { get; set; } = [];

        public IReadOnlyList<ComponentInjectedServiceSnapshot> InjectedServices { get; set; } = [];

        public IReadOnlyList<ComponentCascadingParameterSnapshot> CascadingParameters { get; set; } = [];

        public ComponentLifecycleMetricsSnapshot? LifecycleMetrics { get; set; }

        public ComponentRenderInfoSnapshot? RenderInfo { get; set; }

        public ComponentRenderDiffInfoSnapshot? RenderDiffInfo { get; set; }

        public int? RenderCount { get; set; }

        public long Sequence { get; }

        public HashSet<string> ChildComponentIds { get; } = [];
    }
}

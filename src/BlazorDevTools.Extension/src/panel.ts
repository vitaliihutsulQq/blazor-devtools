type ComponentNode = {
  id: string;
  name: string;
  fullTypeName: string;
  assemblyName: string;
  domMarkerId?: string | null;
  parameters?: ComponentParameterSnapshot[];
  injectedServices?: ComponentInjectedServiceSnapshot[];
  cascadingParameters?: ComponentCascadingParameterSnapshot[];
  lifecycleMetrics?: ComponentLifecycleMetricsSnapshot | null;
  renderInfo?: ComponentRenderInfoSnapshot | null;
  renderDiffInfo?: ComponentRenderDiffInfoSnapshot | null;
  renderCount?: number | null;
  children: ComponentNode[];
};

type ComponentParameterSnapshot = {
  name: string;
  value: string | null;
};

type ComponentInjectedServiceSnapshot = {
  propertyName: string;
  serviceTypeName: string;
  fullServiceTypeName: string;
};

type ComponentCascadingParameterSnapshot = {
  propertyName: string;
  valueTypeName: string;
  fullValueTypeName: string;
  providerHint: string | null;
};

type ComponentLifecycleMetricsSnapshot = {
  timeToFirstRenderMs: number | null;
  renderCount: number;
  averageRenderTimeMs: number | null;
  stateHasChangedCount: number;
  onInitializedTimeMs: number | null;
  onInitializedAsyncTimeMs: number | null;
  onParametersSetTimeMs: number | null;
  onAfterRenderTimeMs: number | null;
  totalRenderTimeMs: number;
};

type ComponentRenderCauseSnapshot = {
  renderSequence: number;
  cause: string;
  isApproximate: boolean;
  details: string | null;
};

type ComponentRenderInfoSnapshot = {
  latestRenderCause: ComponentRenderCauseSnapshot | null;
  recentRenderCauses: ComponentRenderCauseSnapshot[];
};

type ComponentParameterDiffSnapshot = {
  name: string;
  previousValue: string | null;
  currentValue: string | null;
};

type ComponentRenderDiffSnapshot = {
  renderSequence: number;
  recordedAt: string;
  hasPreviousSnapshot: boolean;
  parameterChanges: ComponentParameterDiffSnapshot[];
};

type ComponentRenderDiffInfoSnapshot = {
  latestRenderDiff: ComponentRenderDiffSnapshot | null;
  recentRenderDiffs: ComponentRenderDiffSnapshot[];
};

type ComponentDependencyGraphNodeSnapshot = {
  componentId: string;
  name: string;
  fullTypeName: string;
};

type ComponentDependencyGraphEdgeSnapshot = {
  sourceComponentId: string;
  targetComponentId: string;
  edgeType: string;
  summary: string;
  relatedValues: string[];
  isInferred: boolean;
  details: string | null;
};

type ComponentDependencyGraphSnapshot = {
  nodes: ComponentDependencyGraphNodeSnapshot[];
  edges: ComponentDependencyGraphEdgeSnapshot[];
};

type ComponentTreeSnapshot = {
  capturedAt: string;
  roots: ComponentNode[];
  dependencyGraph: ComponentDependencyGraphSnapshot;
};

type DevToolsMessageEnvelope = {
  source: string;
  messageType: string;
  payload: ComponentTreeSnapshot;
};

type RelayMessage = {
  kind: string;
  payload: unknown;
};

type PickedComponentPayload = {
  componentId: string;
};

type InspectModePayload = {
  active: boolean;
};

type DetailsTab = "overview" | "parameters" | "dependencies" | "graph" | "performance" | "render-cause" | "render-diff";

type TreeNodeViewModel = {
  id: string;
  name: string;
  fullTypeName: string;
  assemblyName: string;
  domMarkerId: string | null;
  parentId: string | null;
  childrenCount: number;
  parameters: ComponentParameterSnapshot[];
  injectedServices: ComponentInjectedServiceSnapshot[];
  cascadingParameters: ComponentCascadingParameterSnapshot[];
  lifecycleMetrics: ComponentLifecycleMetricsSnapshot | null;
  renderInfo: ComponentRenderInfoSnapshot | null;
  renderDiffInfo: ComponentRenderDiffInfoSnapshot | null;
  renderCount: number | null;
  children: TreeNodeViewModel[];
};

type FocusedDependencyGraph = {
  nodes: ComponentDependencyGraphNodeSnapshot[];
  edges: ComponentDependencyGraphEdgeSnapshot[];
};

type MetricSeverity = "good" | "warning" | "bad" | "neutral";

type DurationThresholds = {
  goodMaxMs: number;
  warningMaxMs: number;
};

type CountThresholds = {
  goodMax: number;
  warningMax: number;
};

type MetricCardOptions = {
  severity?: MetricSeverity;
  emphasizeLabel?: boolean;
};

const firstRenderDurationThresholds: DurationThresholds = {
  goodMaxMs: 16,
  warningMaxMs: 50
};

const averageRenderDurationThresholds: DurationThresholds = {
  goodMaxMs: 4,
  warningMaxMs: 12
};

const totalRenderDurationThresholds: DurationThresholds = {
  goodMaxMs: 30,
  warningMaxMs: 80
};

const standardLifecycleDurationThresholds: DurationThresholds = {
  goodMaxMs: 0.5,
  warningMaxMs: 2
};

const asyncLifecycleDurationThresholds: DurationThresholds = {
  goodMaxMs: 2,
  warningMaxMs: 10
};

const renderCountThresholds: CountThresholds = {
  // make render count thresholds less aggressive so occasional re-renders don't appear alarming
  goodMax: 2,
  warningMax: 10
};

const stateHasChangedCountThresholds: CountThresholds = {
  goodMax: 1,
  warningMax: 3
};

document.addEventListener("DOMContentLoaded", () => {
  const panelLayout = document.getElementById("panel-layout");
  const treePane = document.getElementById("tree-pane");
  const detailsPane = document.getElementById("details-pane");
  const paneSplitter = document.getElementById("pane-splitter");
  const treeRoot = document.getElementById("tree-root");
  const detailsRoot = document.getElementById("details-root");
  const status = document.getElementById("snapshot-status");
  const inspectToggle = document.getElementById("inspect-toggle");
  const treeFilter = document.getElementById("tree-filter");

  if (!panelLayout
    || !treePane
    || !detailsPane
    || !paneSplitter
    || !treeRoot
    || !detailsRoot
    || !status
    || !(inspectToggle instanceof HTMLButtonElement)
    || !(treeFilter instanceof HTMLInputElement)
    || !(paneSplitter instanceof HTMLDivElement)) {
    return;
  }

  const treePaneMinWidthPx = 260;
  const detailsPaneMinWidthPx = 340;
  const splitterWidthPx = 12;
  const splitterKeyboardStepPx = 24;
  const splitterKeyboardLargeStepPx = 64;
  const paneWidthStorageKey = "blazor-devtools:panel-tree-pane-width";
  const stackedLayoutMedia = window.matchMedia("(max-width: 720px)");

  const tabId = getInspectedTabId();
  const expandedNodeIds = new Set<string>();
  let selectedNodeId: string | null = null;
  let currentTree: TreeNodeViewModel[] = [];
  let currentDependencyGraph: ComponentDependencyGraphSnapshot | null = null;
  let inspectModeActive = false;
  let searchTerm = "";
  let showOnlyChangedRenders = false;
  let selectedRenderSequence: number | null = null;
  let renderHistoryCollapsed = false;
  let selectedDetailsTab: DetailsTab = "overview";
  let activeResizePointerId: number | null = null;
  let resizeStartX = 0;
  let resizeStartTreePaneWidth = 0;
  let currentTreePaneWidth = 0;

  const isStackedLayout = (): boolean => stackedLayoutMedia.matches;

  const readPersistedTreePaneWidth = (): number | null => {
    try {
      const stored = window.localStorage.getItem(paneWidthStorageKey);
      if (!stored) {
        return null;
      }

      const parsed = Number.parseFloat(stored);
      return Number.isFinite(parsed) ? parsed : null;
    } catch {
      return null;
    }
  };

  const persistTreePaneWidth = (width: number): void => {
    try {
      window.localStorage.setItem(paneWidthStorageKey, width.toString());
    } catch {
      // ignore storage access failures inside the DevTools panel
    }
  };

  const getPanelResizeBounds = (): { min: number; max: number } => {
    const availableWidth = panelLayout.clientWidth - splitterWidthPx;
    const max = Math.max(treePaneMinWidthPx, availableWidth - detailsPaneMinWidthPx);
    return {
      min: treePaneMinWidthPx,
      max
    };
  };

  const updateSplitterAria = (width: number): void => {
    const bounds = getPanelResizeBounds();
    paneSplitter.setAttribute("aria-valuemin", bounds.min.toString());
    paneSplitter.setAttribute("aria-valuemax", bounds.max.toString());
    paneSplitter.setAttribute("aria-valuenow", Math.round(width).toString());
  };

  const clampTreePaneWidth = (candidateWidth: number): number => {
    const bounds = getPanelResizeBounds();
    return Math.round(Math.min(bounds.max, Math.max(bounds.min, candidateWidth)));
  };

  const clearTreePaneWidth = (): void => {
    panelLayout.style.removeProperty("--tree-pane-width");
    currentTreePaneWidth = Math.round(treePane.getBoundingClientRect().width);
    updateSplitterAria(currentTreePaneWidth);
  };

  const getDefaultTreePaneWidth = (): number => {
    const measuredWidth = Math.round(treePane.getBoundingClientRect().width);
    if (measuredWidth > 0) {
      return clampTreePaneWidth(measuredWidth);
    }

    return clampTreePaneWidth(Math.round(panelLayout.clientWidth * 0.38));
  };

  const applyTreePaneWidth = (candidateWidth: number, persistWidth: boolean): void => {
    if (isStackedLayout()) {
      clearTreePaneWidth();
      return;
    }

    const width = clampTreePaneWidth(candidateWidth);
    currentTreePaneWidth = width;
    panelLayout.style.setProperty("--tree-pane-width", `${width}px`);
    updateSplitterAria(width);

    if (persistWidth) {
      persistTreePaneWidth(width);
    }
  };

  const syncTreePaneWidth = (persistWidth: boolean): void => {
    if (isStackedLayout()) {
      clearTreePaneWidth();
      return;
    }

    const targetWidth = currentTreePaneWidth > 0
      ? currentTreePaneWidth
      : readPersistedTreePaneWidth() ?? getDefaultTreePaneWidth();
    applyTreePaneWidth(targetWidth, persistWidth);
  };

  const stopPaneResize = (pointerId: number | null): void => {
    if (activeResizePointerId === null || (pointerId !== null && activeResizePointerId !== pointerId)) {
      return;
    }

    const releasedPointerId = activeResizePointerId;
    activeResizePointerId = null;
    paneSplitter.classList.remove("is-active");
    document.body.classList.remove("is-resizing-panes");

    if (paneSplitter.hasPointerCapture(releasedPointerId)) {
      paneSplitter.releasePointerCapture(releasedPointerId);
    }

    if (!isStackedLayout() && currentTreePaneWidth > 0) {
      persistTreePaneWidth(currentTreePaneWidth);
    }
  };

  paneSplitter.addEventListener("pointerdown", (event) => {
    if (event.button !== 0 || isStackedLayout()) {
      return;
    }

    activeResizePointerId = event.pointerId;
    resizeStartX = event.clientX;
    resizeStartTreePaneWidth = currentTreePaneWidth > 0
      ? currentTreePaneWidth
      : getDefaultTreePaneWidth();
    paneSplitter.classList.add("is-active");
    document.body.classList.add("is-resizing-panes");
    paneSplitter.setPointerCapture(event.pointerId);
    event.preventDefault();
  });

  paneSplitter.addEventListener("pointermove", (event) => {
    if (activeResizePointerId !== event.pointerId) {
      return;
    }

    const deltaX = event.clientX - resizeStartX;
    applyTreePaneWidth(resizeStartTreePaneWidth + deltaX, false);
  });

  paneSplitter.addEventListener("pointerup", (event) => {
    stopPaneResize(event.pointerId);
  });

  paneSplitter.addEventListener("pointercancel", (event) => {
    stopPaneResize(event.pointerId);
  });

  paneSplitter.addEventListener("keydown", (event) => {
    if (isStackedLayout()) {
      return;
    }

    const step = event.shiftKey ? splitterKeyboardLargeStepPx : splitterKeyboardStepPx;
    const bounds = getPanelResizeBounds();
    let nextWidth: number | null = null;

    switch (event.key) {
      case "ArrowLeft":
        nextWidth = (currentTreePaneWidth || getDefaultTreePaneWidth()) - step;
        break;
      case "ArrowRight":
        nextWidth = (currentTreePaneWidth || getDefaultTreePaneWidth()) + step;
        break;
      case "Home":
        nextWidth = bounds.min;
        break;
      case "End":
        nextWidth = bounds.max;
        break;
      default:
        return;
    }

    event.preventDefault();
    applyTreePaneWidth(nextWidth, true);
  });

  const panelResizeObserver = new ResizeObserver(() => {
    if (activeResizePointerId !== null) {
      return;
    }

    syncTreePaneWidth(false);
  });
  panelResizeObserver.observe(panelLayout);

  stackedLayoutMedia.addEventListener("change", () => {
    if (stackedLayoutMedia.matches) {
      stopPaneResize(null);
      clearTreePaneWidth();
      return;
    }

    syncTreePaneWidth(false);
  });

  syncTreePaneWidth(false);

  const render = () => {
    treeRoot.replaceChildren();
    detailsRoot.replaceChildren();

    const visibleTree = filterTreeNodes(currentTree, searchTerm);

    if (currentTree.length === 0) {
      treeRoot.append(createEmptyState("Click the export button in the sample app after opening this panel."));
      detailsRoot.append(createEmptyState("Select a component node to inspect its metadata."));
      return;
    }

    if (visibleTree.length === 0) {
      treeRoot.append(createEmptyState(`No components match "${searchTerm}".`));
      detailsRoot.append(createEmptyState("Select a component node to inspect its metadata."));
      return;
    }

    treeRoot.append(renderTreeLevel(visibleTree));

    const selectedNode = selectedNodeId ? findNodeById(currentTree, selectedNodeId) : null;
    if (selectedNode) {
      detailsRoot.append(renderDetails(selectedNode));
      return;
    }

    detailsRoot.append(createEmptyState("Select a component node to inspect its metadata."));
  };

  const handleSnapshot = (snapshot: ComponentTreeSnapshot) => {
    currentTree = snapshot.roots.map((root) => toTreeNode(root, null));
    currentDependencyGraph = snapshot.dependencyGraph;

    if (!selectedNodeId || !findNodeById(currentTree, selectedNodeId)) {
      selectedNodeId = currentTree[0]?.id ?? null;
    }

    if (selectedNodeId) {
      expandPathToNode(currentTree, selectedNodeId, expandedNodeIds);
    }

    status.textContent = `Snapshot captured at ${new Date(snapshot.capturedAt).toLocaleTimeString()}.`;
    render();
  };

  const requestInitialSnapshot = async () => {
    if (tabId === null) {
      status.textContent = "Blazor DevTools is unavailable because the inspected tab could not be resolved.";
      return;
    }

    const delivered = await safeTabsSendMessage(tabId, {
      kind: "blazor-devtools:request-snapshot"
    });

    if (!delivered) {
      status.textContent = "Waiting for the inspected Blazor app to expose Blazor DevTools runtime hooks.";
    }
  };

  const setInspectToggleState = (active: boolean) => {
    inspectModeActive = active;
    inspectToggle.classList.toggle("is-active", active);
    inspectToggle.textContent = active ? "Picking Active" : "Pick From Page";
  };

  const selectNodeById = (componentId: string) => {
    if (!findNodeById(currentTree, componentId)) {
      status.textContent = `Picked component ${componentId} was not found in the current snapshot.`;
      render();
      return;
    }

    selectedNodeId = componentId;
    selectedRenderSequence = null;
    expandPathToNode(currentTree, componentId, expandedNodeIds);
    status.textContent = `Selected component ${componentId} from page inspect mode.`;
    render();
  };

  chrome.runtime.onMessage.addListener((message: RelayMessage) => {
    if (!message) {
      return;
    }

    if (message.kind === "blazor-devtools:relay") {
      const snapshot = extractSnapshot(message.payload);
      if (!snapshot) {
        status.textContent = "Received an unsupported snapshot payload.";
        return;
      }

      handleSnapshot(snapshot);
      return;
    }

    if (message.kind === "blazor-devtools:component-picked" && isPickedComponentPayload(message.payload)) {
      setInspectToggleState(false);
      selectNodeById(message.payload.componentId);
      return;
    }

    if (message.kind === "blazor-devtools:inspect-mode-changed" && isInspectModePayload(message.payload)) {
      setInspectToggleState(message.payload.active);
    }
  });

  inspectToggle.addEventListener("click", () => {
    if (tabId === null) {
      status.textContent = "Inspect mode is unavailable because the inspected tab could not be resolved.";
      return;
    }

    const nextState = !inspectModeActive;
    setInspectToggleState(nextState);
    status.textContent = nextState
      ? "Inspect mode active. Hover the page and click a highlighted Blazor component."
      : "Inspect mode disabled.";

    void (async () => {
      const delivered = await safeTabsSendMessage(tabId, {
        kind: "blazor-devtools:set-inspect-mode",
        payload: { active: nextState }
      });

      if (!delivered) {
        setInspectToggleState(false);
        status.textContent = "Inspect mode is unavailable because the inspected page is not ready for Blazor DevTools yet.";
      }
    })();
  });

  treeFilter.addEventListener("input", () => {
    searchTerm = treeFilter.value.trim();
    render();
  });

  render();
  void requestInitialSnapshot();

  function renderTreeLevel(nodes: TreeNodeViewModel[]): HTMLOListElement {
    const list = document.createElement("ol");
    list.className = "tree-level";

    for (const node of nodes) {
      const listItem = document.createElement("li");
      listItem.className = "tree-item";

      const row = document.createElement("div");
      row.className = "tree-row";

      if (node.children.length > 0) {
        const toggle = document.createElement("button");
        toggle.type = "button";
        toggle.className = "tree-toggle";
        toggle.textContent = expandedNodeIds.has(node.id) ? "-" : "+";
        toggle.setAttribute("aria-label", expandedNodeIds.has(node.id) ? `Collapse ${node.name}` : `Expand ${node.name}`);
        toggle.addEventListener("click", () => {
          if (expandedNodeIds.has(node.id)) {
            expandedNodeIds.delete(node.id);
          } else {
            expandedNodeIds.add(node.id);
          }

          render();
        });
        row.append(toggle);
      } else {
        const spacer = document.createElement("span");
        spacer.className = "tree-spacer";
        row.append(spacer);
      }

      const button = document.createElement("button");
      button.type = "button";
      button.className = "tree-node";
      if (node.id === selectedNodeId) {
        button.classList.add("is-selected");
      }

      const label = document.createElement("span");
      label.textContent = node.name;
      button.append(label);

      const meta = document.createElement("span");
      meta.className = "tree-meta";
      meta.textContent = `${node.childrenCount} child${node.childrenCount === 1 ? "" : "ren"}`;
      button.append(meta);

      button.addEventListener("click", () => {
        selectedNodeId = node.id;
        selectedRenderSequence = null;
        expandPathToNode(currentTree, node.id, expandedNodeIds);
        render();
      });

      row.append(button);
      listItem.append(row);

      if (node.children.length > 0 && (searchTerm.length > 0 || expandedNodeIds.has(node.id))) {
        listItem.append(renderTreeLevel(node.children));
      }

      list.append(listItem);
    }

    return list;
  }

  function renderDetails(node: TreeNodeViewModel): HTMLDivElement {
    const wrapper = document.createElement("div");
    wrapper.className = "details-shell";

    const sticky = document.createElement("div");
    sticky.className = "details-sticky";

    const title = document.createElement("h3");
    title.className = "details-name";
    title.textContent = node.name;
    sticky.append(title);

    const subtitle = document.createElement("p");
    subtitle.className = "details-subtitle";
    subtitle.textContent = node.fullTypeName;
    sticky.append(subtitle);

    const summaryGrid = document.createElement("div");
    summaryGrid.className = "summary-grid";
    summaryGrid.append(
      createSummaryCard("Render Count", node.renderCount === null ? "Unavailable" : node.renderCount.toString(), {
        severity: getSeverityFromCount(node.renderCount, renderCountThresholds),
        emphasizeLabel: true
      }),
      createSummaryCard("Time to First Render", formatDuration(node.lifecycleMetrics?.timeToFirstRenderMs ?? null), {
        severity: getSeverityFromDuration(node.lifecycleMetrics?.timeToFirstRenderMs ?? null, firstRenderDurationThresholds),
        emphasizeLabel: true
      }),
      createSummaryCard("Avg Render Time", formatDuration(node.lifecycleMetrics?.averageRenderTimeMs ?? null), {
        severity: getSeverityFromDuration(node.lifecycleMetrics?.averageRenderTimeMs ?? null, averageRenderDurationThresholds),
        emphasizeLabel: true
      }),
      createSummaryCard("Latest Render Cause", node.renderInfo?.latestRenderCause ? formatRenderCause(node.renderInfo.latestRenderCause) : "Unavailable", {
        severity: "neutral",
        emphasizeLabel: true
      })
    );
    sticky.append(summaryGrid);

    sticky.append(renderTabNavigation());
    wrapper.append(sticky);

    const panel = document.createElement("div");
    panel.className = "tab-panel";

    switch (selectedDetailsTab) {
      case "overview":
        panel.append(renderOverview(node));
        break;
      case "parameters":
        panel.append(renderParameters(node.parameters));
        break;
      case "dependencies":
        panel.append(renderDependencies(node));
        break;
      case "graph":
        panel.classList.add("graph-tab-panel");
        panel.append(renderDependencyGraph(node));
        break;
      case "performance":
        panel.append(renderLifecycleMetrics(node.lifecycleMetrics));
        break;
      case "render-cause":
        panel.append(renderWhyDidThisRender(node.renderInfo));
        break;
      case "render-diff":
        panel.append(renderRenderDiff(node.renderDiffInfo, node.renderInfo));
        break;
    }

    wrapper.append(panel);
    return wrapper;
  }

  function renderOverview(node: TreeNodeViewModel): HTMLDivElement {
    const section = document.createElement("div");
    section.className = "overview-grid";
    section.append(
      createOverviewItem("Component name", node.name),
      createOverviewItem("Full type name", node.fullTypeName),
      createOverviewItem("Assembly name", node.assemblyName),
      createOverviewItem("Component id", node.id),
      createOverviewItem("DOM marker", node.domMarkerId ?? "Unavailable"),
      createOverviewItem("Parent id", node.parentId ?? "Root component"),
      createOverviewItem("Children count", node.childrenCount.toString()),
      createOverviewItem("Render count", node.renderCount === null ? "Unavailable" : node.renderCount.toString())
    );

    return section;
  }

  function renderTabNavigation(): HTMLDivElement {
    const tabs = document.createElement("div");
    tabs.className = "tabs";

    const tabDefinitions: Array<{ id: DetailsTab; label: string }> = [
      { id: "overview", label: "Overview" },
      { id: "parameters", label: "Parameters" },
      { id: "dependencies", label: "Dependencies" },
      { id: "graph", label: "Graph" },
      { id: "performance", label: "Performance" },
      { id: "render-cause", label: "Render Cause" },
      { id: "render-diff", label: "Render Diff" }
    ];

    for (const tabDefinition of tabDefinitions) {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "tab-button";
      if (selectedDetailsTab === tabDefinition.id) {
        button.classList.add("is-active");
      }

      button.textContent = tabDefinition.label;
      button.addEventListener("click", () => {
        selectedDetailsTab = tabDefinition.id;
        render();
      });
      tabs.append(button);
    }

    return tabs;
  }

  function renderDependencies(node: TreeNodeViewModel): HTMLDivElement {
    const section = document.createElement("div");
    section.className = "dependencies-section";
    section.append(
      renderInjectedServices(node.injectedServices),
      renderCascadingParameters(node.cascadingParameters)
    );
    return section;
  }

  function renderDependencyGraph(node: TreeNodeViewModel): HTMLDivElement {
    const section = document.createElement("div");
    section.className = "dependencies-graph-section";

    const title = document.createElement("h4");
    title.className = "dependencies-graph-title";
    title.textContent = "Dependencies Graph";
    section.append(title);

    const note = document.createElement("p");
    note.className = "dependencies-graph-note";
    note.textContent = "Focused graph for the selected component and its nearby tracked relationships. Use the zoom controls, Ctrl/Cmd + wheel, or drag the viewport to inspect the graph.";
    section.append(note);

    section.append(renderDependencyGraphLegend());

    if (!currentDependencyGraph) {
      section.append(createEmptyState("Dependency graph data is not available for this snapshot."));
      return section;
    }

    const focusedGraph = buildFocusedDependencyGraph(node.id, currentDependencyGraph);
    if (focusedGraph.nodes.length === 0) {
      section.append(createEmptyState("No nearby tracked dependencies are available for this component."));
      return section;
    }

    section.append(renderDependencyGraphCanvas(node.id, focusedGraph));

    const hasVisibleCascadingEdge = focusedGraph.edges.some(
      (edge) => edge.edgeType === "CascadingDependency" && edge.targetComponentId === node.id
    );

    if (node.cascadingParameters.length > 0 && !hasVisibleCascadingEdge) {
      const hint = document.createElement("p");
      hint.className = "dependencies-graph-hint";
      hint.textContent = "This component consumes cascading values, but the exact provider component is not currently proven in the tracked neighborhood.";
      section.append(hint);
    }

    return section;
  }

  function renderDependencyGraphLegend(): HTMLDivElement {
    const legend = document.createElement("div");
    legend.className = "dependency-graph-legend";

    legend.append(
      createDependencyLegendItem("Parent/child", "ParentChild"),
      createDependencyLegendItem("Parameter flow", "ParameterFlow"),
      createDependencyLegendItem("Cascading dependency", "CascadingDependency"),
      createDependencyStrengthLegendItem("Exact", false),
      createDependencyStrengthLegendItem("Inferred", true)
    );

    return legend;
  }

  function createDependencyLegendItem(labelText: string, edgeType: string): HTMLDivElement {
    const item = document.createElement("div");
    item.className = "dependency-graph-legend-item";

    const swatch = document.createElement("span");
    swatch.className = `dependency-graph-legend-swatch dependency-graph-legend-swatch--${edgeType}`;

    const label = document.createElement("span");
    label.className = "dependency-graph-legend-label";
    label.textContent = labelText;

    item.append(swatch, label);
    return item;
  }

  function createDependencyStrengthLegendItem(labelText: string, isInferred: boolean): HTMLDivElement {
    const item = document.createElement("div");
    item.className = "dependency-graph-legend-item";

    const swatch = document.createElement("span");
    swatch.className = `dependency-graph-legend-swatch ${isInferred
      ? "dependency-graph-legend-swatch--inferred"
      : "dependency-graph-legend-swatch--exact"}`;

    const label = document.createElement("span");
    label.className = "dependency-graph-legend-label";
    label.textContent = labelText;

    item.append(swatch, label);
    return item;
  }

  function renderDependencyGraphCanvas(selectedComponentId: string, focusedGraph: FocusedDependencyGraph): HTMLDivElement {
    const wrapper = document.createElement("div");
    wrapper.className = "dependency-graph-canvas";

    const controls = document.createElement("div");
    controls.className = "dependency-graph-controls";

    const controlsGroup = document.createElement("div");
    controlsGroup.className = "dependency-graph-controls-group";

    const zoomOutButton = createDependencyGraphControlButton("−", "Zoom out");
    const zoomInButton = createDependencyGraphControlButton("+", "Zoom in");
    const resetButton = createDependencyGraphControlButton("Reset", "Reset zoom");
    const fitButton = createDependencyGraphControlButton("Fit", "Fit graph to view");

    const zoomValue = document.createElement("span");
    zoomValue.className = "dependency-graph-zoom-value";
    zoomValue.textContent = "100%";

    controlsGroup.append(zoomOutButton, zoomValue, zoomInButton, resetButton, fitButton);

    const controlsHint = document.createElement("span");
    controlsHint.className = "dependency-graph-controls-hint";
    controlsHint.textContent = "Drag to pan · Ctrl/Cmd + wheel to zoom";

    controls.append(controlsGroup, controlsHint);
    wrapper.append(controls);

    const selectionSummary = document.createElement("p");
    selectionSummary.className = "dependency-graph-selection-summary";
    wrapper.append(selectionSummary);

    const viewport = document.createElement("div");
    viewport.className = "dependency-graph-viewport";
    viewport.tabIndex = 0;
    viewport.setAttribute("aria-label", "Dependencies graph viewport");

    const stage = document.createElement("div");
    stage.className = "dependency-graph-stage";

    const edgeTooltip = document.createElement("div");
    edgeTooltip.className = "dependency-graph-tooltip";
    edgeTooltip.dataset.horizontalPlacement = "right";
    edgeTooltip.dataset.verticalPlacement = "above";
    edgeTooltip.hidden = true;

    viewport.append(stage, edgeTooltip);
    wrapper.append(viewport);

    const svgNamespace = "http://www.w3.org/2000/svg";
    const svg = document.createElementNS(svgNamespace, "svg");
    svg.classList.add("dependency-graph-svg");
    svg.setAttribute("text-rendering", "geometricPrecision");

    const selectedNode = focusedGraph.nodes.find((node) => node.componentId === selectedComponentId);
    if (!selectedNode) {
      wrapper.append(createEmptyState("The selected component is not present in the dependency graph."));
      return wrapper;
    }

    const incomingNodes = getOrderedGraphNeighbors(focusedGraph, selectedComponentId, "incoming");
    const outgoingNodes = getOrderedGraphNeighbors(focusedGraph, selectedComponentId, "outgoing");
    const laneCount = Math.max(incomingNodes.length, outgoingNodes.length, 1);
    const viewBoxWidth = 980;
    const viewBoxHeight = Math.max(320, laneCount * 132 + 132);
    const nodeWidth = 228;
    const nodeHeight = 76;
    const leftX = 52;
    const centerX = Math.round((viewBoxWidth - nodeWidth) / 2);
    const rightX = viewBoxWidth - nodeWidth - 52;
    const minZoom = 0.35;
    const maxZoom = 2.5;
    const zoomStep = 0.18;
    const edgeTypeOrder = new Map<string, number>([
      ["ParentChild", 0],
      ["ParameterFlow", 1],
      ["CascadingDependency", 2]
    ]);
    const zoomState = {
      zoom: 1,
      defaultZoom: 1,
      fitZoom: 1
    };
    let isPanning = false;
    let activePointerId: number | null = null;
    let panStartX = 0;
    let panStartY = 0;
    let panStartScrollLeft = 0;
    let panStartScrollTop = 0;
    let activeTooltipAnchor: { svgX: number; svgY: number } | null = null;
    let hideTooltipTimeoutId: number | null = null;

    selectionSummary.textContent = `Selected in graph: ${selectedNode.name}. Click a node to inspect it in the details pane.`;

    svg.setAttribute("viewBox", `0 0 ${viewBoxWidth} ${viewBoxHeight}`);
    svg.setAttribute("role", "img");
    svg.setAttribute("aria-label", "Focused component dependency graph");

    const defs = document.createElementNS(svgNamespace, "defs");
    defs.append(
      createDependencyArrowMarker(svgNamespace, "parent-child-arrow", getDependencyEdgeColor("ParentChild")),
      createDependencyArrowMarker(svgNamespace, "parameter-flow-arrow", getDependencyEdgeColor("ParameterFlow")),
      createDependencyArrowMarker(svgNamespace, "cascading-dependency-arrow", getDependencyEdgeColor("CascadingDependency"))
    );
    svg.append(defs);

    const positionByNodeId = new Map<string, { x: number; y: number }>();
    for (const [index, graphNode] of incomingNodes.entries()) {
      positionByNodeId.set(graphNode.componentId, { x: leftX, y: calculateGraphNodeY(index, incomingNodes.length, viewBoxHeight, nodeHeight) });
    }

    positionByNodeId.set(selectedComponentId, { x: centerX, y: calculateGraphNodeY(0, 1, viewBoxHeight, nodeHeight) });

    for (const [index, graphNode] of outgoingNodes.entries()) {
      positionByNodeId.set(graphNode.componentId, { x: rightX, y: calculateGraphNodeY(index, outgoingNodes.length, viewBoxHeight, nodeHeight) });
    }

    const edgeLayer = document.createElementNS(svgNamespace, "g");
    edgeLayer.setAttribute("class", "dependency-graph-edges");
    const labelLayer = document.createElementNS(svgNamespace, "g");
    labelLayer.setAttribute("class", "dependency-graph-edge-labels");
    const nodeLayer = document.createElementNS(svgNamespace, "g");
    nodeLayer.setAttribute("class", "dependency-graph-nodes");

    function bindEdgeTooltip(
      element: SVGElement,
      edge: ComponentDependencyGraphEdgeSnapshot,
      tooltipAnchor: { svgX: number; svgY: number }
    ): void {
      element.addEventListener("pointerenter", () => showEdgeTooltip(edge, tooltipAnchor));
      element.addEventListener("pointerleave", scheduleHideEdgeTooltip);
      element.addEventListener("pointerdown", hideEdgeTooltip);
    }

    const edgeGroups = groupDependencyEdgesByPair([...focusedGraph.edges].sort((left, right) => {
      const orderDifference = (edgeTypeOrder.get(left.edgeType) ?? 99) - (edgeTypeOrder.get(right.edgeType) ?? 99);
      if (orderDifference !== 0) {
        return orderDifference;
      }

      if (left.isInferred !== right.isInferred) {
        return left.isInferred ? 1 : -1;
      }

      return left.summary.localeCompare(right.summary);
    }));

    for (const edgesForPair of edgeGroups.values()) {
      edgesForPair.forEach((edge, index) => {
        const sourcePosition = positionByNodeId.get(edge.sourceComponentId);
        const targetPosition = positionByNodeId.get(edge.targetComponentId);

        if (!sourcePosition || !targetPosition) {
          return;
        }

        const sourceX = sourcePosition.x + nodeWidth;
        const sourceY = sourcePosition.y + nodeHeight / 2 + getParallelEdgeOffset(index, edgesForPair.length);
        const targetX = targetPosition.x;
        const targetY = targetPosition.y + nodeHeight / 2 + getParallelEdgeOffset(index, edgesForPair.length);
        const controlX = (sourceX + targetX) / 2;
        const pathData = `M ${sourceX} ${sourceY} C ${controlX} ${sourceY} ${controlX} ${targetY} ${targetX} ${targetY}`;

        const path = document.createElementNS(svgNamespace, "path");
        path.setAttribute("class", `dependency-graph-edge dependency-graph-edge--${edge.edgeType} ${edge.isInferred ? "is-inferred" : "is-exact"}`);
        path.setAttribute("d", pathData);
        path.setAttribute("marker-end", `url(#${getDependencyMarkerId(edge.edgeType)})`);
        path.setAttribute("fill", "none");
        edgeLayer.append(path);

        const hitArea = document.createElementNS(svgNamespace, "path");
        hitArea.setAttribute("class", "dependency-graph-edge-hit-area");
        hitArea.setAttribute("d", pathData);
        edgeLayer.append(hitArea);

        const labelText = formatDependencyEdgeLabel(edge);
        const labelWidth = Math.min(220, Math.max(74, labelText.length * 7 + 26));
        const labelX = Math.max(12, Math.min(viewBoxWidth - labelWidth - 12, controlX - labelWidth / 2));
        const labelY = (sourceY + targetY) / 2 - 13;
        const tooltipAnchor = {
          svgX: labelX + labelWidth / 2,
          svgY: labelY + 13
        };

        const labelGroup = document.createElementNS(svgNamespace, "g");
        labelGroup.setAttribute("class", "dependency-graph-edge-label-group");

        const labelBackground = document.createElementNS(svgNamespace, "rect");
        labelBackground.setAttribute("class", `dependency-graph-edge-label-bg dependency-graph-edge-label-bg--${edge.edgeType} ${edge.isInferred ? "is-inferred" : "is-exact"}`);
        labelBackground.setAttribute("x", labelX.toString());
        labelBackground.setAttribute("y", labelY.toString());
        labelBackground.setAttribute("width", labelWidth.toString());
        labelBackground.setAttribute("height", "26");
        labelBackground.setAttribute("rx", "13");
        labelGroup.append(labelBackground);

        const label = document.createElementNS(svgNamespace, "text");
        label.setAttribute("class", `dependency-graph-edge-label dependency-graph-edge-label--${edge.edgeType}`);
        label.setAttribute("x", (labelX + labelWidth / 2).toString());
        label.setAttribute("y", (labelY + 17).toString());
        label.setAttribute("text-anchor", "middle");
        label.textContent = labelText;
        labelGroup.append(label);

        const tooltip = document.createElementNS(svgNamespace, "title");
        tooltip.textContent = buildDependencyEdgeTooltip(edge);
        path.append(tooltip.cloneNode(true));
        labelGroup.append(tooltip);
        bindEdgeTooltip(hitArea, edge, tooltipAnchor);
        bindEdgeTooltip(labelGroup, edge, tooltipAnchor);
        labelLayer.append(labelGroup);
      });
    }

    for (const graphNode of focusedGraph.nodes) {
      const position = positionByNodeId.get(graphNode.componentId);
      if (!position) {
        continue;
      }

      const button = document.createElementNS(svgNamespace, "g");
      button.setAttribute("class", `dependency-graph-node${graphNode.componentId === selectedComponentId ? " is-selected" : ""}`);
      button.setAttribute("transform", `translate(${position.x} ${position.y})`);
      button.setAttribute("tabindex", "0");
      button.setAttribute("role", "button");
      button.setAttribute("aria-label", `Inspect ${graphNode.name}`);

      const rect = document.createElementNS(svgNamespace, "rect");
      rect.setAttribute("class", "dependency-graph-node-rect");
      rect.setAttribute("width", nodeWidth.toString());
      rect.setAttribute("height", nodeHeight.toString());
      rect.setAttribute("rx", "14");
      rect.setAttribute("ry", "14");

      const nameText = document.createElementNS(svgNamespace, "text");
      nameText.setAttribute("class", "dependency-graph-node-name");
      nameText.setAttribute("x", "16");
      nameText.setAttribute("y", "31");
      nameText.textContent = truncateText(graphNode.name, 28);

      const typeText = document.createElementNS(svgNamespace, "text");
      typeText.setAttribute("class", "dependency-graph-node-type");
      typeText.setAttribute("x", "16");
      typeText.setAttribute("y", "55");
      typeText.textContent = truncateText(graphNode.fullTypeName, 36);

      const tooltip = document.createElementNS(svgNamespace, "title");
      tooltip.textContent = graphNode.fullTypeName;
      button.append(rect, nameText, typeText, tooltip);

      button.addEventListener("click", () => selectComponentInGraph(graphNode.componentId));
      button.addEventListener("keydown", (event) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          selectComponentInGraph(graphNode.componentId);
        }
      });

      nodeLayer.append(button);
    }

    svg.append(edgeLayer, labelLayer, nodeLayer);
    stage.append(svg);

    const updateZoomUi = (): void => {
      zoomValue.textContent = `${Math.round(zoomState.zoom * 100)}%`;
      zoomOutButton.disabled = zoomState.zoom <= minZoom + 0.001;
      zoomInButton.disabled = zoomState.zoom >= maxZoom - 0.001;
    };

    const updateVisibleEdgeTooltipPosition = (): void => {
      if (!activeTooltipAnchor || edgeTooltip.hidden) {
        return;
      }

      const tooltipWidth = edgeTooltip.offsetWidth;
      const tooltipHeight = edgeTooltip.offsetHeight;
      const viewportPadding = 10;
      const tooltipGap = 12;
      const anchorX = activeTooltipAnchor.svgX * zoomState.zoom;
      const anchorY = activeTooltipAnchor.svgY * zoomState.zoom;
      const visibleLeft = viewport.scrollLeft + viewportPadding;
      const visibleTop = viewport.scrollTop + viewportPadding;
      const visibleRight = viewport.scrollLeft + viewport.clientWidth - viewportPadding;
      const visibleBottom = viewport.scrollTop + viewport.clientHeight - viewportPadding;
      let horizontalPlacement: "left" | "right" = "right";
      let verticalPlacement: "above" | "below" = "above";
      let left = anchorX + tooltipGap;
      let top = anchorY - tooltipHeight - tooltipGap;

      if (left + tooltipWidth > visibleRight) {
        horizontalPlacement = "left";
        left = anchorX - tooltipWidth - tooltipGap;
      }

      if (top < visibleTop) {
        verticalPlacement = "below";
        top = anchorY + tooltipGap;
      }

      if (horizontalPlacement === "left" && left < visibleLeft && anchorX + tooltipGap + tooltipWidth <= visibleRight) {
        horizontalPlacement = "right";
        left = anchorX + tooltipGap;
      }

      if (verticalPlacement === "below" && top + tooltipHeight > visibleBottom && anchorY - tooltipHeight - tooltipGap >= visibleTop) {
        verticalPlacement = "above";
        top = anchorY - tooltipHeight - tooltipGap;
      }

      const maxLeft = Math.max(visibleLeft, visibleRight - tooltipWidth);
      const maxTop = Math.max(visibleTop, visibleBottom - tooltipHeight);
      left = Math.max(visibleLeft, Math.min(left, maxLeft));
      top = Math.max(visibleTop, Math.min(top, maxTop));

      edgeTooltip.style.left = `${Math.round(left)}px`;
      edgeTooltip.style.top = `${Math.round(top)}px`;
      edgeTooltip.dataset.horizontalPlacement = horizontalPlacement;
      edgeTooltip.dataset.verticalPlacement = verticalPlacement;
    };

    const applyZoom = (nextZoom: number): void => {
      zoomState.zoom = Math.max(minZoom, Math.min(maxZoom, nextZoom));
      stage.style.width = `${Math.round(viewBoxWidth * zoomState.zoom)}px`;
      stage.style.height = `${Math.round(viewBoxHeight * zoomState.zoom)}px`;
      updateZoomUi();
    };

    const setZoom = (nextZoom: number, anchorX: number, anchorY: number): void => {
      const previousZoom = zoomState.zoom;
      const clampedZoom = Math.max(minZoom, Math.min(maxZoom, nextZoom));
      if (Math.abs(clampedZoom - previousZoom) < 0.001) {
        updateZoomUi();
        return;
      }

      const contentAnchorX = (viewport.scrollLeft + anchorX) / previousZoom;
      const contentAnchorY = (viewport.scrollTop + anchorY) / previousZoom;

      applyZoom(clampedZoom);

      viewport.scrollLeft = Math.max(0, contentAnchorX * zoomState.zoom - anchorX);
      viewport.scrollTop = Math.max(0, contentAnchorY * zoomState.zoom - anchorY);
      updateVisibleEdgeTooltipPosition();
    };

    const zoomFromViewportCenter = (nextZoom: number): void => {
      setZoom(nextZoom, viewport.clientWidth / 2, viewport.clientHeight / 2);
    };

    const centerGraphInViewport = (): void => {
      viewport.scrollLeft = Math.max(0, (stage.scrollWidth - viewport.clientWidth) / 2);
      viewport.scrollTop = Math.max(0, (stage.scrollHeight - viewport.clientHeight) / 2);
      updateVisibleEdgeTooltipPosition();
    };

    const fitGraphToViewport = (): void => {
      const fitZoom = calculateDependencyGraphFitZoom(viewBoxWidth, viewBoxHeight, viewport.clientWidth, viewport.clientHeight, minZoom, maxZoom);
      zoomState.fitZoom = fitZoom;
      setZoom(fitZoom, viewport.clientWidth / 2, viewport.clientHeight / 2);
      centerGraphInViewport();
    };

    const resetGraphZoom = (): void => {
      setZoom(zoomState.defaultZoom, viewport.clientWidth / 2, viewport.clientHeight / 2);
      centerGraphInViewport();
    };

    function showEdgeTooltip(edge: ComponentDependencyGraphEdgeSnapshot, tooltipAnchor: { svgX: number; svgY: number }): void {
      if (hideTooltipTimeoutId !== null) {
        window.clearTimeout(hideTooltipTimeoutId);
        hideTooltipTimeoutId = null;
      }

      activeTooltipAnchor = tooltipAnchor;
      edgeTooltip.replaceChildren(createDependencyEdgeTooltipContent(edge));
      edgeTooltip.hidden = false;
      updateVisibleEdgeTooltipPosition();
    }

    function scheduleHideEdgeTooltip(): void {
      if (hideTooltipTimeoutId !== null) {
        window.clearTimeout(hideTooltipTimeoutId);
      }

      hideTooltipTimeoutId = window.setTimeout(() => {
        hideTooltipTimeoutId = null;
        hideEdgeTooltip();
      }, 48);
    }

    function hideEdgeTooltip(): void {
      if (hideTooltipTimeoutId !== null) {
        window.clearTimeout(hideTooltipTimeoutId);
        hideTooltipTimeoutId = null;
      }

      activeTooltipAnchor = null;
      edgeTooltip.hidden = true;
    }

    zoomOutButton.addEventListener("click", () => zoomFromViewportCenter(zoomState.zoom - zoomStep));
    zoomInButton.addEventListener("click", () => zoomFromViewportCenter(zoomState.zoom + zoomStep));
    resetButton.addEventListener("click", resetGraphZoom);
    fitButton.addEventListener("click", fitGraphToViewport);

    viewport.addEventListener("wheel", (event) => {
      if (!event.ctrlKey && !event.metaKey) {
        return;
      }

      event.preventDefault();
      const bounds = viewport.getBoundingClientRect();
      const anchorX = event.clientX - bounds.left;
      const anchorY = event.clientY - bounds.top;
      const direction = event.deltaY < 0 ? 1 : -1;
      setZoom(zoomState.zoom + direction * zoomStep, anchorX, anchorY);
    }, { passive: false });

    viewport.addEventListener("scroll", updateVisibleEdgeTooltipPosition, { passive: true });

    viewport.addEventListener("pointerdown", (event) => {
      if (event.button !== 0) {
        return;
      }

      if (event.target instanceof Element && event.target.closest(".dependency-graph-node")) {
        return;
      }

      isPanning = true;
      activePointerId = event.pointerId;
      panStartX = event.clientX;
      panStartY = event.clientY;
      panStartScrollLeft = viewport.scrollLeft;
      panStartScrollTop = viewport.scrollTop;
      hideEdgeTooltip();
      viewport.classList.add("is-panning");
      viewport.setPointerCapture(event.pointerId);
      event.preventDefault();
    });

    const stopPanning = (event: PointerEvent): void => {
      if (!isPanning || activePointerId !== event.pointerId) {
        return;
      }

      isPanning = false;
      activePointerId = null;
      viewport.classList.remove("is-panning");
      if (viewport.hasPointerCapture(event.pointerId)) {
        viewport.releasePointerCapture(event.pointerId);
      }
    };

    viewport.addEventListener("pointermove", (event) => {
      if (!isPanning || activePointerId !== event.pointerId) {
        return;
      }

      viewport.scrollLeft = panStartScrollLeft - (event.clientX - panStartX);
      viewport.scrollTop = panStartScrollTop - (event.clientY - panStartY);
    });

    viewport.addEventListener("pointerup", stopPanning);
    viewport.addEventListener("pointercancel", stopPanning);
    viewport.addEventListener("pointerleave", (event) => {
      if (isPanning) {
        return;
      }

      if (event.relatedTarget instanceof Node && viewport.contains(event.relatedTarget)) {
        return;
      }

      hideEdgeTooltip();
    });

    requestAnimationFrame(() => {
      zoomState.fitZoom = calculateDependencyGraphFitZoom(viewBoxWidth, viewBoxHeight, viewport.clientWidth, viewport.clientHeight, minZoom, maxZoom);
      zoomState.defaultZoom = Math.max(1, zoomState.fitZoom);
      applyZoom(zoomState.defaultZoom);
      centerGraphInViewport();
    });

    return wrapper;
  }

  function selectComponentInGraph(componentId: string): void {
    if (!findNodeById(currentTree, componentId)) {
      return;
    }

    selectedNodeId = componentId;
    selectedRenderSequence = null;
    expandPathToNode(currentTree, componentId, expandedNodeIds);
    render();
  }

  function buildFocusedDependencyGraph(selectedComponentId: string, graph: ComponentDependencyGraphSnapshot): FocusedDependencyGraph {
    const connectedEdges = graph.edges.filter(
      (edge) => edge.sourceComponentId === selectedComponentId || edge.targetComponentId === selectedComponentId
    );
    const nodeIds = new Set<string>([selectedComponentId]);

    for (const edge of connectedEdges) {
      nodeIds.add(edge.sourceComponentId);
      nodeIds.add(edge.targetComponentId);
    }

    return {
      nodes: graph.nodes.filter((node) => nodeIds.has(node.componentId)),
      edges: connectedEdges
    };
  }

  function getOrderedGraphNeighbors(
    graph: FocusedDependencyGraph,
    selectedComponentId: string,
    direction: "incoming" | "outgoing"
  ): ComponentDependencyGraphNodeSnapshot[] {
    const nodeIds = new Set(
      graph.edges
        .filter((edge) => direction === "incoming"
          ? edge.targetComponentId === selectedComponentId && edge.sourceComponentId !== selectedComponentId
          : edge.sourceComponentId === selectedComponentId && edge.targetComponentId !== selectedComponentId)
        .map((edge) => direction === "incoming" ? edge.sourceComponentId : edge.targetComponentId)
    );

    return graph.nodes
      .filter((node) => nodeIds.has(node.componentId))
      .sort((left, right) => left.name.localeCompare(right.name));
  }

  function groupDependencyEdgesByPair(edges: ComponentDependencyGraphEdgeSnapshot[]): Map<string, ComponentDependencyGraphEdgeSnapshot[]> {
    const groups = new Map<string, ComponentDependencyGraphEdgeSnapshot[]>();

    for (const edge of edges) {
      const key = `${edge.sourceComponentId}->${edge.targetComponentId}`;
      const existing = groups.get(key);
      if (existing) {
        existing.push(edge);
      } else {
        groups.set(key, [edge]);
      }
    }

    return groups;
  }

  function createDependencyArrowMarker(svgNamespace: string, markerId: string, color: string): SVGMarkerElement {
    const marker = document.createElementNS(svgNamespace, "marker") as SVGMarkerElement;
    marker.setAttribute("id", markerId);
    marker.setAttribute("markerWidth", "8");
    marker.setAttribute("markerHeight", "8");
    marker.setAttribute("refX", "7");
    marker.setAttribute("refY", "4");
    marker.setAttribute("orient", "auto");

    const path = document.createElementNS(svgNamespace, "path");
    path.setAttribute("d", "M 0 0 L 8 4 L 0 8 z");
    path.setAttribute("fill", color);
    marker.append(path);
    return marker;
  }

  function calculateGraphNodeY(index: number, count: number, totalHeight: number, nodeHeight: number): number {
    if (count <= 1) {
      return (totalHeight - nodeHeight) / 2;
    }

    const availableHeight = totalHeight - nodeHeight - 48;
    return 24 + (availableHeight / (count - 1)) * index;
  }

  function getParallelEdgeOffset(index: number, edgeCount: number): number {
    return (index - (edgeCount - 1) / 2) * 12;
  }

  function formatDependencyEdgeLabel(edge: ComponentDependencyGraphEdgeSnapshot): string {
    if (edge.edgeType === "ParentChild") {
      return edge.summary;
    }

    const relatedValueLabel = edge.relatedValues.length > 0 ? `: ${truncateText(edge.relatedValues.join(", "), 18)}` : "";
    return `${edge.summary}${relatedValueLabel}`;
  }

  function buildDependencyEdgeTooltip(edge: ComponentDependencyGraphEdgeSnapshot): string {
    const parts = [edge.summary, `${formatDependencyEdgeType(edge.edgeType)} · ${edge.isInferred ? "Inferred" : "Exact"}`];

    if (edge.relatedValues.length > 0) {
      parts.push(`Values: ${edge.relatedValues.join(", ")}`);
    }

    if (edge.details) {
      parts.push(edge.details);
    }

    return parts.join("\n");
  }

  function getDependencyEdgeColor(edgeType: string): string {
    switch (edgeType) {
      case "ParentChild":
        return "#3b82f6";
      case "ParameterFlow":
        return "#16a34a";
      case "CascadingDependency":
        return "#7c3aed";
      default:
        return "#94a3b8";
    }
  }

  function getDependencyMarkerId(edgeType: string): string {
    switch (edgeType) {
      case "ParentChild":
        return "parent-child-arrow";
      case "ParameterFlow":
        return "parameter-flow-arrow";
      case "CascadingDependency":
        return "cascading-dependency-arrow";
      default:
        return "parent-child-arrow";
    }
  }

  function createDependencyGraphControlButton(labelText: string, ariaLabel: string): HTMLButtonElement {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "dependency-graph-control-button";
    button.textContent = labelText;
    button.setAttribute("aria-label", ariaLabel);
    return button;
  }

  function calculateDependencyGraphFitZoom(
    graphWidth: number,
    graphHeight: number,
    viewportWidth: number,
    viewportHeight: number,
    minZoom: number,
    maxZoom: number
  ): number {
    if (viewportWidth <= 0 || viewportHeight <= 0) {
      return 1;
    }

    const paddedWidth = Math.max(0, viewportWidth - 40);
    const paddedHeight = Math.max(0, viewportHeight - 40);
    const zoom = Math.min(paddedWidth / graphWidth, paddedHeight / graphHeight);
    return Math.max(minZoom, Math.min(maxZoom, zoom));
  }

  function createDependencyEdgeTooltipContent(edge: ComponentDependencyGraphEdgeSnapshot): HTMLDivElement {
    const tooltip = document.createElement("div");
    tooltip.className = "dependency-graph-tooltip-content";

    const title = document.createElement("strong");
    title.className = "dependency-graph-tooltip-title";
    title.textContent = edge.summary;
    tooltip.append(title);

    const meta = document.createElement("div");
    meta.className = "dependency-graph-tooltip-meta";
    meta.textContent = `${formatDependencyEdgeType(edge.edgeType)} · ${edge.isInferred ? "Inferred" : "Exact"}`;
    tooltip.append(meta);

    if (edge.relatedValues.length > 0) {
      const values = document.createElement("div");
      values.className = "dependency-graph-tooltip-detail";
      values.textContent = `Values: ${edge.relatedValues.join(", ")}`;
      tooltip.append(values);
    }

    if (edge.details) {
      const details = document.createElement("div");
      details.className = "dependency-graph-tooltip-detail";
      details.textContent = edge.details;
      tooltip.append(details);
    }

    return tooltip;
  }

  function formatDependencyEdgeType(edgeType: string): string {
    switch (edgeType) {
      case "ParentChild":
        return "Parent/child";
      case "ParameterFlow":
        return "Parameter flow";
      case "CascadingDependency":
        return "Cascading dependency";
      default:
        return edgeType;
    }
  }

  function truncateText(text: string, maxLength: number): string {
    return text.length <= maxLength ? text : `${text.slice(0, Math.max(0, maxLength - 1))}…`;
  }

  function renderParameters(parameters: ComponentParameterSnapshot[]): HTMLDivElement {
    const section = document.createElement("div");
    section.className = "parameters-section";

    const title = document.createElement("h4");
    title.className = "parameters-title";
    title.textContent = "Parameters";
    section.append(title);

    if (parameters.length === 0) {
      section.append(createEmptyState("This component does not expose tracked parameters."));
      return section;
    }

    const list = document.createElement("ul");
    list.className = "parameter-list";

    for (const parameter of parameters) {
      const item = document.createElement("li");
      item.className = "parameter-item";

      const name = document.createElement("span");
      name.className = "parameter-name";
      name.textContent = parameter.name;

      const value = document.createElement("p");
      value.className = "parameter-value";
      const code = document.createElement("code");
      code.textContent = parameter.value ?? "null";
      value.append(code);

      item.append(name, value);
      list.append(item);
    }

    section.append(list);
    return section;
  }

  function renderInjectedServices(injectedServices: ComponentInjectedServiceSnapshot[]): HTMLDivElement {
    const section = document.createElement("div");
    section.className = "services-section";

    const title = document.createElement("h4");
    title.className = "services-title";
    title.textContent = "Injected Services";
    section.append(title);

    if (injectedServices.length === 0) {
      section.append(createEmptyState("This component does not expose tracked injected services."));
      return section;
    }

    const list = document.createElement("ul");
    list.className = "service-list";

    for (const service of injectedServices) {
      const item = document.createElement("li");
      item.className = "service-item";

      const name = document.createElement("span");
      name.className = "service-name";
      name.textContent = service.propertyName;

      const type = document.createElement("p");
      type.className = "service-type";

      const code = document.createElement("code");
      code.textContent = service.fullServiceTypeName;
      type.append(code);

      item.append(name, type);
      list.append(item);
    }

    section.append(list);
    return section;
  }

  function renderCascadingParameters(cascadingParameters: ComponentCascadingParameterSnapshot[]): HTMLDivElement {
    const section = document.createElement("div");
    section.className = "cascading-section";

    const title = document.createElement("h4");
    title.className = "cascading-title";
    title.textContent = "Cascading Values";
    section.append(title);

    if (cascadingParameters.length === 0) {
      section.append(createEmptyState("This component does not expose tracked cascading parameters."));
      return section;
    }

    const list = document.createElement("ul");
    list.className = "cascading-list";

    for (const cascadingParameter of cascadingParameters) {
      const item = document.createElement("li");
      item.className = "cascading-item";

      const name = document.createElement("span");
      name.className = "cascading-name";
      name.textContent = cascadingParameter.providerHint
        ? `${cascadingParameter.propertyName} (Name: ${cascadingParameter.providerHint})`
        : cascadingParameter.propertyName;

      const type = document.createElement("p");
      type.className = "cascading-type";
      const code = document.createElement("code");
      code.textContent = cascadingParameter.fullValueTypeName;
      type.append(code);

      item.append(name, type);
      list.append(item);
    }

    section.append(list);
    return section;
  }

  function renderLifecycleMetrics(lifecycleMetrics: ComponentLifecycleMetricsSnapshot | null): HTMLDivElement {
    const section = document.createElement("div");
    section.className = "metrics-section";

    const title = document.createElement("h4");
    title.className = "metrics-title";
    title.textContent = "Lifecycle Metrics";
    section.append(title);

    if (!lifecycleMetrics) {
      section.append(createEmptyState("Lifecycle metrics are not available for this component."));
      return section;
    }

    const grid = document.createElement("div");
    grid.className = "metrics-grid";
    grid.append(
      createMetricCard("Time to first render", formatDuration(lifecycleMetrics.timeToFirstRenderMs), {
        severity: getSeverityFromDuration(lifecycleMetrics.timeToFirstRenderMs, firstRenderDurationThresholds),
        emphasizeLabel: true
      }),
      createMetricCard("Render count", lifecycleMetrics.renderCount.toString(), {
        severity: getSeverityFromCount(lifecycleMetrics.renderCount, renderCountThresholds),
        emphasizeLabel: true
      }),
      createMetricCard("Avg render time", formatDuration(lifecycleMetrics.averageRenderTimeMs), {
        severity: getSeverityFromDuration(lifecycleMetrics.averageRenderTimeMs, averageRenderDurationThresholds),
        emphasizeLabel: true
      }),
      createMetricCard("StateHasChanged count", `${lifecycleMetrics.stateHasChangedCount} (approx.)`, {
        severity: getSeverityFromCount(lifecycleMetrics.stateHasChangedCount, stateHasChangedCountThresholds),
        emphasizeLabel: true
      }),
      createMetricCard("OnInitialized", formatDuration(lifecycleMetrics.onInitializedTimeMs), {
        severity: getSeverityFromDuration(lifecycleMetrics.onInitializedTimeMs, standardLifecycleDurationThresholds),
        emphasizeLabel: true
      }),
      createMetricCard("OnInitializedAsync", formatDuration(lifecycleMetrics.onInitializedAsyncTimeMs), {
        severity: getSeverityFromDuration(lifecycleMetrics.onInitializedAsyncTimeMs, asyncLifecycleDurationThresholds),
        emphasizeLabel: true
      }),
      createMetricCard("OnParametersSet", formatDuration(lifecycleMetrics.onParametersSetTimeMs), {
        severity: getSeverityFromDuration(lifecycleMetrics.onParametersSetTimeMs, standardLifecycleDurationThresholds),
        emphasizeLabel: true
      }),
      createMetricCard("OnAfterRender", formatDuration(lifecycleMetrics.onAfterRenderTimeMs), {
        severity: getSeverityFromDuration(lifecycleMetrics.onAfterRenderTimeMs, standardLifecycleDurationThresholds),
        emphasizeLabel: true
      }),
      createMetricCard("Total render time", formatDuration(lifecycleMetrics.totalRenderTimeMs), {
        severity: getSeverityFromDuration(lifecycleMetrics.totalRenderTimeMs, totalRenderDurationThresholds),
        emphasizeLabel: true
      })
    );

    section.append(grid);
    return section;
  }

  function renderWhyDidThisRender(renderInfo: ComponentRenderInfoSnapshot | null): HTMLDivElement {
    const section = document.createElement("div");
    section.className = "render-info-section";

    const title = document.createElement("h4");
    title.className = "render-info-title";
    title.textContent = "Why Did This Render?";
    section.append(title);

    if (!renderInfo || !renderInfo.latestRenderCause) {
      section.append(createEmptyState("No recent render-cause information is available for this component."));
      return section;
    }

    section.append(createMetricCard("Latest known cause", formatRenderCause(renderInfo.latestRenderCause), {
      severity: "neutral",
      emphasizeLabel: true
    }));

    if (renderInfo.recentRenderCauses.length > 0) {
      const list = document.createElement("ul");
      list.className = "render-cause-list";

      for (const cause of [...renderInfo.recentRenderCauses].reverse()) {
        const item = document.createElement("li");
        item.className = "render-cause-item";

        const name = document.createElement("span");
        name.className = "render-cause-name";
        name.textContent = `#${cause.renderSequence} ${cause.cause}${cause.isApproximate ? " (approx.)" : ""}`;

        const details = document.createElement("p");
        details.className = "render-cause-details";
        const code = document.createElement("code");
        code.textContent = cause.details ?? "No additional details";
        details.append(code);

        item.append(name, details);
        list.append(item);
      }

      section.append(list);
    }

    return section;
  }

  function renderRenderDiff(renderDiffInfo: ComponentRenderDiffInfoSnapshot | null, renderInfo: ComponentRenderInfoSnapshot | null): HTMLDivElement {
    const section = document.createElement("div");
    section.className = "render-diff-section";

    const title = document.createElement("h4");
    title.className = "render-diff-title";
    title.textContent = "Render Diff";
    section.append(title);

    const note = document.createElement("p");
    note.className = "render-diff-note";
    note.textContent = "This MVP tracks parameter changes between renders. Arbitrary component-local state is not currently diffed.";
    section.append(note);

    const latestRenderDiff = renderDiffInfo?.latestRenderDiff ?? null;
    if (!latestRenderDiff) {
      section.append(createEmptyState("No render diff is available for this component yet."));
      return section;
    }

    const renderCauseBySequence = createRenderCauseLookup(renderInfo);

    const renderHistory = getRenderDiffHistory(renderDiffInfo);
    const visibleRenderDiffs = showOnlyChangedRenders
      ? renderHistory.filter(diff => diff.hasPreviousSnapshot && diff.parameterChanges.length > 0)
      : renderHistory;
    const selectedRenderDiff = getSelectedRenderDiff(visibleRenderDiffs, latestRenderDiff, selectedRenderSequence);
    selectedRenderSequence = selectedRenderDiff?.renderSequence ?? null;

    const historyHeader = document.createElement("div");
    historyHeader.className = "render-diff-history-header";

    const historyTitle = document.createElement("h5");
    historyTitle.className = "render-diff-history-title";
    historyTitle.textContent = "Recent render history";
    historyHeader.append(historyTitle);

    const historyControls = document.createElement("div");
    historyControls.className = "render-diff-history-controls";

    const filterLabel = document.createElement("label");
    filterLabel.className = "render-diff-filter-toggle";

    const filterInput = document.createElement("input");
    filterInput.type = "checkbox";
    filterInput.checked = showOnlyChangedRenders;
    filterInput.addEventListener("change", () => {
      showOnlyChangedRenders = filterInput.checked;
      render();
    });

    const filterText = document.createElement("span");
    filterText.textContent = "Show only renders with changes";

    filterLabel.append(filterInput, filterText);
    historyControls.append(filterLabel);

    if (renderHistory.length > 1) {
      const collapseButton = document.createElement("button");
      collapseButton.type = "button";
      collapseButton.className = "render-diff-history-toggle";
      collapseButton.textContent = renderHistoryCollapsed ? "Show history" : "Hide history";
      collapseButton.addEventListener("click", () => {
        renderHistoryCollapsed = !renderHistoryCollapsed;
        render();
      });
      historyControls.append(collapseButton);
    }

    historyHeader.append(historyControls);
    section.append(historyHeader);

    if (visibleRenderDiffs.length === 0) {
      section.append(createEmptyState(showOnlyChangedRenders
        ? "No tracked renders with parameter changes match the current filter."
        : "No render history is available for this component yet."));
    } else if (!renderHistoryCollapsed) {
      const historyList = document.createElement("ul");
      historyList.className = "render-diff-history-list";

      for (const diff of visibleRenderDiffs) {
        const item = document.createElement("li");
        item.className = "render-diff-history-item";

        const summary = document.createElement("button");
        summary.type = "button";
        summary.className = "render-diff-history-summary";
        if (selectedRenderDiff?.renderSequence === diff.renderSequence) {
          summary.classList.add("is-selected");
        }

        summary.setAttribute("aria-pressed", selectedRenderDiff?.renderSequence === diff.renderSequence ? "true" : "false");
        summary.title = formatRenderHistorySummary(diff, renderCauseBySequence.get(diff.renderSequence) ?? null);
        summary.append(createRenderHistorySummaryContent(diff, renderCauseBySequence.get(diff.renderSequence) ?? null));
        summary.addEventListener("click", () => {
          selectedRenderSequence = diff.renderSequence;
          render();
        });

        item.append(summary);
        historyList.append(item);
      }

      section.append(historyList);
    }

    section.append(renderSelectedRenderDetails(selectedRenderDiff, renderCauseBySequence));

    return section;
  }

  function renderSelectedRenderDetails(
    selectedRenderDiff: ComponentRenderDiffSnapshot | null,
    renderCauseBySequence: Map<number, ComponentRenderCauseSnapshot>
  ): HTMLDivElement {
    const wrapper = document.createElement("div");
    wrapper.className = "render-diff-selected-details";

    const title = document.createElement("h5");
    title.className = "render-diff-selected-title";
    title.textContent = "Selected render details";
    wrapper.append(title);

    if (!selectedRenderDiff) {
      wrapper.append(createEmptyState(showOnlyChangedRenders
        ? "Select a visible render row or clear the filter to inspect another tracked render."
        : "Select a render row to inspect its tracked diff details."));
      return wrapper;
    }

    const renderCause = renderCauseBySequence.get(selectedRenderDiff.renderSequence) ?? null;
    const detailsGrid = document.createElement("div");
    detailsGrid.className = "details-grid render-diff-selected-grid";
    detailsGrid.append(
      createDetailCard("Render sequence", `#${selectedRenderDiff.renderSequence}`),
      createDetailCard("Timestamp", formatRenderTimestamp(selectedRenderDiff.recordedAt)),
      createDetailCard("Render cause", renderCause ? formatRenderCause(renderCause) : "Cause unavailable")
    );
    wrapper.append(detailsGrid);

    if (renderCause?.details) {
      const causeDetails = document.createElement("p");
      causeDetails.className = "render-diff-selected-cause-details";
      causeDetails.textContent = renderCause.details;
      wrapper.append(causeDetails);
    }

    const parameterChangesTitle = document.createElement("h6");
    parameterChangesTitle.className = "render-diff-selected-subtitle";
    parameterChangesTitle.textContent = "Parameter changes";
    wrapper.append(parameterChangesTitle);

    if (!selectedRenderDiff.hasPreviousSnapshot) {
      wrapper.append(createEmptyState("This is the first tracked render. Diffing begins on the next render."));
      return wrapper;
    }

    if (selectedRenderDiff.parameterChanges.length === 0) {
      wrapper.append(createEmptyState("No tracked parameter changes were observed for this render."));
      return wrapper;
    }

    wrapper.append(createRenderDiffDetails(selectedRenderDiff));
    return wrapper;
  }

  function createDetailCard(labelText: string, valueText: string): HTMLDivElement {
    const card = document.createElement("div");
    card.className = "detail-card";

    const label = document.createElement("span");
    label.className = "detail-label";
    label.textContent = labelText;

    const value = document.createElement("p");
    value.className = "detail-value";

    const code = document.createElement("code");
    code.textContent = valueText;
    value.append(code);

    card.append(label, value);
    return card;
  }

  function getRenderDiffHistory(renderDiffInfo: ComponentRenderDiffInfoSnapshot | null): ComponentRenderDiffSnapshot[] {
    if (!renderDiffInfo?.latestRenderDiff) {
      return [];
    }

    const bySequence = new Map<number, ComponentRenderDiffSnapshot>();
    bySequence.set(renderDiffInfo.latestRenderDiff.renderSequence, renderDiffInfo.latestRenderDiff);

    for (const diff of renderDiffInfo.recentRenderDiffs) {
      bySequence.set(diff.renderSequence, diff);
    }

    return [...bySequence.values()].sort((left, right) => right.renderSequence - left.renderSequence);
  }

  function getSelectedRenderDiff(
    visibleRenderDiffs: ComponentRenderDiffSnapshot[],
    latestRenderDiff: ComponentRenderDiffSnapshot,
    preferredRenderSequence: number | null
  ): ComponentRenderDiffSnapshot | null {
    if (visibleRenderDiffs.length === 0) {
      return null;
    }

    if (preferredRenderSequence !== null) {
      const preferredMatch = visibleRenderDiffs.find(diff => diff.renderSequence === preferredRenderSequence);
      if (preferredMatch) {
        return preferredMatch;
      }
    }

    const latestVisibleMatch = visibleRenderDiffs.find(diff => diff.renderSequence === latestRenderDiff.renderSequence);
    return latestVisibleMatch ?? visibleRenderDiffs[0] ?? null;
  }

  function createMetricCard(labelText: string, valueText: string, options: MetricCardOptions = {}): HTMLDivElement {
    const severity = options.severity ?? "neutral";
    const card = document.createElement("div");
    card.className = "metric-card";
    card.dataset.severity = severity;

    const label = document.createElement("span");
    label.className = "metric-label";
    if (options.emphasizeLabel) {
      label.classList.add("metric-label--performance");
    }
    label.textContent = labelText;

    const value = document.createElement("p");
    value.className = "metric-value";
    value.classList.add(`metric-value--${severity}`);
    const code = document.createElement("code");
    code.textContent = valueText;
    value.append(code);

    card.append(label, value);
    return card;
  }

  function createSummaryCard(labelText: string, valueText: string, options: MetricCardOptions = {}): HTMLDivElement {
    const severity = options.severity ?? "neutral";
    const card = document.createElement("div");
    card.className = "summary-card";
    card.dataset.severity = severity;

    const label = document.createElement("span");
    label.className = "summary-label";
    if (options.emphasizeLabel) {
      label.classList.add("summary-label--performance");
    }
    label.textContent = labelText;

    const value = document.createElement("p");
    value.className = "summary-value";
    value.classList.add(`summary-value--${severity}`);
    value.textContent = valueText;

    card.append(label, value);
    return card;
  }

  function createOverviewItem(labelText: string, valueText: string): HTMLDivElement {
    const item = document.createElement("div");
    item.className = "overview-item";

    const label = document.createElement("span");
    label.className = "overview-label";
    label.textContent = labelText;

    const value = document.createElement("p");
    value.className = "overview-value";

    const code = document.createElement("code");
    code.textContent = valueText;
    value.append(code);

    item.append(label, value);
    return item;
  }

function formatRenderCause(cause: ComponentRenderCauseSnapshot): string {
  return `${cause.cause}${cause.isApproximate ? " (approx.)" : ""}`;
}

function createRenderCauseLookup(renderInfo: ComponentRenderInfoSnapshot | null): Map<number, ComponentRenderCauseSnapshot> {
  const lookup = new Map<number, ComponentRenderCauseSnapshot>();

  if (!renderInfo) {
    return lookup;
  }

  if (renderInfo.latestRenderCause) {
    lookup.set(renderInfo.latestRenderCause.renderSequence, renderInfo.latestRenderCause);
  }

  for (const cause of renderInfo.recentRenderCauses) {
    lookup.set(cause.renderSequence, cause);
  }

  return lookup;
}

function formatRenderHistorySummary(diff: ComponentRenderDiffSnapshot, cause: ComponentRenderCauseSnapshot | null): string {
  return `#${diff.renderSequence} · ${formatRenderTimestamp(diff.recordedAt)} · ${cause ? formatRenderCause(cause) : "Cause unavailable"} · ${formatRenderDiffChangeSummary(diff)}`;
}

function createRenderHistorySummaryContent(diff: ComponentRenderDiffSnapshot, cause: ComponentRenderCauseSnapshot | null): DocumentFragment {
  const fragment = document.createDocumentFragment();
  fragment.append(
    createRenderHistoryPart(`#${diff.renderSequence}`, "render-diff-history-sequence"),
    createRenderHistorySeparator(),
    createRenderHistoryPart(formatRenderTimestamp(diff.recordedAt), "render-diff-history-timestamp"),
    createRenderHistorySeparator(),
    createRenderHistoryPart(cause ? formatRenderCause(cause) : "Cause unavailable", "render-diff-history-cause"),
    createRenderHistorySeparator(),
    createRenderHistoryPart(formatRenderDiffChangeSummary(diff), "render-diff-history-change-summary")
  );
  return fragment;
}

function createRenderHistoryPart(text: string, className?: string): HTMLSpanElement {
  const span = document.createElement("span");
  span.className = className ?? "render-diff-history-part";
  span.textContent = text;
  return span;
}

function createRenderHistorySeparator(): HTMLSpanElement {
  const separator = document.createElement("span");
  separator.className = "render-diff-history-separator";
  separator.textContent = "·";
  return separator;
}

function formatRenderTimestamp(timestamp: string): string {
  const date = new Date(timestamp);
  if (Number.isNaN(date.getTime())) {
    return "time unavailable";
  }

  return new Intl.DateTimeFormat(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    fractionalSecondDigits: 3,
    hour12: false
  }).format(date);
}

function formatRenderDiffChangeSummary(diff: ComponentRenderDiffSnapshot): string {
  if (!diff.hasPreviousSnapshot) {
    return "first tracked render";
  }

  if (diff.parameterChanges.length === 0) {
    return "no parameter changes";
  }

  return `${diff.parameterChanges.length} change${diff.parameterChanges.length === 1 ? "" : "s"}`;
}

function createRenderDiffDetails(diff: ComponentRenderDiffSnapshot): HTMLElement {
  if (!diff.hasPreviousSnapshot) {
    return createEmptyState("First tracked render - no previous snapshot.");
  }

  if (diff.parameterChanges.length === 0) {
    return createEmptyState("No tracked parameter changes were observed for this render.");
  }

  const list = document.createElement("ul");
  list.className = "render-diff-list";

  for (const parameterChange of diff.parameterChanges) {
    const item = document.createElement("li");
    item.className = "render-diff-item";

    const name = document.createElement("span");
    name.className = "render-diff-name";
    name.textContent = parameterChange.name;

    const values = document.createElement("div");
    values.className = "render-diff-values";

    const prevCode = document.createElement("code");
    prevCode.className = "render-diff-prev";
    prevCode.textContent = parameterChange.previousValue ?? "null";
    prevCode.title = parameterChange.previousValue ?? "null";

    const arrow = document.createElement("span");
    arrow.className = "render-diff-arrow";
    arrow.textContent = "→";

    const currCode = document.createElement("code");
    currCode.className = "render-diff-current";
    currCode.textContent = parameterChange.currentValue ?? "null";
    currCode.title = parameterChange.currentValue ?? "null";

    values.append(prevCode, arrow, currCode);
    item.append(name, values);
    list.append(item);
  }

  return list;
}

function createEmptyState(text: string): HTMLParagraphElement {
  const emptyState = document.createElement("p");
  emptyState.className = "empty-state";
  emptyState.textContent = text;
  return emptyState;
}
});

function extractSnapshot(payload: unknown): ComponentTreeSnapshot | null {
  if (!isEnvelope(payload)) {
    return null;
  }

  return isSnapshot(payload.payload) ? payload.payload : null;
}

function isEnvelope(payload: unknown): payload is DevToolsMessageEnvelope {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.source === "string" &&
    typeof candidate.messageType === "string" &&
    typeof candidate.payload === "object" &&
    candidate.payload !== null
  );
}

function isSnapshot(payload: unknown): payload is ComponentTreeSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.capturedAt === "string" &&
    Array.isArray(candidate.roots) &&
    candidate.roots.every(isComponentNode) &&
    isDependencyGraph(candidate.dependencyGraph)
  );
}

function isComponentNode(payload: unknown): payload is ComponentNode {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.id === "string" &&
    typeof candidate.name === "string" &&
    typeof candidate.fullTypeName === "string" &&
    typeof candidate.assemblyName === "string" &&
    (candidate.domMarkerId === undefined || candidate.domMarkerId === null || typeof candidate.domMarkerId === "string") &&
    (candidate.renderCount === undefined || candidate.renderCount === null || typeof candidate.renderCount === "number") &&
    (candidate.parameters === undefined || (Array.isArray(candidate.parameters) && candidate.parameters.every(isComponentParameter))) &&
    (candidate.injectedServices === undefined || (Array.isArray(candidate.injectedServices) && candidate.injectedServices.every(isInjectedService))) &&
    (candidate.cascadingParameters === undefined || (Array.isArray(candidate.cascadingParameters) && candidate.cascadingParameters.every(isCascadingParameter))) &&
    (candidate.lifecycleMetrics === undefined || candidate.lifecycleMetrics === null || isLifecycleMetrics(candidate.lifecycleMetrics)) &&
    (candidate.renderInfo === undefined || candidate.renderInfo === null || isRenderInfo(candidate.renderInfo)) &&
    (candidate.renderDiffInfo === undefined || candidate.renderDiffInfo === null || isRenderDiffInfo(candidate.renderDiffInfo)) &&
    Array.isArray(candidate.children) &&
    candidate.children.every(isComponentNode)
  );
}

function isComponentParameter(payload: unknown): payload is ComponentParameterSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return typeof candidate.name === "string" && (candidate.value === null || typeof candidate.value === "string");
}

function isInjectedService(payload: unknown): payload is ComponentInjectedServiceSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.propertyName === "string" &&
    typeof candidate.serviceTypeName === "string" &&
    typeof candidate.fullServiceTypeName === "string"
  );
}

function isLifecycleMetrics(payload: unknown): payload is ComponentLifecycleMetricsSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    (candidate.timeToFirstRenderMs === null || typeof candidate.timeToFirstRenderMs === "number") &&
    typeof candidate.renderCount === "number" &&
    (candidate.averageRenderTimeMs === null || typeof candidate.averageRenderTimeMs === "number") &&
    typeof candidate.stateHasChangedCount === "number" &&
    (candidate.onInitializedTimeMs === null || typeof candidate.onInitializedTimeMs === "number") &&
    (candidate.onInitializedAsyncTimeMs === null || typeof candidate.onInitializedAsyncTimeMs === "number") &&
    (candidate.onParametersSetTimeMs === null || typeof candidate.onParametersSetTimeMs === "number") &&
    (candidate.onAfterRenderTimeMs === null || typeof candidate.onAfterRenderTimeMs === "number") &&
    typeof candidate.totalRenderTimeMs === "number"
  );
}

function isCascadingParameter(payload: unknown): payload is ComponentCascadingParameterSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.propertyName === "string" &&
    typeof candidate.valueTypeName === "string" &&
    typeof candidate.fullValueTypeName === "string" &&
    (candidate.providerHint === null || typeof candidate.providerHint === "string")
  );
}

function isRenderCause(payload: unknown): payload is ComponentRenderCauseSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.renderSequence === "number" &&
    typeof candidate.cause === "string" &&
    typeof candidate.isApproximate === "boolean" &&
    (candidate.details === null || typeof candidate.details === "string")
  );
}

function isRenderInfo(payload: unknown): payload is ComponentRenderInfoSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    (candidate.latestRenderCause === null || isRenderCause(candidate.latestRenderCause)) &&
    Array.isArray(candidate.recentRenderCauses) &&
    candidate.recentRenderCauses.every(isRenderCause)
  );
}

function isParameterDiff(payload: unknown): payload is ComponentParameterDiffSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.name === "string" &&
    (candidate.previousValue === null || typeof candidate.previousValue === "string") &&
    (candidate.currentValue === null || typeof candidate.currentValue === "string")
  );
}

function isRenderDiff(payload: unknown): payload is ComponentRenderDiffSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.renderSequence === "number" &&
    typeof candidate.recordedAt === "string" &&
    typeof candidate.hasPreviousSnapshot === "boolean" &&
    Array.isArray(candidate.parameterChanges) &&
    candidate.parameterChanges.every(isParameterDiff)
  );
}

function isRenderDiffInfo(payload: unknown): payload is ComponentRenderDiffInfoSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    (candidate.latestRenderDiff === null || isRenderDiff(candidate.latestRenderDiff)) &&
    Array.isArray(candidate.recentRenderDiffs) &&
    candidate.recentRenderDiffs.every(isRenderDiff)
  );
}

function isDependencyGraphNode(payload: unknown): payload is ComponentDependencyGraphNodeSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.componentId === "string" &&
    typeof candidate.name === "string" &&
    typeof candidate.fullTypeName === "string"
  );
}

function isDependencyGraphEdge(payload: unknown): payload is ComponentDependencyGraphEdgeSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    typeof candidate.sourceComponentId === "string" &&
    typeof candidate.targetComponentId === "string" &&
    typeof candidate.edgeType === "string" &&
    typeof candidate.summary === "string" &&
    Array.isArray(candidate.relatedValues) &&
    candidate.relatedValues.every((value) => typeof value === "string") &&
    typeof candidate.isInferred === "boolean" &&
    (candidate.details === null || typeof candidate.details === "string")
  );
}

function isDependencyGraph(payload: unknown): payload is ComponentDependencyGraphSnapshot {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return (
    Array.isArray(candidate.nodes) &&
    candidate.nodes.every(isDependencyGraphNode) &&
    Array.isArray(candidate.edges) &&
    candidate.edges.every(isDependencyGraphEdge)
  );
}

function toTreeNode(node: ComponentNode, parentId: string | null): TreeNodeViewModel {
  const children = node.children.map((child) => toTreeNode(child, node.id));
  return {
    id: node.id,
    name: node.name,
    fullTypeName: node.fullTypeName,
    assemblyName: node.assemblyName,
    domMarkerId: node.domMarkerId ?? null,
    parentId,
    childrenCount: children.length,
    parameters: node.parameters ?? [],
    injectedServices: node.injectedServices ?? [],
    cascadingParameters: node.cascadingParameters ?? [],
    lifecycleMetrics: node.lifecycleMetrics ?? null,
    renderInfo: node.renderInfo ?? null,
    renderDiffInfo: node.renderDiffInfo ?? null,
    renderCount: node.renderCount ?? null,
    children
  };
}

function formatDuration(durationMs: number | null): string {
  if (durationMs === null) {
    return "Unavailable";
  }

  if (durationMs < 1) {
    return `${Math.round(durationMs * 1000)} us`;
  }

  return `${durationMs.toFixed(durationMs >= 10 ? 1 : 2)} ms`;
}

function getSeverityFromDuration(durationMs: number | null, thresholds: DurationThresholds): MetricSeverity {
  if (durationMs === null || !Number.isFinite(durationMs)) {
    return "neutral";
  }

  if (durationMs <= thresholds.goodMaxMs) {
    return "good";
  }

  if (durationMs <= thresholds.warningMaxMs) {
    return "warning";
  }

  return "bad";
}

function getSeverityFromCount(count: number | null, thresholds: CountThresholds): MetricSeverity {
  if (count === null || !Number.isFinite(count)) {
    return "neutral";
  }

  if (count <= thresholds.goodMax) {
    return "good";
  }

  if (count <= thresholds.warningMax) {
    return "warning";
  }

  return "bad";
}

function findNodeById(nodes: TreeNodeViewModel[], nodeId: string): TreeNodeViewModel | null {
  for (const node of nodes) {
    if (node.id === nodeId) {
      return node;
    }

    const childMatch = findNodeById(node.children, nodeId);
    if (childMatch) {
      return childMatch;
    }
  }

  return null;
}

function expandPathToNode(nodes: TreeNodeViewModel[], nodeId: string, expandedNodeIds: Set<string>): boolean {
  for (const node of nodes) {
    if (node.id === nodeId) {
      return true;
    }

    if (expandPathToNode(node.children, nodeId, expandedNodeIds)) {
      expandedNodeIds.add(node.id);
      return true;
    }
  }

  return false;
}

function filterTreeNodes(nodes: TreeNodeViewModel[], searchTerm: string): TreeNodeViewModel[] {
  if (searchTerm.length === 0) {
    return nodes;
  }

  const normalizedTerm = searchTerm.toLocaleLowerCase();
  const filteredNodes: TreeNodeViewModel[] = [];

  for (const node of nodes) {
    const filteredChildren = filterTreeNodes(node.children, searchTerm);
    const matchesNode =
      node.name.toLocaleLowerCase().includes(normalizedTerm) ||
      node.fullTypeName.toLocaleLowerCase().includes(normalizedTerm);

    if (matchesNode || filteredChildren.length > 0) {
      filteredNodes.push({
        ...node,
        children: filteredChildren
      });
    }
  }

  return filteredNodes;
}

function isPickedComponentPayload(payload: unknown): payload is PickedComponentPayload {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return typeof candidate.componentId === "string";
}

function isInspectModePayload(payload: unknown): payload is InspectModePayload {
  if (typeof payload !== "object" || payload === null) {
    return false;
  }

  const candidate = payload as Record<string, unknown>;
  return typeof candidate.active === "boolean";
}

function getInspectedTabId(): number | null {
  const url = new URL(window.location.href);
  const rawTabId = url.searchParams.get("tabId");

  if (!rawTabId) {
    return null;
  }

  const tabId = Number.parseInt(rawTabId, 10);
  return Number.isNaN(tabId) ? null : tabId;
}

async function safeTabsSendMessage(tabId: number, message: unknown): Promise<boolean> {
  try {
    await chrome.tabs.sendMessage(tabId, message);
    return true;
  } catch {
    return false;
  }
}

function isMissingReceiverError(error: unknown): boolean {
  if (!(error instanceof Error)) {
    return false;
  }

  return error.message.includes("Receiving end does not exist") || error.message.includes("Could not establish connection");
}

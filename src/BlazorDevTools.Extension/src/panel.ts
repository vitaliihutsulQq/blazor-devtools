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

type ComponentTreeSnapshot = {
  capturedAt: string;
  roots: ComponentNode[];
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

type DetailsTab = "overview" | "parameters" | "dependencies" | "performance" | "render-cause";

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
  renderCount: number | null;
  children: TreeNodeViewModel[];
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
  goodMaxMs: 8,
  warningMaxMs: 16
};

const averageRenderDurationThresholds: DurationThresholds = {
  goodMaxMs: 1,
  warningMaxMs: 4
};

const totalRenderDurationThresholds: DurationThresholds = {
  goodMaxMs: 10,
  warningMaxMs: 25
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
  goodMax: 1,
  warningMax: 4
};

const stateHasChangedCountThresholds: CountThresholds = {
  goodMax: 1,
  warningMax: 3
};

document.addEventListener("DOMContentLoaded", () => {
  const treeRoot = document.getElementById("tree-root");
  const detailsRoot = document.getElementById("details-root");
  const status = document.getElementById("snapshot-status");
  const inspectToggle = document.getElementById("inspect-toggle");
  const treeFilter = document.getElementById("tree-filter");

  if (!treeRoot || !detailsRoot || !status || !(inspectToggle instanceof HTMLButtonElement) || !(treeFilter instanceof HTMLInputElement)) {
    return;
  }

  const tabId = getInspectedTabId();
  const expandedNodeIds = new Set<string>();
  let selectedNodeId: string | null = null;
  let currentTree: TreeNodeViewModel[] = [];
  let inspectModeActive = false;
  let searchTerm = "";
  let selectedDetailsTab: DetailsTab = "overview";

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
        panel.append(renderInjectedServices(node.injectedServices), renderCascadingParameters(node.cascadingParameters));
        break;
      case "performance":
        panel.append(renderLifecycleMetrics(node.lifecycleMetrics));
        break;
      case "render-cause":
        panel.append(renderWhyDidThisRender(node.renderInfo));
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
      { id: "performance", label: "Performance" },
      { id: "render-cause", label: "Render Cause" }
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
  return typeof candidate.capturedAt === "string" && Array.isArray(candidate.roots) && candidate.roots.every(isComponentNode);
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

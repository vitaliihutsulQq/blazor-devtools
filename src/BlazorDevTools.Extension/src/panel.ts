type ComponentNode = {
  id: string;
  name: string;
  fullTypeName: string;
  assemblyName: string;
  domMarkerId: string | null;
  parameters: ComponentParameterSnapshot[];
  renderCount: number | null;
  children: ComponentNode[];
};

type ComponentParameterSnapshot = {
  name: string;
  value: string | null;
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

type TreeNodeViewModel = {
  id: string;
  name: string;
  fullTypeName: string;
  assemblyName: string;
  domMarkerId: string | null;
  parentId: string | null;
  childrenCount: number;
  parameters: ComponentParameterSnapshot[];
  renderCount: number | null;
  children: TreeNodeViewModel[];
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

    const title = document.createElement("h3");
    title.className = "details-name";
    title.textContent = node.name;
    wrapper.append(title);

    const grid = document.createElement("div");
    grid.className = "details-grid";
    grid.append(
      createDetailCard("Component name", node.name),
      createDetailCard("Full type name", node.fullTypeName),
      createDetailCard("Assembly name", node.assemblyName),
      createDetailCard("Component id", node.id),
      createDetailCard("DOM marker", node.domMarkerId ?? "Unavailable"),
      createDetailCard("Parent id", node.parentId ?? "Root component"),
      createDetailCard("Children count", node.childrenCount.toString()),
      createDetailCard("Render count", node.renderCount === null ? "Unavailable" : node.renderCount.toString())
    );

    wrapper.append(grid);
    wrapper.append(renderParameters(node.parameters));
    return wrapper;
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
    (candidate.domMarkerId === null || typeof candidate.domMarkerId === "string") &&
    (candidate.renderCount === null || typeof candidate.renderCount === "number") &&
    Array.isArray(candidate.parameters) &&
    candidate.parameters.every(isComponentParameter) &&
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

function toTreeNode(node: ComponentNode, parentId: string | null): TreeNodeViewModel {
  const children = node.children.map((child) => toTreeNode(child, node.id));
  return {
    id: node.id,
    name: node.name,
    fullTypeName: node.fullTypeName,
    assemblyName: node.assemblyName,
    domMarkerId: node.domMarkerId,
    parentId,
    childrenCount: children.length,
    parameters: node.parameters,
    renderCount: node.renderCount,
    children
  };
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

(() => {
  const snapshotMessageType = "blazor-devtools:component-tree-snapshot";
  const runtimeSource = "blazor-devtools-runtime";
  const extensionSource = "blazor-devtools-extension";
  const domMarkerAttributeName = "data-blazor-devtools-component-id";
  const overlayId = "__blazor_devtools_overlay__";
  const requestSnapshotKind = "blazor-devtools:request-snapshot";
  const requestSnapshotMessageType = "blazor-devtools:request-current-snapshot";

  let inspectModeActive = false;
  let overlayElement: HTMLDivElement | null = null;
  let hoveredContext: InspectContext | null = null;
  let latestSnapshotMessage: unknown | null = null;

  window.addEventListener("message", (event) => {
    if (event.source !== window) {
      return;
    }

    const message = event.data;

    if (!isSnapshotMessage(message)) {
      return;
    }

    latestSnapshotMessage = message;

    void safeSendRuntimeMessage({
      kind: "blazor-devtools:relay",
      payload: message
    });
  });

  chrome.runtime.onMessage.addListener((message) => {
    if (!message) {
      return;
    }

    if (message.kind === requestSnapshotKind) {
      if (latestSnapshotMessage) {
        void safeSendRuntimeMessage({
          kind: "blazor-devtools:relay",
          payload: latestSnapshotMessage
        });
      }

      requestSnapshotFromPage();
      return;
    }

    if (message.kind !== "blazor-devtools:set-inspect-mode") {
      return;
    }

    const active = Boolean(message.payload?.active);
    setInspectMode(active);
  });

  document.addEventListener("mousemove", (event) => {
    if (!inspectModeActive) {
      return;
    }

    const target = event.target instanceof Element ? event.target : null;
    hoveredContext = findInspectContext(target);
    updateOverlay(hoveredContext?.overlayElement ?? null);
  }, true);

  document.addEventListener("click", (event) => {
    if (!inspectModeActive) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();

    const target = event.target instanceof Element ? event.target : null;
    const inspectContext = findInspectContext(target);
    const componentId = inspectContext?.componentId;

    if (componentId) {
      void safeSendRuntimeMessage({
        kind: "blazor-devtools:component-picked",
        payload: { componentId }
      });
    }

    setInspectMode(false);
  }, true);

  document.addEventListener("keydown", (event) => {
    if (!inspectModeActive || event.key !== "Escape") {
      return;
    }

    setInspectMode(false);
  }, true);

  window.addEventListener("scroll", () => {
    if (inspectModeActive) {
      updateOverlay(hoveredContext?.overlayElement ?? null);
    }
  }, true);

  window.addEventListener("resize", () => {
    if (inspectModeActive) {
      updateOverlay(hoveredContext?.overlayElement ?? null);
    }
  });

  function setInspectMode(active: boolean): void {
    inspectModeActive = active;
    document.documentElement.style.cursor = active ? "crosshair" : "";
    document.body?.style.setProperty("cursor", active ? "crosshair" : "");

    if (!active) {
      hoveredContext = null;
      removeOverlay();
    }

    void safeSendRuntimeMessage({
      kind: "blazor-devtools:inspect-mode-changed",
      payload: { active }
    });
  }

  function requestSnapshotFromPage(): void {
    window.postMessage(
      {
        source: extensionSource,
        messageType: requestSnapshotMessageType
      },
      window.location.origin
    );
  }

async function safeSendRuntimeMessage(message: unknown): Promise<void> {
  try {
    await chrome.runtime.sendMessage(message);
  } catch {
    return;
  }
}

  function updateOverlay(element: Element | null): void {
    if (!inspectModeActive || !element) {
      removeOverlay();
      return;
    }

    const rect = element.getBoundingClientRect();
    const overlay = ensureOverlay();
    overlay.style.display = "block";
    overlay.style.top = `${rect.top + window.scrollY}px`;
    overlay.style.left = `${rect.left + window.scrollX}px`;
    overlay.style.width = `${rect.width}px`;
    overlay.style.height = `${rect.height}px`;
  }

  function ensureOverlay(): HTMLDivElement {
    if (overlayElement) {
      return overlayElement;
    }

    const overlay = document.createElement("div");
    overlay.id = overlayId;
    overlay.style.position = "absolute";
    overlay.style.pointerEvents = "none";
    overlay.style.zIndex = "2147483647";
    overlay.style.border = "2px solid #2563eb";
    overlay.style.background = "rgba(37, 99, 235, 0.12)";
    overlay.style.borderRadius = "6px";
    overlay.style.boxSizing = "border-box";
    overlay.style.display = "none";

    document.documentElement.append(overlay);
    overlayElement = overlay;
    return overlay;
  }

  function removeOverlay(): void {
    overlayElement?.remove();
    overlayElement = null;
  }

  function findInspectContext(target: Element | null): InspectContext | null {
    if (!(target instanceof HTMLElement)) {
      return null;
    }

    const markerElement = target.closest(`[${domMarkerAttributeName}]`) as HTMLElement | null;
    if (!markerElement) {
      return null;
    }

    return {
      componentId: markerElement.getAttribute(domMarkerAttributeName),
      overlayElement: target
    };
  }

  function isSnapshotMessage(message: unknown): message is {
    source: string;
    messageType: string;
    payload: unknown;
  } {
    if (typeof message !== "object" || message === null) {
      return false;
    }

    const candidate = message as Record<string, unknown>;
    return candidate.source === runtimeSource && candidate.messageType === snapshotMessageType;
  }

  type InspectContext = {
    componentId: string | null;
    overlayElement: HTMLElement;
  };
})();

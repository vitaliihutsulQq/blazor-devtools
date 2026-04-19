const blazorDevToolsSnapshotMessageType = "blazor-devtools:component-tree-snapshot";
const blazorDevToolsRuntimeSource = "blazor-devtools-runtime";
const blazorDevToolsExtensionSource = "blazor-devtools-extension";
const blazorDevToolsRequestSnapshotMessageType = "blazor-devtools:request-current-snapshot";

let latestSnapshotMessage = null;

window.BlazorDevTools = {
  postMessage(message) {
    if (isSnapshotMessage(message)) {
      latestSnapshotMessage = message;
    }

    window.postMessage(message, window.location.origin);
  }
};

window.addEventListener("message", (event) => {
  if (event.source !== window || !isSnapshotRequestMessage(event.data) || !latestSnapshotMessage) {
    return;
  }

  window.postMessage(latestSnapshotMessage, window.location.origin);
});

function isSnapshotMessage(message) {
  return Boolean(
    message &&
      typeof message === "object" &&
      message.source === blazorDevToolsRuntimeSource &&
      message.messageType === blazorDevToolsSnapshotMessageType
  );
}

function isSnapshotRequestMessage(message) {
  return Boolean(
    message &&
      typeof message === "object" &&
      message.source === blazorDevToolsExtensionSource &&
      message.messageType === blazorDevToolsRequestSnapshotMessageType
  );
}

console.debug("[Blazor DevTools][devtools] devtools page loaded");

const panelUrl = new URL("panel.html", window.location.href);
const inspectedTabId = chrome.devtools.inspectedWindow.tabId;

console.debug("[Blazor DevTools][devtools] inspectedWindow.tabId", inspectedTabId);

panelUrl.searchParams.set("tabId", inspectedTabId.toString());

const panelPath = panelUrl.pathname.startsWith("/") ? panelUrl.pathname.slice(1) : panelUrl.pathname;

chrome.devtools.panels.create("Blazor", "", panelPath + panelUrl.search);

console.debug("[Blazor DevTools][devtools] panel created", {
  tabId: inspectedTabId,
  panelUrl: panelPath + panelUrl.search
});

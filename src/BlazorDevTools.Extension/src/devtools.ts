const panelUrl = new URL("panel.html", window.location.href);
const inspectedTabId = chrome.devtools.inspectedWindow.tabId;

panelUrl.searchParams.set("tabId", inspectedTabId.toString());

const panelPath = panelUrl.pathname.startsWith("/") ? panelUrl.pathname.slice(1) : panelUrl.pathname;

chrome.devtools.panels.create("Blazor", "", panelPath + panelUrl.search);

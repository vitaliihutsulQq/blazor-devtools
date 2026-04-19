using BlazorDevTools.Protocol;
using Microsoft.JSInterop;

namespace BlazorDevTools.Runtime;

public sealed class DevToolsSnapshotBridge
{
    private readonly ComponentTracker componentTracker;
    private readonly IJSRuntime jsRuntime;

    public DevToolsSnapshotBridge(ComponentTracker componentTracker, IJSRuntime jsRuntime)
    {
        this.componentTracker = componentTracker;
        this.jsRuntime = jsRuntime;
    }

    public ValueTask PostSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = componentTracker.BuildSnapshot();
        var message = new DevToolsMessage<ComponentTreeSnapshot>(
            DevToolsSources.Runtime,
            DevToolsMessageTypes.ComponentTreeSnapshot,
            snapshot);

        return jsRuntime.InvokeVoidAsync("BlazorDevTools.postMessage", cancellationToken, message);
    }
}

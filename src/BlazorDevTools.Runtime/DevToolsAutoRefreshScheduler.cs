using Microsoft.JSInterop;

namespace BlazorDevTools.Runtime;

public sealed class DevToolsAutoRefreshScheduler : IDisposable
{
    private readonly Func<CancellationToken, Task> publishSnapshotAsync;
    private readonly TimeSpan debounceInterval;
    private readonly object syncRoot = new();
    private CancellationTokenSource? pendingRefreshCancellation;
    private int refreshVersion;
    private bool isDisposed;

    public DevToolsAutoRefreshScheduler(DevToolsSnapshotBridge snapshotBridge)
        : this(cancellationToken => snapshotBridge.PostSnapshotAsync(cancellationToken).AsTask(), TimeSpan.FromMilliseconds(150))
    {
    }

    internal DevToolsAutoRefreshScheduler(Func<CancellationToken, Task> publishSnapshotAsync, TimeSpan debounceInterval)
    {
        this.publishSnapshotAsync = publishSnapshotAsync;
        this.debounceInterval = debounceInterval;
    }

    public void RequestRefresh()
    {
        CancellationTokenSource? previousCancellation;
        CancellationTokenSource nextCancellation;
        int nextVersion;

        lock (syncRoot)
        {
            if (isDisposed)
            {
                return;
            }

            nextVersion = ++refreshVersion;
            previousCancellation = pendingRefreshCancellation;
            nextCancellation = new CancellationTokenSource();
            pendingRefreshCancellation = nextCancellation;
        }

        previousCancellation?.Cancel();
        previousCancellation?.Dispose();

        _ = PublishSnapshotWhenQuietAsync(nextVersion, nextCancellation);
    }

    public void Dispose()
    {
        CancellationTokenSource? cancellation;

        lock (syncRoot)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            cancellation = pendingRefreshCancellation;
            pendingRefreshCancellation = null;
        }

        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private async Task PublishSnapshotWhenQuietAsync(int version, CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(debounceInterval, cancellation.Token);

            lock (syncRoot)
            {
                if (isDisposed || version != refreshVersion)
                {
                    return;
                }
            }

            await publishSnapshotAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (JSException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            lock (syncRoot)
            {
                if (ReferenceEquals(pendingRefreshCancellation, cancellation))
                {
                    pendingRefreshCancellation = null;
                }
            }

            cancellation.Dispose();
        }
    }
}

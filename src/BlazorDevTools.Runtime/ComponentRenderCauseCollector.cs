using BlazorDevTools.Protocol;

namespace BlazorDevTools.Runtime;

public sealed class ComponentRenderCauseCollector
{
    private const int MaxRecentCauses = 5;

    private readonly List<ComponentRenderCauseSnapshot> recentCauses = [];
    private string? lastParameterFingerprint;
    private PendingCause? pendingCause;
    private int renderSequence;

    public void ObserveParameters(IReadOnlyList<ComponentParameterSnapshot> parameters, bool isRegistered)
    {
        var fingerprint = string.Join("|", parameters.Select(parameter => $"{parameter.Name}={parameter.Value}"));

        if (!isRegistered)
        {
            pendingCause = new PendingCause("First render", false, "Initial component render");
            lastParameterFingerprint = fingerprint;
            return;
        }

        if (!string.Equals(lastParameterFingerprint, fingerprint, StringComparison.Ordinal))
        {
            pendingCause = new PendingCause("Parameters changed", false, "Component parameters changed before rendering");
            lastParameterFingerprint = fingerprint;
        }
    }

    public void MarkStateHasChanged()
    {
        pendingCause = new PendingCause("StateHasChanged invoked", false, "StateHasChanged was invoked on this component");
    }

    public void RecordRender(bool isFirstRender)
    {
        var cause = pendingCause ?? (isFirstRender
            ? new PendingCause("First render", false, "Initial component render")
            : new PendingCause("Parent rendered / framework-triggered render", true, "No direct component-local cause was observed before this render"));

        renderSequence++;
        recentCauses.Add(new ComponentRenderCauseSnapshot(renderSequence, cause.Cause, cause.IsApproximate, cause.Details));

        if (recentCauses.Count > MaxRecentCauses)
        {
            recentCauses.RemoveAt(0);
        }

        pendingCause = null;
    }

    public ComponentRenderInfoSnapshot BuildSnapshot()
    {
        return new ComponentRenderInfoSnapshot(recentCauses.LastOrDefault(), recentCauses.ToArray());
    }

    private sealed record PendingCause(string Cause, bool IsApproximate, string? Details);
}

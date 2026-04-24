using BlazorDevTools.Protocol;

namespace BlazorDevTools.Runtime;

public sealed class ComponentRenderDiffCollector
{
    private const int MaxRecentDiffs = 5;
    private const string MissingValueSummary = "<not supplied>";

    private readonly List<ComponentRenderDiffSnapshot> recentDiffs = [];
    private readonly Func<DateTimeOffset> timestampProvider;
    private IReadOnlyList<ComponentParameterSnapshot>? latestParameters;
    private PendingDiff? pendingDiff;
    private int renderSequence;

    public ComponentRenderDiffCollector(Func<DateTimeOffset>? timestampProvider = null)
    {
        this.timestampProvider = timestampProvider ?? (() => DateTimeOffset.UtcNow);
    }

    public void ObserveParameters(IReadOnlyList<ComponentParameterSnapshot> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var parameterSnapshot = parameters.ToArray();
        pendingDiff = new PendingDiff(
            HasPreviousSnapshot: latestParameters is not null,
            ParameterChanges: latestParameters is null
                ? []
                : BuildParameterDiffs(latestParameters, parameterSnapshot));
        latestParameters = parameterSnapshot;
    }

    public void RecordRender()
    {
        var diff = pendingDiff ?? new PendingDiff(
            HasPreviousSnapshot: latestParameters is not null,
            ParameterChanges: []);

        renderSequence++;
        recentDiffs.Add(new ComponentRenderDiffSnapshot(renderSequence, timestampProvider(), diff.HasPreviousSnapshot, diff.ParameterChanges));

        if (recentDiffs.Count > MaxRecentDiffs)
        {
            recentDiffs.RemoveAt(0);
        }

        pendingDiff = null;
    }

    public ComponentRenderDiffInfoSnapshot BuildSnapshot()
    {
        return new ComponentRenderDiffInfoSnapshot(recentDiffs.LastOrDefault(), recentDiffs.ToArray());
    }

    private static IReadOnlyList<ComponentParameterDiffSnapshot> BuildParameterDiffs(
        IReadOnlyList<ComponentParameterSnapshot> previousParameters,
        IReadOnlyList<ComponentParameterSnapshot> currentParameters)
    {
        var previousMap = previousParameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value, StringComparer.Ordinal);
        var currentMap = currentParameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value, StringComparer.Ordinal);
        var orderedNames = new List<string>(currentParameters.Count + previousParameters.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parameter in currentParameters)
        {
            if (seen.Add(parameter.Name))
            {
                orderedNames.Add(parameter.Name);
            }
        }

        foreach (var parameter in previousParameters)
        {
            if (seen.Add(parameter.Name))
            {
                orderedNames.Add(parameter.Name);
            }
        }

        var diffs = new List<ComponentParameterDiffSnapshot>();
        foreach (var name in orderedNames)
        {
            var hadPreviousValue = previousMap.TryGetValue(name, out var previousValue);
            var hasCurrentValue = currentMap.TryGetValue(name, out var currentValue);
            var normalizedPreviousValue = hadPreviousValue ? previousValue : MissingValueSummary;
            var normalizedCurrentValue = hasCurrentValue ? currentValue : MissingValueSummary;

            if (string.Equals(normalizedPreviousValue, normalizedCurrentValue, StringComparison.Ordinal))
            {
                continue;
            }

            diffs.Add(new ComponentParameterDiffSnapshot(name, normalizedPreviousValue, normalizedCurrentValue));
        }

        return diffs;
    }

    private sealed record PendingDiff(bool HasPreviousSnapshot, IReadOnlyList<ComponentParameterDiffSnapshot> ParameterChanges);
}

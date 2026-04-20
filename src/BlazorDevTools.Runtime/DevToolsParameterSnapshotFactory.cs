using BlazorDevTools.Protocol;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.Runtime;

public static class DevToolsParameterSnapshotFactory
{
    public static IReadOnlyList<ComponentParameterSnapshot> Create(ParameterView parameters, params string[] ignoredParameterNames)
    {
        ArgumentNullException.ThrowIfNull(ignoredParameterNames);

        HashSet<string>? ignoredParameters = ignoredParameterNames.Length == 0
            ? null
            : [.. ignoredParameterNames];

        var capturedParameters = new List<ComponentParameterSnapshot>();

        foreach (var parameter in parameters)
        {
            if (ignoredParameters?.Contains(parameter.Name) == true)
            {
                continue;
            }

            capturedParameters.Add(new ComponentParameterSnapshot(parameter.Name, ComponentParameterFormatter.Format(parameter.Value)));
        }

        return capturedParameters;
    }
}

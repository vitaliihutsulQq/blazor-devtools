using System.Reflection;
using BlazorDevTools.Protocol;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.Runtime;

public static class ComponentInjectedServiceSnapshotFactory
{
    public static IReadOnlyList<ComponentInjectedServiceSnapshot> Create(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var snapshots = new List<ComponentInjectedServiceSnapshot>();
        var seenPropertyNames = new HashSet<string>(StringComparer.Ordinal);

        for (var currentType = componentType;
             currentType is not null && currentType != typeof(DevtoolsComponentBase) && currentType != typeof(ComponentBase);
             currentType = currentType.BaseType)
        {
            var properties = currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(property => property.GetCustomAttribute<InjectAttribute>(inherit: false) is not null)
                .OrderBy(property => property.MetadataToken);

            foreach (var property in properties)
            {
                if (!seenPropertyNames.Add(property.Name))
                {
                    continue;
                }

                snapshots.Add(new ComponentInjectedServiceSnapshot(
                    property.Name,
                    property.PropertyType.Name,
                    property.PropertyType.FullName ?? property.PropertyType.Name));
            }
        }

        return snapshots;
    }
}

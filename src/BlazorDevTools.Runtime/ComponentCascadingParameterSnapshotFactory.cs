using System.Reflection;
using BlazorDevTools.Protocol;
using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.Runtime;

public static class ComponentCascadingParameterSnapshotFactory
{
    public static IReadOnlyList<ComponentCascadingParameterSnapshot> Create(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var snapshots = new List<ComponentCascadingParameterSnapshot>();
        var seenPropertyNames = new HashSet<string>(StringComparer.Ordinal);

        for (var currentType = componentType;
             currentType is not null && currentType != typeof(DevtoolsComponentBase) && currentType != typeof(ComponentBase);
             currentType = currentType.BaseType)
        {
            var properties = currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Select(property => new
                {
                    Property = property,
                    Attribute = property.GetCustomAttribute<CascadingParameterAttribute>(inherit: false)
                })
                .Where(entry => entry.Attribute is not null)
                .OrderBy(entry => entry.Property.MetadataToken);

            foreach (var entry in properties)
            {
                if (!seenPropertyNames.Add(entry.Property.Name))
                {
                    continue;
                }

                snapshots.Add(new ComponentCascadingParameterSnapshot(
                    entry.Property.Name,
                    entry.Property.PropertyType.Name,
                    entry.Property.PropertyType.FullName ?? entry.Property.PropertyType.Name,
                    string.IsNullOrWhiteSpace(entry.Attribute!.Name) ? null : entry.Attribute.Name));
            }
        }

        return snapshots;
    }
}

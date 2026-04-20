using System.Reflection;

namespace BlazorDevTools.Runtime;

public sealed class DevToolsComponentProxyRegistry : IDevToolsComponentProxyRegistry
{
    private readonly IReadOnlyDictionary<Type, Type> componentMappings;

    public DevToolsComponentProxyRegistry(IEnumerable<DevToolsComponentProxyRegistration> registrations)
    {
        var discoveredRegistrations = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(GetRegistrationsFromAssembly);

        componentMappings = registrations
            .Concat(discoveredRegistrations)
            .GroupBy(registration => registration.ComponentType)
            .ToDictionary(group => group.Key, group => group.Last().ProxyType);
    }

    public bool TryGetProxyType(Type componentType, out Type proxyType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        return componentMappings.TryGetValue(componentType, out proxyType!);
    }

    private static IEnumerable<DevToolsComponentProxyRegistration> GetRegistrationsFromAssembly(Assembly assembly)
    {
        Type[] candidateTypes;

        try
        {
            candidateTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            candidateTypes = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }

        foreach (var manifestType in candidateTypes.Where(IsManifestType))
        {
            if (Activator.CreateInstance(manifestType) is not IDevToolsComponentProxyManifest manifest)
            {
                continue;
            }

            foreach (var registration in manifest.GetRegistrations())
            {
                yield return registration;
            }
        }
    }

    private static bool IsManifestType(Type type)
    {
        return type is
        {
            IsAbstract: false,
            IsInterface: false,
            ContainsGenericParameters: false
        }
        && typeof(IDevToolsComponentProxyManifest).IsAssignableFrom(type)
        && type.GetConstructor(Type.EmptyTypes) is not null;
    }
}

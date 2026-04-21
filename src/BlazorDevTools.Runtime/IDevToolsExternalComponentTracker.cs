namespace BlazorDevTools.Runtime;

public interface IDevToolsExternalComponentTracker
{
    void EnsureInitialized();

    string? ResolveParentComponentId(Type componentType);
}

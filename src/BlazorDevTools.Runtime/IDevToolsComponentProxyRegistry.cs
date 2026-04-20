namespace BlazorDevTools.Runtime;

public interface IDevToolsComponentProxyRegistry
{
    bool TryGetProxyType(Type componentType, out Type proxyType);
}

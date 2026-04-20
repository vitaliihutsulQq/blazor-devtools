namespace BlazorDevTools.Runtime;

public interface IDevToolsComponentProxyManifest
{
    IReadOnlyList<DevToolsComponentProxyRegistration> GetRegistrations();
}

using Microsoft.AspNetCore.Components;

namespace BlazorDevTools.Runtime;

public interface IDevToolsComponentProxy : IComponent, IDisposable
{
    Type DevToolsOriginalComponentType { get; }
}

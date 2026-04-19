namespace BlazorDevTools.Protocol;

public sealed record DevToolsMessage<TPayload>(string Source, string MessageType, TPayload Payload);

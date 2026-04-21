using System.Reflection;
using System.Linq.Expressions;
using BlazorDevTools.Protocol;

namespace BlazorDevTools.Runtime;

public sealed class DevToolsExternalComponentTracker : IDevToolsExternalComponentTracker, IDisposable
{
    private const string RadzenDialogServiceTypeName = "Radzen.DialogService, Radzen.Blazor";
    private const string RadzenDialogRootTypeName = "Radzen.DialogService.DialogRoot";

    private readonly IServiceProvider serviceProvider;
    private readonly ComponentTracker componentTracker;
    private readonly DevToolsAutoRefreshScheduler autoRefreshScheduler;
    private readonly object syncRoot = new();
    private readonly List<Subscription> subscriptions = [];
    private readonly List<ExternalRootEntry> openRoots = [];
    private bool isInitialized;

    public DevToolsExternalComponentTracker(
        IServiceProvider serviceProvider,
        ComponentTracker componentTracker,
        DevToolsAutoRefreshScheduler autoRefreshScheduler)
    {
        this.serviceProvider = serviceProvider;
        this.componentTracker = componentTracker;
        this.autoRefreshScheduler = autoRefreshScheduler;
    }

    public void EnsureInitialized()
    {
        lock (syncRoot)
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            TryInitializeRadzenDialogTracking();
        }
    }

    public string? ResolveParentComponentId(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        EnsureInitialized();

        lock (syncRoot)
        {
            var typedMatch = openRoots.LastOrDefault(entry =>
                !entry.IsClosed &&
                entry.ContentComponentType == componentType);

            if (typedMatch is not null)
            {
                typedMatch.HasAttachedChild = true;
                return typedMatch.RootComponentId;
            }

            var fragmentMatch = openRoots.LastOrDefault(entry => !entry.IsClosed);

            if (fragmentMatch is not null)
            {
                fragmentMatch.HasAttachedChild = true;
                return fragmentMatch.RootComponentId;
            }

            return null;
        }
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Event.RemoveEventHandler(subscription.Target, subscription.Handler);
            }

            subscriptions.Clear();
            openRoots.Clear();
        }
    }

    private void TryInitializeRadzenDialogTracking()
    {
        var serviceType = Type.GetType(RadzenDialogServiceTypeName, throwOnError: false);
        if (serviceType is null)
        {
            return;
        }

        var dialogService = serviceProvider.GetService(serviceType);
        if (dialogService is null)
        {
            return;
        }

        Subscribe(dialogService, "OnOpen", args => HandleDialogOpened(args, isSideDialog: false));
        Subscribe(dialogService, "OnClose", args => HandleDialogClosed(isSideDialog: false));
        Subscribe(dialogService, "OnSideOpen", args => HandleDialogOpened(args, isSideDialog: true));
        Subscribe(dialogService, "OnSideClose", args => HandleDialogClosed(isSideDialog: true));
    }

    private void Subscribe(object target, string eventName, Action<object?[]> callback)
    {
        var @event = target.GetType().GetEvent(eventName);
        if (@event?.EventHandlerType is null)
        {
            return;
        }

        var invokeMethod = @event.EventHandlerType.GetMethod("Invoke");
        if (invokeMethod is null)
        {
            return;
        }

        var parameters = invokeMethod.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();

        var callbackTarget = Expression.Constant(callback.Target);
        var callbackMethod = callback.Method;
        var callbackArguments = Expression.NewArrayInit(
            typeof(object),
            parameters.Select(parameter => Expression.Convert(parameter, typeof(object))));

        var body = Expression.Call(callbackTarget, callbackMethod, callbackArguments);
        var handler = Expression.Lambda(@event.EventHandlerType, body, parameters).Compile();
        @event.AddEventHandler(target, handler);
        subscriptions.Add(new Subscription(target, @event, handler));
    }

    private void HandleDialogOpened(object?[] args, bool isSideDialog)
    {
        var title = args.OfType<string>().FirstOrDefault() ?? (isSideDialog ? "Radzen side dialog" : "Radzen dialog");
        var componentType = args.OfType<Type>().FirstOrDefault(IsUserDialogContentType);
        var parameters = args.OfType<IDictionary<string, object>>().FirstOrDefault();
        var rootComponentId = $"radzen-dialog-{Guid.NewGuid():N}";
        var parameterSnapshots = CreateDialogParameterSnapshots(title, componentType, isSideDialog, parameters);

        lock (syncRoot)
        {
            openRoots.Add(new ExternalRootEntry(rootComponentId, title, componentType, isSideDialog));
        }

        componentTracker.RegisterSyntheticComponent(
            rootComponentId,
            isSideDialog ? "RadzenSideDialog" : "RadzenDialog",
            RadzenDialogRootTypeName,
            "Radzen.Blazor",
            parentComponentId: null,
            parameters: parameterSnapshots);

        autoRefreshScheduler.RequestRefresh();
    }

    private void HandleDialogClosed(bool isSideDialog)
    {
        ExternalRootEntry? entry;

        lock (syncRoot)
        {
            entry = openRoots.LastOrDefault(root => !root.IsClosed && root.IsSideDialog == isSideDialog);
            if (entry is null)
            {
                return;
            }

            entry.IsClosed = true;
            openRoots.Remove(entry);
        }

        componentTracker.UnregisterComponent(entry.RootComponentId);
        autoRefreshScheduler.RequestRefresh();
    }

    private static IReadOnlyList<ComponentParameterSnapshot> CreateDialogParameterSnapshots(
        string title,
        Type? componentType,
        bool isSideDialog,
        IDictionary<string, object>? parameters)
    {
        var snapshots = new List<ComponentParameterSnapshot>
        {
            new("Title", title),
            new("DialogKind", isSideDialog ? "SideDialog" : "Dialog")
        };

        if (componentType is not null)
        {
            snapshots.Add(new("ContentComponentType", componentType.FullName ?? componentType.Name));
        }
        else
        {
            snapshots.Add(new("ContentComponentType", "<render-fragment>"));
        }

        if (parameters is not null)
        {
            foreach (var parameter in parameters)
            {
                snapshots.Add(new($"DialogParameter:{parameter.Key}", ComponentParameterFormatter.Format(parameter.Value)));
            }
        }

        return snapshots;
    }

    private static bool IsUserDialogContentType(Type type)
    {
        return typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(type)
               && !(type.Namespace?.StartsWith("Radzen", StringComparison.Ordinal) ?? false);
    }

    private sealed record Subscription(object Target, EventInfo Event, Delegate Handler);

    private sealed class ExternalRootEntry
    {
        public ExternalRootEntry(string rootComponentId, string title, Type? contentComponentType, bool isSideDialog)
        {
            RootComponentId = rootComponentId;
            Title = title;
            ContentComponentType = contentComponentType;
            IsSideDialog = isSideDialog;
        }

        public string RootComponentId { get; }

        public string Title { get; }

        public Type? ContentComponentType { get; }

        public bool IsSideDialog { get; }

        public bool HasAttachedChild { get; set; }

        public bool IsClosed { get; set; }
    }
}

using Autodesk.Revit.UI;
using MagicDefor.Revit.Services;

namespace MagicDefor.Revit.Infrastructure;

internal sealed class LiveFilterExternalEventHandler : IExternalEventHandler
{
    private readonly RevitFilterService _service;
    private readonly object _sync = new();
    private Action<UIApplication>? _pendingWork;

    public LiveFilterExternalEventHandler(RevitFilterService service)
    {
        _service = service;
    }

    public string GetName() => "MagicDefor Live Filter";

    public void Update(UIApplication uiApplication, LiveFilterRequest request)
    {
        lock (_sync)
        {
            _pendingWork = delegate(UIApplication application)
            {
                _service.Execute(application, request);
            };
        }
    }

    public void Update(UIApplication uiApplication, Action<UIApplication> action)
    {
        lock (_sync)
        {
            _pendingWork = action;
        }
    }

    public void Execute(UIApplication app)
    {
        Action<UIApplication>? pendingWork;

        lock (_sync)
        {
            pendingWork = _pendingWork;
            _pendingWork = null;
        }

        if (pendingWork == null)
        {
            return;
        }

        pendingWork(app);
    }
}

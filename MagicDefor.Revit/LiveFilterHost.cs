using Autodesk.Revit.UI;
using MagicDefor.Revit.Infrastructure;
using MagicDefor.Revit.Services;
using MagicDefor.Revit.ViewModels;
using MagicDefor.Revit.Views;

namespace MagicDefor.Revit;

internal sealed class LiveFilterHost : IDisposable
{
    private LiveFilterWindow? _window;
    private ExternalEvent? _externalEvent;
    private LiveFilterExternalEventHandler? _handler;

    public void Show(UIApplication uiApplication)
    {
        var uiDocument = uiApplication.ActiveUIDocument;
        var document = uiDocument?.Document;

        if (uiDocument is null || document is null)
        {
            TaskDialog.Show("MagicDefor", "Open a Revit document before starting the live filter.");
            return;
        }

        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        var selectionService = new SelectionSourceService();
        var snapshot = selectionService.ReadFromCurrentSelection(uiApplication);
        _handler = new LiveFilterExternalEventHandler(new RevitFilterService());
        _externalEvent = ExternalEvent.Create(_handler);
        var repository = new FilterConfigurationRepository();

        var viewModel = new LiveFilterViewModel(
            snapshot,
            request =>
            {
                if (_handler is null || _externalEvent is null)
                {
                    return;
                }

                _handler.Update(uiApplication, request);
                _externalEvent.Raise();
            },
            refreshSelection =>
            {
                if (_handler is null || _externalEvent is null)
                {
                    return;
                }

                _handler.Update(uiApplication, delegate(UIApplication application)
                {
                    var latestSnapshot = selectionService.ReadFromCurrentSelection(application);
                    _window?.Dispatcher.Invoke(delegate
                    {
                        refreshSelection(latestSnapshot);
                    });
                });
                _externalEvent.Raise();
            },
            repository);

        _window = new LiveFilterWindow(viewModel);
        _window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            ReleaseWindowResources();
        };

        _window.Show();
        viewModel.QueueRefresh();
    }

    public void Dispose()
    {
        _window?.Close();
        ReleaseWindowResources();
    }

    private void ReleaseWindowResources()
    {
        _window = null;
        _externalEvent?.Dispose();
        _externalEvent = null;
        _handler = null;
    }
}

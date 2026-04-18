using Autodesk.Revit.UI;
using MagicDefor.Revit.Commands;

namespace MagicDefor.Revit;

public sealed class App : IExternalApplication
{
    internal static LiveFilterHost? Host { get; private set; }

    public Result OnStartup(UIControlledApplication application)
    {
        const string tabName = "DEFOR Tools";
        const string panelName = "Parameters";

        try
        {
            application.CreateRibbonTab(tabName);
        }
        catch
        {
        }

        var panel = application.GetRibbonPanels(tabName).FirstOrDefault(p => p.Name == panelName)
                    ?? application.CreateRibbonPanel(tabName, panelName);

        var button = new PushButtonData(
            "MagicDefor.LiveFilter.Open",
            "Live Filter",
            typeof(App).Assembly.Location,
            typeof(OpenLiveFilterCommand).FullName!)
        {
            ToolTip = "Open the DEFOR live filter window for selection-based advanced element filtering."
        };

        panel.AddItem(button);
        Host = new LiveFilterHost();
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        Host?.Dispose();
        Host = null;
        return Result.Succeeded;
    }
}

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace MagicDefor.Revit.Commands;

[Transaction(TransactionMode.Manual)]
public sealed class OpenLiveFilterCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        App.Host?.Show(commandData.Application);
        return Result.Succeeded;
    }
}

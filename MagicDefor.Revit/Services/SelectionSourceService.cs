using Autodesk.Revit.UI;
using MagicDefor.Revit.Infrastructure;
using MagicDefor.Revit.Models;

namespace MagicDefor.Revit.Services;

internal sealed class SelectionSourceService
{
    public SelectionSnapshot ReadFromCurrentSelection(UIApplication uiApplication)
    {
        var uiDocument = uiApplication.ActiveUIDocument;
        var document = uiDocument != null ? uiDocument.Document : null;

        if (uiDocument == null || document == null)
        {
            return new SelectionSnapshot("No document", 0, new SelectionCategoryContext[0]);
        }

        return SelectionSnapshotFactory.Create(document, uiDocument.Selection.GetElementIds());
    }
}

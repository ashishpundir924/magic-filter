using MagicDefor.Revit.Models;

namespace MagicDefor.Revit.Infrastructure;

internal sealed class LiveFilterRequest
{
    public LiveFilterAction Action { get; set; }

    public List<SelectionScopeDefinition> SelectedScopes { get; set; } = new List<SelectionScopeDefinition>();

    public List<FilterRuleDefinition> Rules { get; set; } = new List<FilterRuleDefinition>();

    public bool IncludeElementTypes { get; set; }

    public bool LimitToActiveView { get; set; } = true;
}

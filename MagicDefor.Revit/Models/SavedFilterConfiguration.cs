namespace MagicDefor.Revit.Models;

public sealed class SavedFilterConfiguration
{
    public string Name { get; set; } = string.Empty;

    public List<string> ScopeKeys { get; set; } = new List<string>();

    public bool IncludeElementTypes { get; set; }

    public bool LimitToActiveView { get; set; } = true;

    public List<FilterRuleDefinition> Rules { get; set; } = new List<FilterRuleDefinition>();
}

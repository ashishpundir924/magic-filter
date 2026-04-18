namespace MagicDefor.Revit.Models;

internal sealed class SelectionScopeDefinition
{
    public string CategoryName { get; set; } = string.Empty;

    public string FamilyName { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public string Key
    {
        get { return string.Join("|", new[] { CategoryName ?? string.Empty, FamilyName ?? string.Empty, TypeName ?? string.Empty }); }
    }
}

namespace MagicDefor.Revit.Models;

internal sealed class SelectionFamilyContext
{
    public SelectionFamilyContext(string familyName, IEnumerable<string> parameterNames, IEnumerable<SelectionTypeContext> types)
    {
        FamilyName = familyName;
        ParameterNames = parameterNames.OrderBy(name => name).ToList();
        Types = types.OrderBy(type => type.TypeName).ToList();
    }

    public string FamilyName { get; private set; }

    public List<string> ParameterNames { get; private set; }

    public List<SelectionTypeContext> Types { get; private set; }
}

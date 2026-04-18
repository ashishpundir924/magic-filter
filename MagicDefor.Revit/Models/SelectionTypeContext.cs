namespace MagicDefor.Revit.Models;

internal sealed class SelectionTypeContext
{
    public SelectionTypeContext(string typeName, IEnumerable<string> parameterNames)
    {
        TypeName = typeName;
        ParameterNames = parameterNames.OrderBy(name => name).ToList();
    }

    public string TypeName { get; private set; }

    public List<string> ParameterNames { get; private set; }
}

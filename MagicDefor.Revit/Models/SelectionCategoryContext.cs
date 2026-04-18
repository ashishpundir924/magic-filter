namespace MagicDefor.Revit.Models;

internal sealed class SelectionCategoryContext
{
    public SelectionCategoryContext(string categoryName, IEnumerable<string> parameterNames, IEnumerable<SelectionFamilyContext> families)
    {
        CategoryName = categoryName;
        ParameterNames = parameterNames.OrderBy(name => name).ToList();
        Families = families.OrderBy(family => family.FamilyName).ToList();
    }

    public string CategoryName { get; private set; }

    public List<string> ParameterNames { get; private set; }

    public List<SelectionFamilyContext> Families { get; private set; }
}

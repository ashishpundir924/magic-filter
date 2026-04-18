namespace MagicDefor.Revit.Models;

internal sealed class SelectionSnapshot
{
    public SelectionSnapshot(string documentTitle, int sourceElementCount, IEnumerable<SelectionCategoryContext> categories)
    {
        DocumentTitle = documentTitle;
        SourceElementCount = sourceElementCount;
        Categories = categories.OrderBy(category => category.CategoryName).ToList();
    }

    public string DocumentTitle { get; private set; }

    public int SourceElementCount { get; private set; }

    public List<SelectionCategoryContext> Categories { get; private set; }
}

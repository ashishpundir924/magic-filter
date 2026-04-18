using Autodesk.Revit.DB;
using MagicDefor.Revit.Models;

namespace MagicDefor.Revit.Infrastructure;

internal static class SelectionSnapshotFactory
{
    public static SelectionSnapshot Create(Document document, ICollection<ElementId> selectedIds)
    {
        var categories = new Dictionary<string, CategoryBuilder>(StringComparer.OrdinalIgnoreCase);

        foreach (var elementId in selectedIds)
        {
            var element = document.GetElement(elementId);
            var categoryName = element != null && element.Category != null ? element.Category.Name : null;
            if (element == null || string.IsNullOrWhiteSpace(categoryName))
            {
                continue;
            }

            var typeElement = GetElementType(document, element);
            var familyName = GetFamilyName(element, typeElement, categoryName);
            var typeName = GetTypeName(element, typeElement);

            CategoryBuilder categoryBuilder;
            if (!categories.TryGetValue(categoryName, out categoryBuilder))
            {
                categoryBuilder = new CategoryBuilder();
                categories.Add(categoryName, categoryBuilder);
            }

            categoryBuilder.ParameterNames.Add("Name");
            var familyBuilder = categoryBuilder.GetOrCreateFamily(familyName);
            familyBuilder.ParameterNames.Add("Name");
            var typeBuilder = familyBuilder.GetOrCreateType(typeName);
            typeBuilder.ParameterNames.Add("Name");

            foreach (Parameter parameter in element.Parameters)
            {
                var parameterName = parameter.Definition != null ? parameter.Definition.Name : null;
                if (string.IsNullOrWhiteSpace(parameterName))
                {
                    continue;
                }

                categoryBuilder.ParameterNames.Add(parameterName);
                familyBuilder.ParameterNames.Add(parameterName);
                typeBuilder.ParameterNames.Add(parameterName);
            }
        }

        var contexts = categories
            .OrderBy(pair => pair.Key)
            .Select(pair => new SelectionCategoryContext(
                pair.Key,
                pair.Value.ParameterNames,
                pair.Value.Families
                    .OrderBy(family => family.Key)
                    .Select(family => new SelectionFamilyContext(
                        family.Key,
                        family.Value.ParameterNames,
                        family.Value.Types
                            .OrderBy(type => type.Key)
                            .Select(type => new SelectionTypeContext(type.Key, type.Value.ParameterNames)))
                    )))
            .ToList();

        return new SelectionSnapshot(document.Title, selectedIds.Count, contexts);
    }

    private static ElementType GetElementType(Document document, Element element)
    {
        var typeId = element.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId)
        {
            return null;
        }

        return document.GetElement(typeId) as ElementType;
    }

    private static string GetFamilyName(Element element, ElementType typeElement, string categoryName)
    {
        if (typeElement != null && !string.IsNullOrWhiteSpace(typeElement.FamilyName))
        {
            return typeElement.FamilyName;
        }

        return categoryName;
    }

    private static string GetTypeName(Element element, ElementType typeElement)
    {
        if (typeElement != null && !string.IsNullOrWhiteSpace(typeElement.Name))
        {
            return typeElement.Name;
        }

        return string.IsNullOrWhiteSpace(element.Name) ? "<Unnamed>" : element.Name;
    }

    private sealed class CategoryBuilder
    {
        public CategoryBuilder()
        {
            ParameterNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            Families = new Dictionary<string, FamilyBuilder>(StringComparer.OrdinalIgnoreCase);
        }

        public SortedSet<string> ParameterNames { get; private set; }

        public Dictionary<string, FamilyBuilder> Families { get; private set; }

        public FamilyBuilder GetOrCreateFamily(string familyName)
        {
            FamilyBuilder familyBuilder;
            if (!Families.TryGetValue(familyName, out familyBuilder))
            {
                familyBuilder = new FamilyBuilder();
                Families.Add(familyName, familyBuilder);
            }

            return familyBuilder;
        }
    }

    private sealed class FamilyBuilder
    {
        public FamilyBuilder()
        {
            ParameterNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            Types = new Dictionary<string, TypeBuilder>(StringComparer.OrdinalIgnoreCase);
        }

        public SortedSet<string> ParameterNames { get; private set; }

        public Dictionary<string, TypeBuilder> Types { get; private set; }

        public TypeBuilder GetOrCreateType(string typeName)
        {
            TypeBuilder typeBuilder;
            if (!Types.TryGetValue(typeName, out typeBuilder))
            {
                typeBuilder = new TypeBuilder();
                Types.Add(typeName, typeBuilder);
            }

            return typeBuilder;
        }
    }

    private sealed class TypeBuilder
    {
        public TypeBuilder()
        {
            ParameterNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public SortedSet<string> ParameterNames { get; private set; }
    }
}

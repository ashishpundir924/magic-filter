using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MagicDefor.Revit.Infrastructure;
using MagicDefor.Revit.Models;

namespace MagicDefor.Revit.Services;

internal sealed class RevitFilterService
{
    public void Execute(UIApplication uiApplication, LiveFilterRequest request)
    {
        var uiDocument = uiApplication.ActiveUIDocument;
        var document = uiDocument != null ? uiDocument.Document : null;
        var activeView = document != null ? document.ActiveView : null;

        if (uiDocument == null || document == null || activeView == null)
        {
            return;
        }

        switch (request.Action)
        {
            case LiveFilterAction.Refresh:
            case LiveFilterAction.Select:
                uiDocument.Selection.SetElementIds(FindMatchingElementIds(document, activeView, request));
                break;
            case LiveFilterAction.Isolate:
                var isolateIds = FindMatchingElementIds(document, activeView, request);
                if (isolateIds.Count == 0)
                {
                    TaskDialog.Show("DEFOR Tools", "No matching elements were found.");
                    return;
                }

                using (var transaction = new Transaction(document, "DEFOR isolate filtered elements"))
                {
                    transaction.Start();
                    activeView.IsolateElementsTemporary(isolateIds);
                    transaction.Commit();
                }
                uiDocument.Selection.SetElementIds(isolateIds);
                break;
            case LiveFilterAction.Clear:
                uiDocument.Selection.SetElementIds(new List<ElementId>());
                if (activeView.IsTemporaryHideIsolateActive())
                {
                    activeView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                }
                break;
        }
    }

    private static IList<ElementId> FindMatchingElementIds(Document document, View activeView, LiveFilterRequest request)
    {
        var collector = request.LimitToActiveView
            ? new FilteredElementCollector(document, activeView.Id)
            : new FilteredElementCollector(document);

        if (!request.IncludeElementTypes)
        {
            collector = collector.WhereElementIsNotElementType();
        }

        var selectedScopes = request.SelectedScopes ?? new List<SelectionScopeDefinition>();
        var useScopeFilter = selectedScopes.Count > 0;
        var matches = new List<ElementId>();

        foreach (var element in collector)
        {
            if (useScopeFilter && !MatchesAnyScope(document, element, selectedScopes))
            {
                continue;
            }

            if (MatchesRules(element, request))
            {
                matches.Add(element.Id);
            }
        }

        return matches;
    }

    private static bool MatchesRules(Element element, LiveFilterRequest request)
    {
        if (request.Rules == null || request.Rules.Count == 0)
        {
            return true;
        }

        var activeRules = request.Rules.Where(rule => !string.IsNullOrWhiteSpace(rule.Value)).ToList();
        if (activeRules.Count == 0)
        {
            return true;
        }

        var result = MatchesRule(element, activeRules[0]);

        for (var index = 1; index < activeRules.Count; index++)
        {
            var rule = activeRules[index];
            var ruleMatch = MatchesRule(element, rule);
            result = rule.JoinWithPrevious == LogicGate.And
                ? result && ruleMatch
                : result || ruleMatch;
        }

        return result;
    }

    private static bool MatchesAnyScope(Document document, Element element, IEnumerable<SelectionScopeDefinition> scopes)
    {
        var categoryName = element.Category != null ? element.Category.Name : string.Empty;
        var typeElement = GetElementType(document, element);
        var familyName = GetFamilyName(element, typeElement, categoryName);
        var typeName = GetTypeName(element, typeElement);

        return scopes.Any(scope =>
            string.Equals(scope.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(scope.FamilyName) || string.Equals(scope.FamilyName, familyName, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(scope.TypeName) || string.Equals(scope.TypeName, typeName, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool MatchesRule(Element element, FilterRuleDefinition rule)
    {
        var comparison = rule.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (string.Equals(rule.ParameterName, "Name", StringComparison.OrdinalIgnoreCase))
        {
            return MatchesValue(element.Name ?? string.Empty, rule, comparison);
        }

        var parameter = element.LookupParameter(rule.ParameterName);
        if (parameter == null)
        {
            return false;
        }

        if (rule.Operator == FilterOperator.GreaterThan || rule.Operator == FilterOperator.LessThan)
        {
            double target;
            if (!double.TryParse(rule.Value, out target))
            {
                return false;
            }

            var numeric = GetParameterNumber(parameter);
            if (!numeric.HasValue)
            {
                return false;
            }

            return rule.Operator == FilterOperator.GreaterThan
                ? numeric.Value > target
                : numeric.Value < target;
        }

        return MatchesValue(GetParameterText(parameter), rule, comparison);
    }

    private static bool MatchesValue(string source, FilterRuleDefinition rule, StringComparison comparison)
    {
        source = source ?? string.Empty;

        switch (rule.Operator)
        {
            case FilterOperator.Contains:
                return source.IndexOf(rule.Value, comparison) >= 0;
            case FilterOperator.Equals:
                return string.Equals(source, rule.Value, comparison);
            case FilterOperator.StartsWith:
                return source.StartsWith(rule.Value, comparison);
            case FilterOperator.EndsWith:
                return source.EndsWith(rule.Value, comparison);
            case FilterOperator.NotContains:
                return source.IndexOf(rule.Value, comparison) < 0;
            default:
                return false;
        }
    }

    private static string GetParameterText(Parameter parameter)
    {
        switch (parameter.StorageType)
        {
            case StorageType.String:
                return parameter.AsString() ?? string.Empty;
            case StorageType.Integer:
                return parameter.AsValueString() ?? parameter.AsInteger().ToString();
            case StorageType.Double:
                return parameter.AsValueString() ?? parameter.AsDouble().ToString("G");
            case StorageType.ElementId:
                return parameter.AsValueString() ?? parameter.AsElementId().ToString();
            default:
                return parameter.AsValueString() ?? string.Empty;
        }
    }

    private static double? GetParameterNumber(Parameter parameter)
    {
        switch (parameter.StorageType)
        {
            case StorageType.Double:
                return parameter.AsDouble();
            case StorageType.Integer:
                return parameter.AsInteger();
            default:
                return null;
        }
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
}

namespace MagicDefor.Revit.Models;

public sealed class FilterRuleDefinition
{
    public LogicGate JoinWithPrevious { get; set; } = LogicGate.Or;

    public string ParameterName { get; set; } = "Name";

    public FilterOperator Operator { get; set; } = FilterOperator.Contains;

    public string Value { get; set; } = string.Empty;

    public bool MatchCase { get; set; }
}

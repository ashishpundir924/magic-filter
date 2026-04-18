using MagicDefor.Revit.Infrastructure;
using MagicDefor.Revit.Models;

namespace MagicDefor.Revit.ViewModels;

internal sealed class FilterRuleViewModel : ObservableObject
{
    private bool _showJoinWithPrevious;
    private LogicGate _joinWithPrevious = LogicGate.Or;
    private string _parameterName = "Name";
    private FilterOperator _selectedOperator = FilterOperator.Contains;
    private string _value = string.Empty;
    private bool _matchCase;

    public FilterRuleViewModel(Action queueRefresh)
    {
        QueueRefresh = queueRefresh;
        ParameterOptions = new List<string>();
        OperatorOptions = Enum.GetValues(typeof(FilterOperator)).Cast<FilterOperator>().ToList();
        LogicOptions = Enum.GetValues(typeof(LogicGate)).Cast<LogicGate>().ToList();
    }

    public Action QueueRefresh { get; private set; }

    public List<string> ParameterOptions { get; private set; }

    public IReadOnlyList<FilterOperator> OperatorOptions { get; private set; }

    public IReadOnlyList<LogicGate> LogicOptions { get; private set; }

    public bool ShowJoinWithPrevious
    {
        get { return _showJoinWithPrevious; }
        set { SetProperty(ref _showJoinWithPrevious, value); }
    }

    public LogicGate JoinWithPrevious
    {
        get { return _joinWithPrevious; }
        set
        {
            if (SetProperty(ref _joinWithPrevious, value))
            {
                QueueRefresh();
            }
        }
    }

    public string ParameterName
    {
        get { return _parameterName; }
        set
        {
            if (SetProperty(ref _parameterName, value))
            {
                QueueRefresh();
            }
        }
    }

    public FilterOperator SelectedOperator
    {
        get { return _selectedOperator; }
        set
        {
            if (SetProperty(ref _selectedOperator, value))
            {
                QueueRefresh();
            }
        }
    }

    public string Value
    {
        get { return _value; }
        set
        {
            if (SetProperty(ref _value, value))
            {
                QueueRefresh();
            }
        }
    }

    public bool MatchCase
    {
        get { return _matchCase; }
        set
        {
            if (SetProperty(ref _matchCase, value))
            {
                QueueRefresh();
            }
        }
    }

    public void UpdateParameterOptions(IEnumerable<string> parameterNames)
    {
        ParameterOptions = parameterNames.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name).ToList();
        if (!ParameterOptions.Contains(ParameterName))
        {
            ParameterName = ParameterOptions.FirstOrDefault() ?? "Name";
        }

        OnPropertyChanged("ParameterOptions");
    }

    public FilterRuleDefinition ToDefinition()
    {
        return new FilterRuleDefinition
        {
            JoinWithPrevious = JoinWithPrevious,
            ParameterName = ParameterName,
            Operator = SelectedOperator,
            Value = Value,
            MatchCase = MatchCase
        };
    }

    public void Apply(FilterRuleDefinition definition)
    {
        JoinWithPrevious = definition.JoinWithPrevious;
        ParameterName = definition.ParameterName;
        SelectedOperator = definition.Operator;
        Value = definition.Value;
        MatchCase = definition.MatchCase;
    }
}

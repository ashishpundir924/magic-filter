using System.Collections.ObjectModel;
using System.Windows.Threading;
using MagicDefor.Revit.Infrastructure;
using MagicDefor.Revit.Models;
using MagicDefor.Revit.Services;

namespace MagicDefor.Revit.ViewModels;

internal sealed class LiveFilterViewModel : ObservableObject, IDisposable
{
    private readonly Action<LiveFilterRequest> _requestSink;
    private readonly Action<Action<SelectionSnapshot>> _selectionRefresh;
    private readonly FilterConfigurationRepository _configurationRepository;
    private readonly DispatcherTimer _debounceTimer;
    private readonly List<SavedFilterConfiguration> _savedConfigurations;

    private SelectionSnapshot _snapshot;
    private bool _includeElementTypes;
    private bool _limitToActiveView = true;
    private string _statusText;
    private string _configurationName = string.Empty;
    private string _selectedConfigurationName = string.Empty;
    private string _scopeSearchText = string.Empty;
    private bool _suspendRefresh;

    public LiveFilterViewModel(
        SelectionSnapshot snapshot,
        Action<LiveFilterRequest> requestSink,
        Action<Action<SelectionSnapshot>> selectionRefresh,
        FilterConfigurationRepository configurationRepository)
    {
        _snapshot = snapshot;
        _requestSink = requestSink;
        _selectionRefresh = selectionRefresh;
        _configurationRepository = configurationRepository;
        _savedConfigurations = _configurationRepository.LoadAll();
        _statusText = BuildSelectionStatus(snapshot);

        ScopeTree = new ObservableCollection<SelectionScopeNodeViewModel>();
        Rules = new ObservableCollection<FilterRuleViewModel>();
        SavedConfigurationNames = new ObservableCollection<string>(_savedConfigurations.Select(configuration => configuration.Name));

        RefreshSelectionCommand = new RelayCommand(RefreshSelectionFromRevit);
        SelectAllScopesCommand = new RelayCommand(SelectAllScopes);
        ClearAllScopesCommand = new RelayCommand(ClearAllScopes);
        ExpandAllScopesCommand = new RelayCommand(ExpandAllScopes);
        CollapseAllScopesCommand = new RelayCommand(CollapseAllScopes);
        AddRuleCommand = new RelayCommand(AddRule);
        RemoveRuleCommand = new RelayCommand(RemoveLastRule, delegate { return Rules.Count > 1; });
        ApplyCommand = new RelayCommand(delegate { Send(LiveFilterAction.Refresh); });
        SelectCommand = new RelayCommand(delegate { Send(LiveFilterAction.Select); });
        IsolateCommand = new RelayCommand(delegate { Send(LiveFilterAction.Isolate); });
        ClearCommand = new RelayCommand(Clear);
        SaveConfigurationCommand = new RelayCommand(SaveConfiguration);
        LoadConfigurationCommand = new RelayCommand(LoadConfiguration, delegate { return !string.IsNullOrWhiteSpace(SelectedConfigurationName); });
        DeleteConfigurationCommand = new RelayCommand(DeleteConfiguration, delegate { return !string.IsNullOrWhiteSpace(SelectedConfigurationName); });

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        _debounceTimer.Tick += delegate
        {
            _debounceTimer.Stop();
            Send(LiveFilterAction.Refresh);
        };

        RebuildFromSnapshot(snapshot);
    }

    public ObservableCollection<SelectionScopeNodeViewModel> ScopeTree { get; private set; }

    public ObservableCollection<FilterRuleViewModel> Rules { get; private set; }

    public ObservableCollection<string> SavedConfigurationNames { get; private set; }

    public RelayCommand RefreshSelectionCommand { get; private set; }

    public RelayCommand SelectAllScopesCommand { get; private set; }

    public RelayCommand ClearAllScopesCommand { get; private set; }

    public RelayCommand ExpandAllScopesCommand { get; private set; }

    public RelayCommand CollapseAllScopesCommand { get; private set; }

    public RelayCommand AddRuleCommand { get; private set; }

    public RelayCommand RemoveRuleCommand { get; private set; }

    public RelayCommand ApplyCommand { get; private set; }

    public RelayCommand SelectCommand { get; private set; }

    public RelayCommand IsolateCommand { get; private set; }

    public RelayCommand ClearCommand { get; private set; }

    public RelayCommand SaveConfigurationCommand { get; private set; }

    public RelayCommand LoadConfigurationCommand { get; private set; }

    public RelayCommand DeleteConfigurationCommand { get; private set; }

    public bool IncludeElementTypes
    {
        get { return _includeElementTypes; }
        set
        {
            if (SetProperty(ref _includeElementTypes, value))
            {
                QueueRefresh();
            }
        }
    }

    public bool LimitToActiveView
    {
        get { return _limitToActiveView; }
        set
        {
            if (SetProperty(ref _limitToActiveView, value))
            {
                QueueRefresh();
            }
        }
    }

    public string StatusText
    {
        get { return _statusText; }
        set { SetProperty(ref _statusText, value); }
    }

    public string ConfigurationName
    {
        get { return _configurationName; }
        set { SetProperty(ref _configurationName, value); }
    }

    public string SelectedConfigurationName
    {
        get { return _selectedConfigurationName; }
        set
        {
            if (SetProperty(ref _selectedConfigurationName, value))
            {
                LoadConfigurationCommand.RaiseCanExecuteChanged();
                DeleteConfigurationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ScopeSearchText
    {
        get { return _scopeSearchText; }
        set
        {
            if (SetProperty(ref _scopeSearchText, value))
            {
                ApplyScopeSearch();
            }
        }
    }

    public void QueueRefresh()
    {
        if (_suspendRefresh || _snapshot.SourceElementCount == 0)
        {
            return;
        }

        StatusText = "Refreshing live filter...";
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    public void Dispose()
    {
        _debounceTimer.Stop();
    }

    private void RefreshSelectionFromRevit()
    {
        StatusText = "Reading selected elements...";
        _selectionRefresh(UpdateSelectionSnapshot);
    }

    private void UpdateSelectionSnapshot(SelectionSnapshot snapshot)
    {
        RebuildFromSnapshot(snapshot);
        StatusText = BuildSelectionStatus(snapshot);
        QueueRefresh();
    }

    private void RebuildFromSnapshot(SelectionSnapshot snapshot)
    {
        _snapshot = snapshot;
        _suspendRefresh = true;

        ScopeTree.Clear();
        foreach (var category in snapshot.Categories)
        {
            var categoryNode = new SelectionScopeNodeViewModel(
                category.CategoryName,
                new SelectionScopeDefinition { CategoryName = category.CategoryName },
                OnScopeSelectionChanged);

            foreach (var family in category.Families)
            {
                var familyNode = new SelectionScopeNodeViewModel(
                    family.FamilyName,
                    new SelectionScopeDefinition { CategoryName = category.CategoryName, FamilyName = family.FamilyName },
                    OnScopeSelectionChanged);

                foreach (var type in family.Types)
                {
                    familyNode.AddChild(new SelectionScopeNodeViewModel(
                        type.TypeName,
                        new SelectionScopeDefinition { CategoryName = category.CategoryName, FamilyName = family.FamilyName, TypeName = type.TypeName },
                        OnScopeSelectionChanged));
                }

                categoryNode.AddChild(familyNode);
            }

            categoryNode.IsChecked = true;
            ScopeTree.Add(categoryNode);
        }

        if (Rules.Count == 0)
        {
            AddRule();
        }

        UpdateRuleParameters();
        UpdateRuleSequence();
        ApplyScopeSearch();
        _suspendRefresh = false;
        RemoveRuleCommand.RaiseCanExecuteChanged();
    }

    private void OnScopeSelectionChanged()
    {
        UpdateRuleParameters();
        QueueRefresh();
    }

    private void SelectAllScopes()
    {
        _suspendRefresh = true;
        foreach (var node in ScopeTree)
        {
            node.SetCheckedRecursive(true);
        }

        _suspendRefresh = false;
        UpdateRuleParameters();
        QueueRefresh();
    }

    private void ClearAllScopes()
    {
        _suspendRefresh = true;
        foreach (var node in ScopeTree)
        {
            node.SetCheckedRecursive(false);
        }

        _suspendRefresh = false;
        UpdateRuleParameters();
        QueueRefresh();
    }

    private void ExpandAllScopes()
    {
        foreach (var node in ScopeTree)
        {
            node.SetExpandedRecursive(true);
        }
    }

    private void CollapseAllScopes()
    {
        foreach (var node in ScopeTree)
        {
            node.SetExpandedRecursive(false);
        }
    }

    private void ApplyScopeSearch()
    {
        foreach (var node in ScopeTree)
        {
            node.ApplySearchFilter(ScopeSearchText);
        }
    }

    private void UpdateRuleParameters()
    {
        var parameterNames = GetActiveParameterNames().ToList();

        foreach (var rule in Rules)
        {
            rule.UpdateParameterOptions(parameterNames);
        }
    }

    private IEnumerable<string> GetActiveParameterNames()
    {
        var selectedNodes = ScopeTree.SelectMany(node => node.GetSelectedScopes()).ToList();
        if (selectedNodes.Count == 0)
        {
            return _snapshot.Categories.SelectMany(category => category.ParameterNames).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        return ScopeTree
            .SelectMany(node => node.CollectParameterNames(_snapshot))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name);
    }

    private void AddRule()
    {
        var rule = new FilterRuleViewModel(QueueRefresh)
        {
            JoinWithPrevious = Rules.Count == 0 ? LogicGate.Or : LogicGate.Or
        };

        rule.UpdateParameterOptions(GetActiveParameterNames());
        Rules.Add(rule);
        UpdateRuleSequence();
        RemoveRuleCommand.RaiseCanExecuteChanged();
        QueueRefresh();
    }

    private void RemoveLastRule()
    {
        if (Rules.Count <= 1)
        {
            return;
        }

        Rules.RemoveAt(Rules.Count - 1);
        UpdateRuleSequence();
        RemoveRuleCommand.RaiseCanExecuteChanged();
        QueueRefresh();
    }

    private void SaveConfiguration()
    {
        var name = string.IsNullOrWhiteSpace(ConfigurationName) ? "Default" : ConfigurationName.Trim();
        var existing = _savedConfigurations.FirstOrDefault(configuration => string.Equals(configuration.Name, name, StringComparison.OrdinalIgnoreCase));
        var replacement = BuildConfiguration(name);

        if (existing != null)
        {
            _savedConfigurations.Remove(existing);
        }

        _savedConfigurations.Add(replacement);
        _configurationRepository.SaveAll(_savedConfigurations);
        RefreshSavedConfigurationNames(name);
        StatusText = "Configuration saved.";
    }

    private void LoadConfiguration()
    {
        var configuration = _savedConfigurations.FirstOrDefault(item => string.Equals(item.Name, SelectedConfigurationName, StringComparison.OrdinalIgnoreCase));
        if (configuration == null)
        {
            return;
        }

        _suspendRefresh = true;
        IncludeElementTypes = configuration.IncludeElementTypes;
        LimitToActiveView = configuration.LimitToActiveView;

        var selectedKeys = new HashSet<string>(configuration.ScopeKeys ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        foreach (var node in ScopeTree)
        {
            node.ApplySelectedScopes(selectedKeys);
        }

        Rules.Clear();
        foreach (var ruleDefinition in configuration.Rules)
        {
            var rule = new FilterRuleViewModel(QueueRefresh);
            rule.UpdateParameterOptions(GetActiveParameterNames());
            rule.Apply(ruleDefinition);
            Rules.Add(rule);
        }

        if (Rules.Count == 0)
        {
            AddRule();
        }

        UpdateRuleParameters();
        UpdateRuleSequence();
        ConfigurationName = configuration.Name;
        _suspendRefresh = false;
        RemoveRuleCommand.RaiseCanExecuteChanged();
        QueueRefresh();
        StatusText = "Configuration loaded.";
    }

    private void DeleteConfiguration()
    {
        var configuration = _savedConfigurations.FirstOrDefault(item => string.Equals(item.Name, SelectedConfigurationName, StringComparison.OrdinalIgnoreCase));
        if (configuration == null)
        {
            return;
        }

        _savedConfigurations.Remove(configuration);
        _configurationRepository.SaveAll(_savedConfigurations);
        RefreshSavedConfigurationNames(string.Empty);
        StatusText = "Configuration deleted.";
    }

    private void RefreshSavedConfigurationNames(string selectedName)
    {
        SavedConfigurationNames.Clear();
        foreach (var name in _savedConfigurations.Select(configuration => configuration.Name).OrderBy(name => name))
        {
            SavedConfigurationNames.Add(name);
        }

        SelectedConfigurationName = selectedName;
    }

    private SavedFilterConfiguration BuildConfiguration(string name)
    {
        return new SavedFilterConfiguration
        {
            Name = name,
            ScopeKeys = ScopeTree.SelectMany(node => node.GetSelectedScopes()).Select(scope => scope.Key).ToList(),
            IncludeElementTypes = IncludeElementTypes,
            LimitToActiveView = LimitToActiveView,
            Rules = Rules.Select(rule => rule.ToDefinition()).ToList()
        };
    }

    private void Clear()
    {
        foreach (var rule in Rules)
        {
            rule.Value = string.Empty;
        }

        StatusText = "Selection and temporary isolate cleared.";
        Send(LiveFilterAction.Clear);
    }

    private void Send(LiveFilterAction action)
    {
        if (_snapshot.SourceElementCount == 0)
        {
            StatusText = "Select source elements first, then click Read Selection.";
            return;
        }

        var rules = Rules.Select(rule => rule.ToDefinition()).Where(rule => !string.IsNullOrWhiteSpace(rule.ParameterName)).ToList();

        _requestSink(new LiveFilterRequest
        {
            Action = action,
            SelectedScopes = ScopeTree.SelectMany(node => node.GetSelectedScopes()).ToList(),
            Rules = rules,
            IncludeElementTypes = IncludeElementTypes,
            LimitToActiveView = LimitToActiveView
        });

        StatusText = action == LiveFilterAction.Isolate
            ? "Applying isolate..."
            : action == LiveFilterAction.Select
                ? "Selecting matches..."
                : action == LiveFilterAction.Clear
                    ? "Clearing result..."
                    : "Refreshing live filter...";
    }

    private static string BuildSelectionStatus(SelectionSnapshot snapshot)
    {
        return snapshot.SourceElementCount == 0
            ? "Select one or more source elements in Revit, then click Read Selection."
            : string.Format("Source: {0} selected element(s) in {1}", snapshot.SourceElementCount, snapshot.DocumentTitle);
    }

    private void UpdateRuleSequence()
    {
        for (var index = 0; index < Rules.Count; index++)
        {
            Rules[index].ShowJoinWithPrevious = index > 0;
        }
    }
}

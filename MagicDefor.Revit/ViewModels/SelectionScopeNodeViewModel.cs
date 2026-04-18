using System.Collections.ObjectModel;
using MagicDefor.Revit.Infrastructure;
using MagicDefor.Revit.Models;

namespace MagicDefor.Revit.ViewModels;

internal sealed class SelectionScopeNodeViewModel : ObservableObject
{
    private readonly Action _selectionChanged;
    private bool? _isChecked;
    private bool _isExpanded;
    private bool _isUpdatingChildren;
    private bool _isVisible = true;

    public SelectionScopeNodeViewModel(string displayName, SelectionScopeDefinition scope, Action selectionChanged)
    {
        DisplayName = displayName;
        Scope = scope;
        _selectionChanged = selectionChanged;
        Children = new ObservableCollection<SelectionScopeNodeViewModel>();
        _isExpanded = true;
    }

    public string DisplayName { get; private set; }

    public SelectionScopeDefinition Scope { get; private set; }

    public ObservableCollection<SelectionScopeNodeViewModel> Children { get; private set; }

    public SelectionScopeNodeViewModel Parent { get; private set; }

    public bool HasChildren
    {
        get { return Children.Count > 0; }
    }

    public bool IsExpanded
    {
        get { return _isExpanded; }
        set { SetProperty(ref _isExpanded, value); }
    }

    public bool IsVisible
    {
        get { return _isVisible; }
        set { SetProperty(ref _isVisible, value); }
    }

    public bool? IsChecked
    {
        get { return _isChecked; }
        set
        {
            if (!SetProperty(ref _isChecked, value))
            {
                return;
            }

            if (!_isUpdatingChildren)
            {
                UpdateChildren(value);
                if (Parent != null)
                {
                    Parent.RefreshFromChildren();
                }

                _selectionChanged();
            }
        }
    }

    public void AddChild(SelectionScopeNodeViewModel child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public IEnumerable<SelectionScopeDefinition> GetSelectedScopes()
    {
        if (IsChecked == true)
        {
            yield return Scope;
            yield break;
        }

        foreach (var child in Children)
        {
            foreach (var scope in child.GetSelectedScopes())
            {
                yield return scope;
            }
        }
    }

    public void ApplySelectedScopes(ISet<string> selectedKeys)
    {
        _isUpdatingChildren = true;

        foreach (var child in Children)
        {
            child.ApplySelectedScopes(selectedKeys);
        }

        if (selectedKeys.Contains(Scope.Key))
        {
            _isChecked = true;
            OnPropertyChanged("IsChecked");
            UpdateChildren(true);
        }
        else if (Children.Count == 0)
        {
            _isChecked = false;
            OnPropertyChanged("IsChecked");
        }
        else
        {
            RefreshFromChildrenCore();
        }

        _isUpdatingChildren = false;
    }

    public IEnumerable<string> CollectParameterNames(SelectionSnapshot snapshot)
    {
        if (!HasChildren)
        {
            return FindMatchingParameters(snapshot);
        }

        if (IsChecked == true)
        {
            return FindMatchingParameters(snapshot);
        }

        return Children.SelectMany(child => child.CollectParameterNames(snapshot)).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public void SetCheckedRecursive(bool isChecked)
    {
        _isUpdatingChildren = true;
        IsChecked = isChecked;
        foreach (var child in Children)
        {
            child.SetCheckedRecursive(isChecked);
        }

        _isUpdatingChildren = false;
        OnPropertyChanged("IsChecked");
    }

    public void SetExpandedRecursive(bool isExpanded)
    {
        IsExpanded = isExpanded;
        foreach (var child in Children)
        {
            child.SetExpandedRecursive(isExpanded);
        }
    }

    public bool ApplySearchFilter(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            IsVisible = true;
            foreach (var child in Children)
            {
                child.ApplySearchFilter(searchText);
            }

            return true;
        }

        var selfMatches = DisplayName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        var childMatches = false;

        foreach (var child in Children)
        {
            if (child.ApplySearchFilter(searchText))
            {
                childMatches = true;
            }
        }

        IsVisible = selfMatches || childMatches;
        if (childMatches)
        {
            IsExpanded = true;
        }

        return IsVisible;
    }

    private void UpdateChildren(bool? value)
    {
        if (Children.Count == 0)
        {
            return;
        }

        foreach (var child in Children)
        {
            child._isUpdatingChildren = true;
            child.IsChecked = value;
            child._isUpdatingChildren = false;
        }
    }

    private void RefreshFromChildren()
    {
        RefreshFromChildrenCore();
        if (Parent != null)
        {
            Parent.RefreshFromChildren();
        }
    }

    private void RefreshFromChildrenCore()
    {
        if (Children.Count == 0)
        {
            return;
        }

        var childStates = Children.Select(child => child.IsChecked).Distinct().ToList();
        bool? newValue = childStates.Count == 1 ? childStates[0] : null;
        _isChecked = newValue;
        OnPropertyChanged("IsChecked");
    }

    private IEnumerable<string> FindMatchingParameters(SelectionSnapshot snapshot)
    {
        var category = snapshot.Categories.FirstOrDefault(item => string.Equals(item.CategoryName, Scope.CategoryName, StringComparison.OrdinalIgnoreCase));
        if (category == null)
        {
            return Enumerable.Empty<string>();
        }

        if (string.IsNullOrWhiteSpace(Scope.FamilyName))
        {
            return category.ParameterNames;
        }

        var family = category.Families.FirstOrDefault(item => string.Equals(item.FamilyName, Scope.FamilyName, StringComparison.OrdinalIgnoreCase));
        if (family == null)
        {
            return Enumerable.Empty<string>();
        }

        if (string.IsNullOrWhiteSpace(Scope.TypeName))
        {
            return family.ParameterNames;
        }

        var type = family.Types.FirstOrDefault(item => string.Equals(item.TypeName, Scope.TypeName, StringComparison.OrdinalIgnoreCase));
        return type != null ? type.ParameterNames : Enumerable.Empty<string>();
    }
}

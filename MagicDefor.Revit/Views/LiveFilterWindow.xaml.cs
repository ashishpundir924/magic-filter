using System.Windows;
using MagicDefor.Revit.ViewModels;

namespace MagicDefor.Revit.Views;

internal partial class LiveFilterWindow : Window
{
    internal LiveFilterWindow(LiveFilterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

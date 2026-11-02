using System.Windows;
using Cantina.Wpf.ViewModels;

namespace Cantina.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainDashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}


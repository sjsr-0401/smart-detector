using System.Windows;
using SmartDetector.ViewModels;

namespace SmartDetector;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => (DataContext as MainViewModel)?.Dispose();
    }
}

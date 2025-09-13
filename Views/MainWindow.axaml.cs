using Avalonia.Controls;
using DialogueCalendarApp.ViewModels;

namespace DialogueCalendarApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(); // <- ViewModel 연결
    }
}

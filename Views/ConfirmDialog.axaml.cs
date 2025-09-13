using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DialogueCalendarApp.Views;

public partial class ConfirmDialog : Window
{
    public bool Result { get; private set; } = false;

    public ConfirmDialog(string message)
    {
        InitializeComponent();
        this.FindControl<TextBlock>("MessageText").Text = message;
    }

    private void Yes_Click(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void No_Click(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}

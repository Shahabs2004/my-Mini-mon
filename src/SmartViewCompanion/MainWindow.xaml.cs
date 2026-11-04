using System.Windows;
using System.Windows.Controls;

namespace SmartViewCompanion;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var capture = (CaptureMethodBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Automatic";
        var tracking = (TrackingModeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Fixed region";
        StatusText.Text = $"Configuration selected: {capture} + {tracking}. The selected capture backend is not yet fully connected in this prototype.";
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Status: Preview stopped.";
    }
}

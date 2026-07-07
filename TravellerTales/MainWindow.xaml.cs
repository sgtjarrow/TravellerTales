using System.Windows;

namespace TravellerTales;

public partial class MainWindow : Window
{
    public string DataLocationText { get; }

    public MainWindow()
    {
        DataLocationText = $"Characters will be stored locally at {AppPaths.CharactersDirectory}.";
        DataContext = this;
        InitializeComponent();
    }

    private void OnPlaceholderAction(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "Character editing and JSON import/export will be implemented in the next scope of work.",
            "Traveller Tales",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}

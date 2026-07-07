using System.Windows;

namespace TravellerTales;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnNewCharacter(object sender, RoutedEventArgs e)
    {
        ShowPlaceholder(
            "New Character Wizard",
            "This screen will guide new character creation in a future scope.");
    }

    private void OnEditNarratives(object sender, RoutedEventArgs e)
    {
        ShowPlaceholder(
            "Edit Narratives",
            "This screen will list saved characters so narrative details can be added or edited.");
    }

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        ShowPlaceholder(
            "Settings",
            "This screen will contain Traveller Tales application settings.");
    }

    private void OnCredits(object sender, RoutedEventArgs e)
    {
        ShowPlaceholder(
            "Credits",
            "Traveller Tales credits and about information will appear here.");
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnBackToLanding(object sender, RoutedEventArgs e)
    {
        PlaceholderView.Visibility = Visibility.Collapsed;
        LandingView.Visibility = Visibility.Visible;
    }

    private void ShowPlaceholder(string title, string body)
    {
        PlaceholderTitle.Text = title;
        PlaceholderBody.Text = body;
        LandingView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Visible;
    }
}

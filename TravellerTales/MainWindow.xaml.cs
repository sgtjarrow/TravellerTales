using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TravellerTales;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private AppSettings _settings;
    private int _loadedSplashDurationSeconds;
    private string _loadedApplicationVersion = string.Empty;
    private string _versionText;
    private bool _isLoadingSettings;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string VersionText
    {
        get => _versionText;
        private set
        {
            if (_versionText == value)
            {
                return;
            }

            _versionText = value;
            OnPropertyChanged();
        }
    }

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        _versionText = GetVersionText(settings.ApplicationVersion);
        DataContext = this;
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
        ShowSettings();
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
        ShowLanding();
    }

    private void ShowPlaceholder(string title, string body)
    {
        PlaceholderTitle.Text = title;
        PlaceholderBody.Text = body;
        LandingView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Visible;
    }

    private void ShowSettings()
    {
        _settings = AppSettings.Load();
        _loadedSplashDurationSeconds = _settings.SplashDurationSeconds;
        _loadedApplicationVersion = _settings.ApplicationVersion;

        _isLoadingSettings = true;
        SplashDurationTextBox.Text = _loadedSplashDurationSeconds.ToString();
        ApplicationVersionTextBox.Text = _loadedApplicationVersion;
        SettingsValidationMessage.Text = string.Empty;
        _isLoadingSettings = false;

        UpdateSettingsDirtyState();

        LandingView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Visible;
    }

    private void OnSettingsValueChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        SettingsValidationMessage.Text = string.Empty;
        UpdateSettingsDirtyState();
    }

    private void OnSaveSettings(object sender, RoutedEventArgs e)
    {
        if (!TryReadSettingsForm(out var splashDurationSeconds, out var applicationVersion))
        {
            return;
        }

        _settings.SplashDurationSeconds = splashDurationSeconds;
        _settings.ApplicationVersion = applicationVersion;
        _settings.Save();

        _loadedSplashDurationSeconds = splashDurationSeconds;
        _loadedApplicationVersion = applicationVersion;
        VersionText = GetVersionText(applicationVersion);

        ShowLanding();
    }

    private void OnCancelSettings(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "Discard all settings changes?",
            "Discard Changes",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            ShowLanding();
        }
    }

    private void OnCloseSettings(object sender, RoutedEventArgs e)
    {
        ShowLanding();
    }

    private bool TryReadSettingsForm(out int splashDurationSeconds, out string applicationVersion)
    {
        applicationVersion = ApplicationVersionTextBox.Text.Trim();

        if (!int.TryParse(SplashDurationTextBox.Text.Trim(), out splashDurationSeconds) ||
            splashDurationSeconds < 0)
        {
            SettingsValidationMessage.Text = "Splash duration must be a non-negative whole number.";
            SplashDurationTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(applicationVersion))
        {
            SettingsValidationMessage.Text = "Application version is required.";
            ApplicationVersionTextBox.Focus();
            return false;
        }

        return true;
    }

    private void UpdateSettingsDirtyState()
    {
        var isDirty = SplashDurationTextBox.Text.Trim() != _loadedSplashDurationSeconds.ToString() ||
            ApplicationVersionTextBox.Text.Trim() != _loadedApplicationVersion;

        SettingsCloseButton.Visibility = isDirty ? Visibility.Collapsed : Visibility.Visible;
        SettingsSaveButton.Visibility = isDirty ? Visibility.Visible : Visibility.Collapsed;
        SettingsCancelButton.Visibility = isDirty ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowLanding()
    {
        SettingsView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Collapsed;
        LandingView.Visibility = Visibility.Visible;
    }

    private static string GetVersionText(string applicationVersion)
    {
        return $"Version {applicationVersion}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

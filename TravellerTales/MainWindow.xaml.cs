using System.Windows;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using TravellerTales.Models;
using TravellerTales.Services;
using TravellerTales.Views;

namespace TravellerTales;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private AppSettings _settings;
    private int _loadedSplashDurationSeconds;
    private string _loadedApplicationVersion = string.Empty;
    private string _loadedBuildDate = string.Empty;
    private string _versionText;
    private bool _isLoadingSettings;
    private const string BuildDateFormat = "yyyy-MMM-dd";

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
        UpdateLandingContinueState();
    }

    private void OnNewCharacter(object sender, RoutedEventArgs e)
    {
        if (CharacterFileService.HasPausedCreation())
        {
            var result = ThemedDialog.Show(
                this,
                "Starting a new character will discard the current paused character creation save. This cannot be reversed.",
                "Discard Paused Character",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            CharacterFileService.DeletePausedCreation();
        }

        ShowNewCharacterWizardFresh();
    }

    private void OnContinueCharacter(object sender, RoutedEventArgs e)
    {
        ShowNewCharacterWizardFromPause();
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
        ShowCredits();
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
        NewCharacterWizardView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        CreditsView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Visible;
    }

    private void ShowSettings()
    {
        _settings = AppSettings.Load();
        _loadedSplashDurationSeconds = _settings.SplashDurationSeconds;
        _loadedApplicationVersion = _settings.ApplicationVersion;
        _loadedBuildDate = NormalizeBuildDate(_settings.BuildDate);

        _isLoadingSettings = true;
        SplashDurationTextBox.Text = _loadedSplashDurationSeconds.ToString();
        ApplicationVersionTextBox.Text = _loadedApplicationVersion;
        SetBuildDatePickerText(_loadedBuildDate);
        SettingsValidationMessage.Text = string.Empty;
        _isLoadingSettings = false;

        UpdateSettingsDirtyState();

        LandingView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Collapsed;
        NewCharacterWizardView.Visibility = Visibility.Collapsed;
        CreditsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Visible;
    }

    private void ShowNewCharacterWizardFresh()
    {
        var state = new CharacterCreationState
        {
            CurrentStepIndex = 0,
            Character = new Character
            {
                CreationMetadata = new CharacterCreationMetadata
                {
                    CreateStartDateTime = DateTime.Now,
                    Status = CharacterCreationStatus.InProgress
                }
            }
        };

        NewCharacterWizard.LoadState(state);
        LandingView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        CreditsView.Visibility = Visibility.Collapsed;
        LicensePopup.Visibility = Visibility.Collapsed;
        NewCharacterWizardView.Visibility = Visibility.Visible;
    }

    private void ShowNewCharacterWizardFromPause()
    {
        CharacterCreationState? state;

        try
        {
            state = CharacterFileService.LoadPausedCreation();
        }
        catch (Exception)
        {
            ThemedDialog.Show(
                this,
                "The paused character creation save could not be loaded.",
                "Continue Character",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            UpdateLandingContinueState();
            return;
        }

        if (state is null)
        {
            UpdateLandingContinueState();
            return;
        }

        state.Character.CreationMetadata.CreateContinueDateTimes.Add(DateTime.Now);
        state.Character.CreationMetadata.Status = CharacterCreationStatus.InProgress;
        CharacterFileService.SavePausedCreation(state);
        NewCharacterWizard.LoadState(state);

        LandingView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        CreditsView.Visibility = Visibility.Collapsed;
        LicensePopup.Visibility = Visibility.Collapsed;
        NewCharacterWizardView.Visibility = Visibility.Visible;
    }

    private void OnSaveCharacterCreation(object sender, CharacterCreationState state)
    {
        if (CharacterFileService.HasPausedCreation())
        {
            var result = ThemedDialog.Show(
                this,
                "A paused character creation save already exists. Saving now will overwrite it and cannot be reversed.",
                "Overwrite Paused Character",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        state.Character.CreationMetadata.CreatePauseDateTimes.Add(DateTime.Now);
        state.Character.CreationMetadata.Status = CharacterCreationStatus.Paused;
        CharacterFileService.SavePausedCreation(state);
        ShowLanding();
    }

    private void OnCheckpointSaveCharacterCreation(object sender, TravellerTales.Views.CharacterCreationCheckpointEventArgs e)
    {
        try
        {
            CharacterFileService.SavePausedCreation(e.State);
            e.Succeeded = true;
        }
        catch (Exception)
        {
            e.Succeeded = false;
            e.ErrorMessage = "The character checkpoint could not be saved. Stay on this step and try again.";
        }
    }

    private void OnCompleteCharacterCreation(object sender, CharacterCreationCompleteEventArgs e)
    {
        try
        {
            var character = e.State.Character;
            character.CaptureFinalCharacteristics();
            character.CreationMetadata.CreateFinalizedDateTime = DateTime.Now;
            character.CreationMetadata.Status = CharacterCreationStatus.Complete;

            if (CharacterFileService.FinalCharacterExists(character))
            {
                throw new InvalidOperationException("A Character with this name already exists.");
            }

            var referencedHomeworldIds = CharacterFileService.GetReferencedFinalHomeworldIds();
            HomeworldFileService.SaveHomeworld(character.Homeworld, referencedHomeworldIds);
            character.HomeworldId = character.Homeworld.Id;
            CharacterFileService.SaveFinalCharacter(character);
            CharacterFileService.DeletePausedCreation();

            e.Succeeded = true;
            ShowLanding();
        }
        catch (InvalidOperationException exception)
        {
            e.Succeeded = false;
            e.ErrorMessage = exception.Message;
        }
        catch (Exception)
        {
            e.Succeeded = false;
            e.ErrorMessage = "The character could not be completed. Stay on this step and try again.";
        }
    }

    private void OnCancelCharacterCreation(object sender, EventArgs e)
    {
        var result = ThemedDialog.Show(
            this,
            "Cancel character creation and discard all current character work?",
            "Cancel Character Creation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        CharacterFileService.DeletePausedCreation();
        ShowLanding();
    }

    private void OnDeleteOrphanedHomeworlds(object sender, RoutedEventArgs e)
    {
        var result = ThemedDialog.Show(
            this,
            "Delete all saved Homeworld files that are not referenced by completed Characters? This cannot be reversed.",
            "Delete Orphaned Homeworlds",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var referencedHomeworldIds = CharacterFileService.GetReferencedFinalHomeworldIds();
            var deleteResult = HomeworldFileService.DeleteOrphanedHomeworlds(referencedHomeworldIds);
            SettingsValidationMessage.Text = deleteResult.DeletedCount == 0
                ? "No orphaned Homeworlds found."
                : $"Deleted {deleteResult.DeletedCount} orphaned Homeworld file(s).";
        }
        catch (Exception)
        {
            SettingsValidationMessage.Text = "Orphaned Homeworld cleanup could not be completed.";
        }
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

    private void OnSettingsDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        if (BuildDatePicker.SelectedDate.HasValue)
        {
            SetBuildDatePickerText(FormatBuildDate(BuildDatePicker.SelectedDate.Value));
        }

        SettingsValidationMessage.Text = string.Empty;
        UpdateSettingsDirtyState();
    }

    private void OnSaveSettings(object sender, RoutedEventArgs e)
    {
        if (!TryReadSettingsForm(out var splashDurationSeconds, out var applicationVersion, out var buildDate))
        {
            return;
        }

        _settings.SplashDurationSeconds = splashDurationSeconds;
        _settings.ApplicationVersion = applicationVersion;
        _settings.BuildDate = buildDate;
        _settings.Save();

        _loadedSplashDurationSeconds = splashDurationSeconds;
        _loadedApplicationVersion = applicationVersion;
        _loadedBuildDate = buildDate;
        VersionText = GetVersionText(applicationVersion);

        ShowLanding();
    }

    private void OnCancelSettings(object sender, RoutedEventArgs e)
    {
        var result = ThemedDialog.Show(
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

    private void OnCloseCredits(object sender, RoutedEventArgs e)
    {
        ShowLanding();
    }

    private void OnShowLicense(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock licenseLink ||
            licenseLink.Tag is not string licenseFileName)
        {
            return;
        }

        LicensePopupTitle.Text = System.Windows.Automation.AutomationProperties.GetName(licenseLink);
        if (string.IsNullOrWhiteSpace(LicensePopupTitle.Text))
        {
            LicensePopupTitle.Text = "Third-Party License";
        }

        LicensePopupBody.Text = ReadLicenseText(licenseFileName);
        LicensePopup.Visibility = Visibility.Visible;
    }

    private void OnCloseLicensePopup(object sender, RoutedEventArgs e)
    {
        LicensePopup.Visibility = Visibility.Collapsed;
    }

    private bool TryReadSettingsForm(out int splashDurationSeconds, out string applicationVersion, out string buildDate)
    {
        applicationVersion = ApplicationVersionTextBox.Text.Trim();
        buildDate = GetBuildDatePickerText();

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

        if (!TryParseBuildDate(buildDate, out var parsedBuildDate))
        {
            SettingsValidationMessage.Text = $"Build date must use {BuildDateFormat}.";
            BuildDatePicker.Focus();
            return false;
        }

        buildDate = FormatBuildDate(parsedBuildDate);
        SetBuildDatePickerText(buildDate);

        return true;
    }

    private void UpdateSettingsDirtyState()
    {
        var isDirty = SplashDurationTextBox.Text.Trim() != _loadedSplashDurationSeconds.ToString() ||
            ApplicationVersionTextBox.Text.Trim() != _loadedApplicationVersion ||
            GetBuildDatePickerText() != _loadedBuildDate;

        SettingsCloseButton.Visibility = isDirty ? Visibility.Collapsed : Visibility.Visible;
        SettingsSaveButton.Visibility = isDirty ? Visibility.Visible : Visibility.Collapsed;
        SettingsCancelButton.Visibility = isDirty ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowLanding()
    {
        SettingsView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Collapsed;
        NewCharacterWizardView.Visibility = Visibility.Collapsed;
        CreditsView.Visibility = Visibility.Collapsed;
        LicensePopup.Visibility = Visibility.Collapsed;
        UpdateLandingContinueState();
        LandingView.Visibility = Visibility.Visible;
    }

    private void ShowCredits()
    {
        _settings = AppSettings.Load();
        CreditsVersionValue.Text = _settings.ApplicationVersion;
        CreditsBuildDateValue.Text = NormalizeBuildDate(_settings.BuildDate);
        LicensePopup.Visibility = Visibility.Collapsed;

        LandingView.Visibility = Visibility.Collapsed;
        PlaceholderView.Visibility = Visibility.Collapsed;
        NewCharacterWizardView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        CreditsView.Visibility = Visibility.Visible;
    }

    private void UpdateLandingContinueState()
    {
        var hasPausedCreation = CharacterFileService.HasPausedCreation();
        ContinueButton.IsEnabled = hasPausedCreation;
        ContinueButton.ToolTip = hasPausedCreation
            ? "Continue paused character creation."
            : "Continue will be available when an in-progress character exists.";
    }

    private static string GetVersionText(string applicationVersion)
    {
        return $"Version {applicationVersion}";
    }

    private static string NormalizeBuildDate(string buildDate)
    {
        return TryParseBuildDate(buildDate, out var parsedBuildDate)
            ? FormatBuildDate(parsedBuildDate)
            : "2026-Jul-07";
    }

    private static bool TryParseBuildDate(string buildDate, out DateTime parsedBuildDate)
    {
        return DateTime.TryParseExact(
            buildDate,
            BuildDateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsedBuildDate);
    }

    private static string FormatBuildDate(DateTime buildDate)
    {
        return buildDate.ToString(BuildDateFormat, CultureInfo.InvariantCulture);
    }

    private string GetBuildDatePickerText()
    {
        return BuildDatePicker.SelectedDate.HasValue
            ? FormatBuildDate(BuildDatePicker.SelectedDate.Value)
            : BuildDatePicker.Tag?.ToString()?.Trim() ?? string.Empty;
    }

    private void SetBuildDatePickerText(string buildDate)
    {
        if (TryParseBuildDate(buildDate, out var parsedBuildDate))
        {
            BuildDatePicker.SelectedDate = parsedBuildDate;
        }

        BuildDatePicker.Tag = buildDate;
        BuildDatePicker.Text = buildDate;
    }

    private static string ReadLicenseText(string licenseFileName)
    {
        var licensePath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Fonts",
            "Licenses",
            licenseFileName);

        try
        {
            return File.ReadAllText(licensePath);
        }
        catch (Exception)
        {
            return $"The local license file could not be loaded: {licenseFileName}";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

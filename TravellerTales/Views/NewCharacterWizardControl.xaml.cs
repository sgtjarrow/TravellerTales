using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TravellerTales.Models;

namespace TravellerTales.Views;

public partial class NewCharacterWizardControl : UserControl
{
    private static readonly string[] StepNames =
    [
        "Biography",
        "Homeworld",
        "Background Skills",
        "Review"
    ];

    private CharacterCreationState _state = new();

    public event EventHandler<CharacterCreationState>? SaveRequested;
    public event EventHandler? CancelRequested;

    public NewCharacterWizardControl()
    {
        InitializeComponent();
        BiographyStep.ValidityChanged += OnBiographyValidityChanged;
    }

    public void LoadState(CharacterCreationState state)
    {
        _state = state;
        _state.CurrentStepIndex = Math.Clamp(_state.CurrentStepIndex, 0, StepNames.Length - 1);
        BiographyStep.LoadCharacter(_state.Character);
        UpdateStep();
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        FooterMessageText.Text = string.Empty;

        if (_state.CurrentStepIndex == 0 && !BiographyStep.TryCommitRequired())
        {
            return;
        }

        CaptureCurrentStep();

        if (_state.CurrentStepIndex >= StepNames.Length - 1)
        {
            FooterMessageText.Text = "Final character completion will be added with the Review step implementation.";
            return;
        }

        _state.CurrentStepIndex++;
        UpdateStep();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        CaptureCurrentStep();
        SaveRequested?.Invoke(this, _state);
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnBiographyValidityChanged(object? sender, EventArgs e)
    {
        if (_state.CurrentStepIndex == 0)
        {
            NextButton.IsEnabled = BiographyStep.IsComplete();
        }
    }

    private void CaptureCurrentStep()
    {
        if (_state.CurrentStepIndex == 0)
        {
            BiographyStep.CommitPartial();
        }
    }

    private void UpdateStep()
    {
        BiographyStep.Visibility = _state.CurrentStepIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        HomeworldPlaceholder.Visibility = _state.CurrentStepIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        BackgroundSkillsPlaceholder.Visibility = _state.CurrentStepIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        ReviewPlaceholder.Visibility = _state.CurrentStepIndex == 3 ? Visibility.Visible : Visibility.Collapsed;

        WizardStatusText.Text = $"Step {_state.CurrentStepIndex + 1} of {StepNames.Length}";
        NextButton.Content = _state.CurrentStepIndex == StepNames.Length - 1 ? "Complete" : "Next";
        NextButton.IsEnabled = _state.CurrentStepIndex == 0 ? BiographyStep.IsComplete() : true;
        FooterMessageText.Text = string.Empty;

        UpdateStepIndicators();
        UpdateReview();
    }

    private void UpdateStepIndicators()
    {
        var indicators = new[]
        {
            BiographyStepIndicator,
            HomeworldStepIndicator,
            BackgroundSkillsStepIndicator,
            ReviewStepIndicator
        };

        for (var index = 0; index < indicators.Length; index++)
        {
            if (index < _state.CurrentStepIndex)
            {
                SetIndicator(indicators[index], "#AA143044", "#8CE8FF", "#BFEFFF");
            }
            else if (index == _state.CurrentStepIndex)
            {
                SetIndicator(indicators[index], "#DD1A5574", "#C99A45", "#FFFFFF");
            }
            else
            {
                SetIndicator(indicators[index], "#AA080D15", "#31596D", "#7897A8");
            }
        }
    }

    private static void SetIndicator(Border indicator, string background, string border, string foreground)
    {
        indicator.Background = (Brush)new BrushConverter().ConvertFromString(background)!;
        indicator.BorderBrush = (Brush)new BrushConverter().ConvertFromString(border)!;

        if (indicator.Child is TextBlock textBlock)
        {
            textBlock.Foreground = (Brush)new BrushConverter().ConvertFromString(foreground)!;
        }
    }

    private void UpdateReview()
    {
        var character = _state.Character;
        ReviewIdentityText.Text =
            $"Name: {ValueOrPending(character.Name)}\n" +
            $"Race: {character.Race}\n" +
            $"Gender: {character.Gender}\n" +
            $"Age: {ValueOrPending(character.Age)}\n" +
            $"Height: {ValueOrPending(character.HeightInches)} inches / {character.HeightMeters:0.00} meters\n" +
            $"Weight: {ValueOrPending(character.WeightPounds)} pounds / {character.WeightKilograms:0.00} kilograms\n" +
            $"Eye Color: {ValueOrPending(character.EyeColor)}\n" +
            $"Description: {ValueOrPending(character.Description)}";

        var metadata = character.CreationMetadata;
        ReviewMetadataText.Text =
            $"Started: {FormatDateTime(metadata.CreateStartDateTime)}\n" +
            $"Paused: {metadata.CreatePauseDateTimes.Count} time(s)\n" +
            $"Continued: {metadata.CreateContinueDateTimes.Count} time(s)\n" +
            $"Finalized: {(metadata.CreateFinalizedDateTime.HasValue ? FormatDateTime(metadata.CreateFinalizedDateTime.Value) : "Pending")}";
    }

    private static string ValueOrPending(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Pending" : value;
    }

    private static string ValueOrPending(int value)
    {
        return value > 0 ? value.ToString(CultureInfo.InvariantCulture) : "Pending";
    }

    private static string FormatDateTime(DateTime value)
    {
        return value.ToString("yyyy-MMM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}

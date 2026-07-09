using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public partial class NewCharacterWizardControl : UserControl
{
    private static readonly string[] StepNames =
    [
        "Biography",
        "Homeworld",
        "Characteristics",
        "Background Skills",
        "Review"
    ];
    private static readonly string[] StepBackgroundAssetNames =
    [
        "medical_scan.png",
        "homeworld.png",
        "medical_scan.png",
        "medical_scan.png",
        "medical_scan.png"
    ];

    private CharacterCreationState _state = new();

    public event EventHandler<CharacterCreationState>? SaveRequested;
    public event EventHandler<CharacterCreationCheckpointEventArgs>? CheckpointSaveRequested;
    public event EventHandler? CancelRequested;

    public NewCharacterWizardControl()
    {
        InitializeComponent();
        BiographyStep.ValidityChanged += OnBiographyValidityChanged;
        HomeworldStep.ValidityChanged += OnHomeworldValidityChanged;
    }

    public void LoadState(CharacterCreationState state)
    {
        _state = state;
        _state.CurrentStepIndex = Math.Clamp(_state.CurrentStepIndex, 0, StepNames.Length - 1);
        _state.Character.Homeworld ??= new();
        BiographyStep.LoadCharacter(_state.Character);
        HomeworldStep.LoadState(_state);
        UpdateStep();
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        FooterMessageText.Text = string.Empty;

        if (_state.CurrentStepIndex == 0 && !BiographyStep.TryCommitRequired())
        {
            return;
        }

        if (_state.CurrentStepIndex == 1 && !HomeworldStep.TryCommitRequired())
        {
            return;
        }

        CaptureCurrentStep();

        if (_state.CurrentStepIndex == 1 && !TrySaveHomeworld())
        {
            return;
        }

        if (_state.CurrentStepIndex >= StepNames.Length - 1)
        {
            FooterMessageText.Text = "Final character completion will be added with the Review step implementation.";
            return;
        }

        var previousStepIndex = _state.CurrentStepIndex;
        _state.CurrentStepIndex++;

        if (!TrySaveCheckpoint())
        {
            _state.CurrentStepIndex = previousStepIndex;
            return;
        }

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

    private void OnHomeworldValidityChanged(object? sender, EventArgs e)
    {
        if (_state.CurrentStepIndex == 1)
        {
            NextButton.IsEnabled = HomeworldStep.IsComplete();
        }
    }

    private void CaptureCurrentStep()
    {
        if (_state.CurrentStepIndex == 0)
        {
            BiographyStep.CommitPartial();
        }
        else if (_state.CurrentStepIndex == 1)
        {
            HomeworldStep.CommitPartial();
        }
    }

    private bool TrySaveCheckpoint()
    {
        var checkpointEventArgs = new CharacterCreationCheckpointEventArgs(_state);
        CheckpointSaveRequested?.Invoke(this, checkpointEventArgs);

        if (checkpointEventArgs.Succeeded)
        {
            return true;
        }

        FooterMessageText.Text = string.IsNullOrWhiteSpace(checkpointEventArgs.ErrorMessage)
            ? "The character checkpoint could not be saved."
            : checkpointEventArgs.ErrorMessage;
        return false;
    }

    private bool TrySaveHomeworld()
    {
        try
        {
            var homeworld = _state.Character.Homeworld;
            HomeworldFileService.SaveHomeworld(homeworld);
            _state.Character.HomeworldId = homeworld.Id;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            HomeworldStep.SetValidationMessage(exception.Message);
            return false;
        }
        catch (Exception)
        {
            HomeworldStep.SetValidationMessage("The Homeworld file could not be saved. Stay on this step and try again.");
            return false;
        }
    }

    private void UpdateStep()
    {
        BiographyStep.Visibility = _state.CurrentStepIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        HomeworldStep.Visibility = _state.CurrentStepIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        CharacteristicsPlaceholder.Visibility = _state.CurrentStepIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        BackgroundSkillsPlaceholder.Visibility = _state.CurrentStepIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
        ReviewPlaceholder.Visibility = _state.CurrentStepIndex == 4 ? Visibility.Visible : Visibility.Collapsed;

        WizardStatusText.Text = $"Step {_state.CurrentStepIndex + 1} of {StepNames.Length}";
        NextButton.Content = _state.CurrentStepIndex == StepNames.Length - 1 ? "Complete" : "Next";
        NextButton.IsEnabled = _state.CurrentStepIndex switch
        {
            0 => BiographyStep.IsComplete(),
            1 => HomeworldStep.IsComplete(),
            _ => true
        };
        FooterMessageText.Text = string.Empty;

        UpdateBackground();
        UpdateStepIndicators();
        UpdateReview();
    }

    private void UpdateBackground()
    {
        var assetName = StepBackgroundAssetNames[_state.CurrentStepIndex];
        WizardBackgroundImage.Source = new BitmapImage(
            new Uri($"pack://application:,,,/Assets/{assetName}", UriKind.Absolute));
    }

    private void UpdateStepIndicators()
    {
        var indicators = new[]
        {
            BiographyStepIndicator,
            HomeworldStepIndicator,
            CharacteristicsStepIndicator,
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
        var worldSize = WorldSizeCatalog.FromValue(character.Homeworld.WorldSizeValue);
        var atmosphere = AtmosphereCatalog.FromValue(character.Homeworld.AtmosphereValue);
        var temperature = TemperatureCatalog.FromKey(character.Homeworld.TemperatureKey);
        var hydrographics = HydrographicsCatalog.FromValue(character.Homeworld.HydrographicsValue);
        var population = PopulationCatalog.FromValue(character.Homeworld.PopulationValue);
        var starport = StarportCatalog.FromCode(character.Homeworld.StarportCode);
        var government = GovernmentCatalog.FromValue(character.Homeworld.GovernmentValue);
        var lawLevel = LawLevelCatalog.FromValue(character.Homeworld.LawLevelValue);
        var techLevel = TechLevelCatalog.FromValue(character.Homeworld.TechLevelValue);
        var travelCode = TravelCodeCatalog.FromCode(character.Homeworld.TravelCode);
        var culturalTags = FormatCulturalTags(character.Homeworld);
        var factions = FormatFactions(character.Homeworld);
        var bases = FormatBases(character.Homeworld);
        ReviewIdentityText.Text =
            $"Name: {ValueOrPending(character.DisplayName)}\n" +
            $"Race: {character.Race}\n" +
            $"Gender: {character.Gender}\n" +
            $"Age: {ValueOrPending(character.Age)}\n" +
            $"Height: {ValueOrPending(character.HeightInches)} inches / {character.HeightMeters:0.00} meters\n" +
            $"Weight: {ValueOrPending(character.WeightPounds)} pounds / {character.WeightKilograms:0.00} kilograms\n" +
            $"Eye Color: {character.EyeColor}\n" +
            $"Homeworld: {ValueOrPending(character.Homeworld.Name)}\n" +
            $"Homeworld Starport: {starport.Name} ({starport.Code})\n" +
            $"Homeworld Size: {worldSize.Name} ({worldSize.Code})\n" +
            $"Homeworld Atmosphere: {atmosphere.Name} ({atmosphere.Code})\n" +
            $"Homeworld Temperature: {temperature.Name}\n" +
            $"Homeworld Hydrographics: {hydrographics.Name} ({hydrographics.Code})\n" +
            $"Homeworld Population: {population.Name} ({population.Code})\n" +
            $"Homeworld Government: {government.Name} ({government.Code})\n" +
            $"Homeworld Law Level: {lawLevel.Name} ({lawLevel.Code})\n" +
            $"Homeworld Tech Level: {techLevel.Name} ({techLevel.Code})\n" +
            $"Homeworld Trade Classifications: {character.Homeworld.TradeClassifications}\n" +
            $"Homeworld Travel Code: {travelCode.Name} ({travelCode.Code})\n" +
            $"Homeworld Cultural Tags: {culturalTags}\n" +
            $"Homeworld Factions: {factions}\n" +
            $"Homeworld Bases: {bases}\n" +
            $"Homeworld Gas Giants: {character.Homeworld.NumberOfGasGiants}\n" +
            $"Homeworld Planetoid Belts: {character.Homeworld.NumberOfPlanetoidBelts}\n" +
            $"Homeworld Notes: {ValueOrPending(character.Homeworld.Notes)}\n" +
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

    private static string FormatCulturalTags(Homeworld homeworld)
    {
        if (homeworld.PopulationValue == 0 || homeworld.CulturalTagValues.Count == 0)
        {
            return "None";
        }

        return string.Join(", ", homeworld.CulturalTagValues.Select(value => CulturalTagCatalog.FromValue(value).Name));
    }

    private static string FormatFactions(Homeworld homeworld)
    {
        if (homeworld.Factions.Count == 0)
        {
            return "None";
        }

        return string.Join("; ", homeworld.Factions.Select(faction =>
        {
            var category = FactionCategoryCatalog.FromCode(faction.CategoryCode);
            var strength = FactionStrengthCatalog.FromCode(faction.StrengthCode);

            return $"{faction.Name}: {category.Name} ({category.Code}), {strength.Name} ({strength.Code})";
        }));
    }

    private static string FormatBases(Homeworld homeworld)
    {
        var bases = homeworld.Bases ?? new HomeworldBases();
        var baseNames = new List<string>();

        if (bases.HighPort)
        {
            baseNames.Add("High Port");
        }

        if (bases.MilitaryBase)
        {
            baseNames.Add("Military Base");
        }

        if (bases.NavalBase)
        {
            baseNames.Add("Naval Base");
        }

        if (bases.NavalBase && bases.NavalDepot)
        {
            baseNames.Add("Naval Depot");
        }

        if (bases.ScoutBase)
        {
            baseNames.Add("Scout Base");
        }

        if (bases.ScoutBase && bases.ScoutWayStation)
        {
            baseNames.Add("Scout Way Station");
        }

        if (bases.CorsairBase)
        {
            baseNames.Add("Corsair Base");
        }

        return baseNames.Count == 0 ? "None" : string.Join(", ", baseNames);
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

public sealed class CharacterCreationCheckpointEventArgs : EventArgs
{
    public CharacterCreationCheckpointEventArgs(CharacterCreationState state)
    {
        State = state;
    }

    public CharacterCreationState State { get; }
    public bool Succeeded { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

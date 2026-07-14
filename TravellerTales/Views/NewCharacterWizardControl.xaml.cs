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
        "Career Terms",
        "Review"
    ];
    private static readonly string[] StepBackgroundAssetNames =
    [
        "medical_scan.png",
        "homeworld.png",
        "medical_scan.png",
        "medical_scan.png",
        "medical_scan.png",
        "medical_scan.png"
    ];

    private CharacterCreationState _state = new();

    public event EventHandler<CharacterCreationState>? SaveRequested;
    public event EventHandler<CharacterCreationCheckpointEventArgs>? CheckpointSaveRequested;
    public event EventHandler<CharacterCreationCompleteEventArgs>? CompleteRequested;
    public event EventHandler<CharacterCreationCancelEventArgs>? CancelRequested;

    public NewCharacterWizardControl()
    {
        InitializeComponent();
        BiographyStep.ValidityChanged += OnBiographyValidityChanged;
        HomeworldStep.ValidityChanged += OnHomeworldValidityChanged;
        CharacteristicsStep.ValidityChanged += OnCharacteristicsValidityChanged;
        BackgroundSkillsStep.ValidityChanged += OnBackgroundSkillsValidityChanged;
        CareerTermsStep.ValidityChanged += OnCareerTermsValidityChanged;
        CareerTermsStep.TermActivityChanged += OnCareerTermsTermActivityChanged;
        CareerTermsStep.CancelCharacterCreationRequested += OnCareerTermsCancelCharacterCreationRequested;
    }

    public void LoadState(CharacterCreationState state)
    {
        _state = state;
        _state.CurrentStepIndex = Math.Clamp(_state.CurrentStepIndex, 0, StepNames.Length - 1);
        _state.Character.Homeworld ??= new();
        _state.BackgroundSkills ??= new();
        _state.CareerTerms ??= new();
        CareerTermService.Normalize(_state.CareerTerms);
        BiographyStep.LoadCharacter(_state.Character);
        HomeworldStep.LoadState(_state);
        UpdateStep();
        ResetCurrentStepView();
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

        if (_state.CurrentStepIndex == 2 && !CharacteristicsStep.TryCommitRequired())
        {
            return;
        }

        if (_state.CurrentStepIndex == 3 && !BackgroundSkillsStep.TryCommitRequired())
        {
            return;
        }

        CaptureCurrentStep();

        if (_state.CurrentStepIndex >= StepNames.Length - 1)
        {
            _state.Character.CareerTerms = _state.CareerTerms;
            var creationCapViolations = SkillAdjustmentService.GetCreationCapViolations(_state.Character);
            if (creationCapViolations.Count > 0)
            {
                FooterMessageText.Text =
                    "A character cannot be completed with Skills or Specialties above 4: " +
                    string.Join(", ", creationCapViolations);
                return;
            }

            var completeEventArgs = new CharacterCreationCompleteEventArgs(_state);
            CompleteRequested?.Invoke(this, completeEventArgs);
            if (!completeEventArgs.Succeeded)
            {
                FooterMessageText.Text = string.IsNullOrWhiteSpace(completeEventArgs.ErrorMessage)
                    ? "The character could not be completed. Stay on this step and try again."
                    : completeEventArgs.ErrorMessage;
            }

            return;
        }

        var previousStepIndex = _state.CurrentStepIndex;
        _state.CurrentStepIndex++;

        if (_state.CurrentStepIndex == 5)
        {
            _state.Character.CaptureFinalCharacteristics();
            _state.Character.CareerTerms = _state.CareerTerms;
        }

        if (!TrySaveCheckpoint())
        {
            _state.CurrentStepIndex = previousStepIndex;
            return;
        }

        UpdateStep();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (_state.CurrentStepIndex == 4 && !CareerTermsStep.CanSave())
        {
            FooterMessageText.Text = "Finish the active Career Term before saving.";
            return;
        }

        CaptureCurrentStep();
        SaveRequested?.Invoke(this, _state);
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, new CharacterCreationCancelEventArgs());
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

    private void OnCharacteristicsValidityChanged(object? sender, EventArgs e)
    {
        if (_state.CurrentStepIndex == 2)
        {
            NextButton.IsEnabled = CharacteristicsStep.IsComplete();
        }
    }

    private void OnBackgroundSkillsValidityChanged(object? sender, EventArgs e)
    {
        if (_state.CurrentStepIndex == 3)
        {
            NextButton.IsEnabled = BackgroundSkillsStep.IsComplete();
        }
    }

    private void OnCareerTermsValidityChanged(object? sender, EventArgs e)
    {
        if (_state.CurrentStepIndex == 4)
        {
            NextButton.IsEnabled = CareerTermsStep.IsComplete();
            UpdateSaveButton();
        }
    }

    private void OnCareerTermsTermActivityChanged(object? sender, EventArgs e)
    {
        if (_state.CurrentStepIndex == 4)
        {
            UpdateSaveButton();
        }
    }

    private void OnCareerTermsCancelCharacterCreationRequested(object? sender, CharacterCreationCancelEventArgs e)
    {
        CancelRequested?.Invoke(this, e);
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
        else if (_state.CurrentStepIndex == 2)
        {
            CharacteristicsStep.CommitPartial();
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

    private void UpdateStep()
    {
        if (_state.CurrentStepIndex == 2)
        {
            CharacteristicsStep.LoadState(_state);
        }
        else if (_state.CurrentStepIndex == 3)
        {
            BackgroundSkillsStep.LoadState(_state);
        }
        else if (_state.CurrentStepIndex == 4)
        {
            CareerTermsStep.LoadState(_state);
        }
        else if (_state.CurrentStepIndex == 5 &&
                 HasCharacteristics(_state.Character.CurrentCharacteristics) &&
                 !HasCharacteristics(_state.Character.FinalCharacteristics))
        {
            _state.Character.CaptureFinalCharacteristics();
        }

        BiographyStep.Visibility = _state.CurrentStepIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        HomeworldStep.Visibility = _state.CurrentStepIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        CharacteristicsStep.Visibility = _state.CurrentStepIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        BackgroundSkillsStep.Visibility = _state.CurrentStepIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
        CareerTermsStep.Visibility = _state.CurrentStepIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
        ReviewPlaceholder.Visibility = _state.CurrentStepIndex == 5 ? Visibility.Visible : Visibility.Collapsed;

        WizardStatusText.Text = $"Step {_state.CurrentStepIndex + 1} of {StepNames.Length}";
        NextButton.Content = _state.CurrentStepIndex == StepNames.Length - 1 ? "Complete" : "Next";
        NextButton.IsEnabled = _state.CurrentStepIndex switch
        {
            0 => BiographyStep.IsComplete(),
            1 => HomeworldStep.IsComplete(),
            2 => CharacteristicsStep.IsComplete(),
            3 => BackgroundSkillsStep.IsComplete(),
            4 => CareerTermsStep.IsComplete(),
            _ => true
        };
        FooterMessageText.Text = string.Empty;
        UpdateSaveButton();

        UpdateBackground();
        UpdateStepIndicators();
        UpdateReview();
        ResetCurrentStepView();
    }

    private void ResetCurrentStepView()
    {
        switch (_state.CurrentStepIndex)
        {
            case 0:
                BiographyStep.ResetView();
                break;
            case 1:
                HomeworldStep.ResetView();
                break;
            case 2:
                CharacteristicsStep.ResetView();
                break;
            case 5:
                ResetReviewView();
                break;
        }
    }

    private void UpdateSaveButton()
    {
        SaveButton.IsEnabled = _state.CurrentStepIndex != 4 || CareerTermsStep.CanSave();
    }

    private void ResetReviewView()
    {
        Dispatcher.BeginInvoke(() =>
        {
            ReviewTabControl.SelectedIndex = 0;
            ReviewCharacterScrollViewer.ScrollToTop();
            ReviewLogScrollViewer.ScrollToTop();
        });
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
            CareerTermsStepIndicator,
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
        var biographyAttributes = FormatBiographyAttributes(character);
        ReviewIdentityText.Text =
            $"Name: {ValueOrPending(character.DisplayName)}\n" +
            $"Race: {FormatEnum(character.Race)}\n" +
            $"Gender: {FormatEnum(character.Gender)}\n" +
            $"Age: {ValueOrPending(character.Age)}\n" +
            $"Height: {ValueOrPending(character.HeightInches)} inches / {character.HeightMeters:0.00} meters\n" +
            $"Weight: {ValueOrPending(character.WeightPounds)} pounds / {character.WeightKilograms:0.00} kilograms\n" +
            $"Eye Color: {FormatEnum(character.EyeColor)}\n" +
            biographyAttributes +
            $"Description: {ValueOrNone(character.Description)}\n" +
            $"Character Notes: {ValueOrNone(character.Notes)}\n" +
            "\n" +
            FormatCharacteristicSection("Starting Characteristics", character.StartingCharacteristics) +
            "\n" +
            FormatCharacteristicSection("Final Characteristics", character.FinalCharacteristics) +
            "\n" +
            FormatSkillSection(character) +
            "\n" +
            FormatCareerTermsSection(_state.CareerTerms) +
            "\n" +
            $"Homeworld: {ValueOrPending(character.Homeworld.Name)}\n" +
            $"Homeworld UWP: {character.Homeworld.Uwp}\n" +
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
            $"Homeworld Notes: {ValueOrNone(character.Homeworld.Notes)}";

        var metadata = character.CreationMetadata;
        ReviewMetadataText.Text =
            $"Started: {FormatDateTime(metadata.CreateStartDateTime)}\n" +
            $"Paused: {metadata.CreatePauseDateTimes.Count} time(s)\n" +
            $"Continued: {metadata.CreateContinueDateTimes.Count} time(s)\n" +
            $"Status: {FormatEnum(metadata.Status)}";
    }

    private static string ValueOrPending(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Pending" : value;
    }

    private static string ValueOrNone(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value;
    }

    private static string FormatCharacteristicSection(string title, CharacteristicSet characteristics)
    {
        if (!HasCharacteristics(characteristics))
        {
            return $"{title}: Pending\n";
        }

        return
            $"{title}\n" +
            $"Strength: {FormatCharacteristicValue(characteristics.Strength)}\n" +
            $"Dexterity: {FormatCharacteristicValue(characteristics.Dexterity)}\n" +
            $"Endurance: {FormatCharacteristicValue(characteristics.Endurance)}\n" +
            $"Intellect: {FormatCharacteristicValue(characteristics.Intellect)}\n" +
            $"Education: {FormatCharacteristicValue(characteristics.Education)}\n" +
            $"Social: {FormatCharacteristicValue(characteristics.Social)}\n";
    }

    private static string FormatCharacteristicValue(int value)
    {
        return $"{value} ({FormatSigned(CharacteristicRules.GetDiceModifier(value))})";
    }

    private static string FormatSkillSection(Character character)
    {
        if (character.Skills.Skills.Count == 0)
        {
            return "Skills: None\n";
        }

        var catalog = SpecialtyCatalogService.LoadCatalog();
        var lines = new List<string> { "Skills" };

        foreach (var skill in character.Skills.Skills.OrderBy(skill => SkillCatalog.GetDisplayName(skill.SkillName)))
        {
            var skillName = SkillCatalog.GetDisplayName(skill.SkillName);
            if (SkillCatalog.IsValueOnly(skill.SkillName))
            {
                lines.Add($"{skillName}: {skill.Value}");
                continue;
            }

            if (skill.Specialties.Count == 0)
            {
                lines.Add($"{skillName}: 0");
                continue;
            }

            foreach (var specialty in skill.Specialties.OrderByDescending(item => item.Value).ThenBy(item => FormatSpecialtyName(catalog, item.SpecialtyId)))
            {
                lines.Add($"{skillName} ({FormatSpecialtyName(catalog, specialty.SpecialtyId)}): {specialty.Value}");
            }
        }

        return string.Join('\n', lines) + "\n";
    }

    private static string FormatCareerTermsSection(CareerTermsState careerTerms)
    {
        CareerTermService.Normalize(careerTerms);
        if (careerTerms.CompletedTerms.Count == 0)
        {
            return "Career Terms: None\n";
        }

        var lines = new List<string> { "Career Terms" };
        foreach (var term in careerTerms.CompletedTerms.OrderBy(term => term.Sequence))
        {
            var eventInfo = !string.IsNullOrWhiteSpace(term.MishapSummary)
                ? term.MishapSummary
                : string.IsNullOrWhiteSpace(term.EventSummary)
                    ? "No Event recorded."
                    : term.EventSummary;

            lines.Add(
                $"Term {term.Sequence}: {term.Career} / {term.Assignment}; " +
                $"Rank {term.EndingRank}; {eventInfo}");
        }

        return string.Join('\n', lines) + "\n";
    }

    private static string FormatSpecialtyName(SkillSpecialtyCatalog catalog, string specialtyId)
    {
        return catalog.FindById(specialtyId)?.DisplayName ?? specialtyId;
    }

    private static bool HasCharacteristics(CharacteristicSet characteristics)
    {
        return characteristics.Strength > 0 ||
               characteristics.Dexterity > 0 ||
               characteristics.Endurance > 0 ||
               characteristics.Intellect > 0 ||
               characteristics.Education > 0 ||
               characteristics.Social > 0;
    }

    private static string FormatSigned(int value)
    {
        return value > 0
            ? $"+{value}"
            : value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatBiographyAttributes(Character character)
    {
        var lines = new List<string>();

        if (character.Race == RaceType.Human)
        {
            if (character.Heritage.HasValue)
            {
                lines.Add($"Heritage: {FormatEnum(character.Heritage.Value)}");
            }

            lines.Add($"Skin Color: {FormatEnum(character.SkinColor)}");

            if (character.HairColor.HasValue)
            {
                lines.Add($"Hair Color: {FormatEnum(character.HairColor.Value)}");
            }
        }
        else
        {
            if (character.FurPattern.HasValue)
            {
                lines.Add($"Fur Pattern: {FormatEnum(character.FurPattern.Value)}");
            }

            if (character.FurPrimaryColor.HasValue)
            {
                lines.Add($"Fur Primary Color: {FormatEnum(character.FurPrimaryColor.Value)}");
            }

            if (character.FurSecondaryColor.HasValue)
            {
                lines.Add($"Fur Secondary Color: {FormatEnum(character.FurSecondaryColor.Value)}");
            }
        }

        return lines.Count == 0
            ? string.Empty
            : string.Join('\n', lines) + "\n";
    }

    private static string FormatEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var text = value.ToString();

        return string.Concat(text.Select((character, index) =>
            index > 0 && char.IsUpper(character) && char.IsLower(text[index - 1])
                ? $" {character}"
                : character.ToString()));
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

public sealed class CharacterCreationCompleteEventArgs : EventArgs
{
    public CharacterCreationCompleteEventArgs(CharacterCreationState state)
    {
        State = state;
    }

    public CharacterCreationState State { get; }
    public bool Succeeded { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class CharacterCreationCancelEventArgs : EventArgs
{
    public bool Cancelled { get; set; }
}

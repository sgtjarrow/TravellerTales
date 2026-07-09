using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public partial class HomeworldStepControl : UserControl
{
    private static readonly Brush ValidBorderBrush = (Brush)new BrushConverter().ConvertFromString("#5FD9FF")!;
    private static readonly Brush InvalidBorderBrush = (Brush)new BrushConverter().ConvertFromString("#D93C3C")!;
    private static readonly Brush DefaultCardBackground = (Brush)new BrushConverter().ConvertFromString("#D006111E")!;
    private static readonly Brush DefaultCardBorder = (Brush)new BrushConverter().ConvertFromString("#5FD9FF")!;
    private static readonly Brush SelectedCardBackground = (Brush)new BrushConverter().ConvertFromString("#DD1A5574")!;
    private static readonly Brush SelectedCardBorder = (Brush)new BrushConverter().ConvertFromString("#C99A45")!;
    private static readonly Brush DefaultCodeBackground = (Brush)new BrushConverter().ConvertFromString("#A0071523")!;
    private static readonly Brush DefaultCodeBorder = (Brush)new BrushConverter().ConvertFromString("#5FD9FF")!;
    private static readonly Brush SelectedCodeBackground = (Brush)new BrushConverter().ConvertFromString("#CC4A3212")!;
    private static readonly Brush SelectedCodeBorder = (Brush)new BrushConverter().ConvertFromString("#C99A45")!;
    private static readonly HomeworldCandidate[] CandidatePool =
    [
        new() { Name = "Meridian Reach", Archetype = "Frontier Garden", Summary = "A temperate border world with scattered domed ports, long survey roads, and old colony ruins beneath the wetlands." },
        new() { Name = "Kharon's Belt", Archetype = "Industrial Belt", Summary = "A hard-scrabble chain of mining habitats where shipyards, ore clans, and spacer unions shape every childhood." },
        new() { Name = "Vespera", Archetype = "Highport World", Summary = "A crowded trade nexus built around orbital elevators, diplomatic enclaves, and families who measure status by passage rights." },
        new() { Name = "Dawnfall", Archetype = "Tide-Locked Colony", Summary = "Settlements cling to the twilight band while scouts, harvesters, and weather towers hold back the planet's extremes." },
        new() { Name = "Ashen Tor", Archetype = "Volcanic Outpost", Summary = "An extraction world of black glass plains, pressure shelters, and crews raised around seismic alarms and refinery lights." },
        new() { Name = "Caldera Blue", Archetype = "Ocean Settlement", Summary = "Floating cities and submerged labs drift across warm seas rich with bioluminescent reefs and salvage claims." },
        new() { Name = "Eidolon Steppe", Archetype = "Sparse Agro World", Summary = "A wide-open world of rover caravans, orbital weather mirrors, and settlements spread across endless engineered grassland." },
        new() { Name = "Nadir Station", Archetype = "Deep Space Habitat", Summary = "A free-floating habitat where every citizen knows pressure doors, ration math, and the politics of passing vessels." },
        new() { Name = "Crownward", Archetype = "Bureaucratic Core", Summary = "A polished administrative world where records, guild sponsorships, and family credentials open nearly every door." }
    ];

    private readonly Border[] _candidateCards;
    private readonly Button[][] _candidateCodeButtons;
    private readonly TextBlock[] _candidateDetailTexts;
    private readonly HomeworldDetailAttribute?[] _candidateDetailAttributes = new HomeworldDetailAttribute?[3];
    private CharacterCreationState _state = new();
    private bool _isLoading;

    public event EventHandler? ValidityChanged;

    public HomeworldStepControl()
    {
        InitializeComponent();

        _candidateCards = [CandidateOneCard, CandidateTwoCard, CandidateThreeCard];
        _candidateCodeButtons =
        [
            [CandidateOneWorldSizeCodeButton, CandidateOneAtmosphereCodeButton, CandidateOneHydrographicsCodeButton],
            [CandidateTwoWorldSizeCodeButton, CandidateTwoAtmosphereCodeButton, CandidateTwoHydrographicsCodeButton],
            [CandidateThreeWorldSizeCodeButton, CandidateThreeAtmosphereCodeButton, CandidateThreeHydrographicsCodeButton]
        ];
        _candidateDetailTexts = [CandidateOneDetailText, CandidateTwoDetailText, CandidateThreeDetailText];
    }

    public void LoadState(CharacterCreationState state)
    {
        _state = state;
        _state.Character.Homeworld ??= new();
        _state.HomeworldCandidates ??= [];

        if (_state.HomeworldCandidates.Count != 3 ||
            _state.HomeworldCandidates.Any(candidate => !IsValidWorldSize(candidate.WorldSizeValue) ||
                                                        !IsValidAtmosphere(candidate.AtmosphereValue) ||
                                                        !IsValidTemperature(candidate.TemperatureKey) ||
                                                        !IsValidHydrographics(candidate.HydrographicsValue)))
        {
            _state.HomeworldCandidates = GenerateCandidates();
            _state.SelectedHomeworldCandidateIndex = null;
            _state.Character.Homeworld.WorldSizeValue = 0;
            _state.Character.Homeworld.AtmosphereValue = 0;
            _state.Character.Homeworld.TemperatureKey = TemperatureCatalog.Swinging.Key;
            _state.Character.Homeworld.HydrographicsValue = 0;
        }

        if (_state.SelectedHomeworldCandidateIndex is < 0 or > 2)
        {
            _state.SelectedHomeworldCandidateIndex = null;
        }

        _isLoading = true;
        Array.Fill(_candidateDetailAttributes, null);
        HomeworldNameTextBox.Text = _state.Character.Homeworld.Name;
        HomeworldNotesTextBox.Text = _state.Character.Homeworld.Notes;
        LoadCandidateCards();
        UpdateSelectionVisuals();
        _isLoading = false;
        Validate(showMessage: false);
    }

    public bool TryCommitRequired()
    {
        CaptureFields();
        return Validate(showMessage: true);
    }

    public void CommitPartial()
    {
        CaptureFields();
        Validate(showMessage: false);
    }

    public bool IsComplete()
    {
        CaptureFields();
        return Validate(showMessage: false);
    }

    public void SetValidationMessage(string message)
    {
        ValidationMessage.Text = message;
        UpdateRequiredFieldBorders();
    }

    private void CaptureFields()
    {
        _state.Character.Homeworld.Name = HomeworldNameTextBox.Text.Trim();
        _state.Character.Homeworld.Notes = HomeworldNotesTextBox.Text.Trim();
        CommitSelectedCandidateAttributes();
    }

    private bool Validate(bool showMessage)
    {
        var message = GetValidationMessage();
        UpdateRequiredFieldBorders();

        if (showMessage || string.IsNullOrWhiteSpace(message))
        {
            ValidationMessage.Text = message;
        }

        return string.IsNullOrWhiteSpace(message);
    }

    private string GetValidationMessage()
    {
        if (string.IsNullOrWhiteSpace(HomeworldNameTextBox.Text))
        {
            return "Homeworld name is required.";
        }

        if (!_state.SelectedHomeworldCandidateIndex.HasValue)
        {
            return "Choose a Homeworld candidate.";
        }

        if (HomeworldFileService.HomeworldExists(HomeworldNameTextBox.Text, _state.Character.Homeworld.Id))
        {
            return "A Homeworld with this name already exists.";
        }

        return string.Empty;
    }

    private void OnGenerateName(object sender, RoutedEventArgs e)
    {
        HomeworldNameTextBox.Text = HomeworldNameGenerator.Generate();
        ValidationMessage.Text = string.Empty;
        UpdateRequiredFieldBorders();
        ValidityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnFieldChanged(object sender, EventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        ValidationMessage.Text = string.Empty;
        UpdateRequiredFieldBorders();
        ValidityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCandidateSelected(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: string tag } && int.TryParse(tag, out var index))
        {
            SelectCandidate(index);
        }
    }

    private void OnUwpCodeClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag })
        {
            return;
        }

        var parts = tag.Split('|', 2);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var index) ||
            !Enum.TryParse(parts[1], out HomeworldDetailAttribute attribute))
        {
            return;
        }

        SelectCandidate(index);
        _candidateDetailAttributes[index] = attribute;
        UpdateCandidateDetail(index);
        UpdateUwpCodeVisuals();
        e.Handled = true;
    }

    private void SelectCandidate(int index)
    {
        if (index < 0 || index >= _candidateCards.Length)
        {
            return;
        }

        _state.SelectedHomeworldCandidateIndex = index;
        CommitSelectedCandidateAttributes();

        ValidationMessage.Text = string.Empty;
        UpdateSelectionVisuals();
        ValidityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadCandidateCards()
    {
        for (var index = 0; index < _state.HomeworldCandidates.Count && index < _candidateCards.Length; index++)
        {
            var candidate = _state.HomeworldCandidates[index];
            var uwp = GetCandidateUwp(candidate);

            for (var codeIndex = 0; codeIndex < _candidateCodeButtons[index].Length; codeIndex++)
            {
                _candidateCodeButtons[index][codeIndex].Content = uwp[codeIndex].ToString();
            }

            UpdateCandidateDetail(index);
        }

        UpdateUwpCodeVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        for (var index = 0; index < _candidateCards.Length; index++)
        {
            var isSelected = _state.SelectedHomeworldCandidateIndex == index;
            _candidateCards[index].Background = isSelected ? SelectedCardBackground : DefaultCardBackground;
            _candidateCards[index].BorderBrush = isSelected ? SelectedCardBorder : DefaultCardBorder;
            _candidateCards[index].BorderThickness = isSelected ? new Thickness(2) : new Thickness(1);
        }

        UpdateUwpCodeVisuals();
    }

    private void UpdateUwpCodeVisuals()
    {
        for (var candidateIndex = 0; candidateIndex < _candidateCodeButtons.Length; candidateIndex++)
        {
            for (var attributeIndex = 0; attributeIndex < _candidateCodeButtons[candidateIndex].Length; attributeIndex++)
            {
                var button = _candidateCodeButtons[candidateIndex][attributeIndex];
                var isSelected = _candidateDetailAttributes[candidateIndex] == (HomeworldDetailAttribute)attributeIndex;

                button.Background = isSelected ? SelectedCodeBackground : DefaultCodeBackground;
                button.BorderBrush = isSelected ? SelectedCodeBorder : DefaultCodeBorder;
                button.BorderThickness = isSelected ? new Thickness(2) : new Thickness(1);
            }
        }
    }

    private void UpdateCandidateDetail(int index)
    {
        if (index < 0 || index >= _candidateDetailTexts.Length || index >= _state.HomeworldCandidates.Count)
        {
            return;
        }

        var candidate = _state.HomeworldCandidates[index];
        _candidateDetailTexts[index].Text = _candidateDetailAttributes[index] switch
        {
            HomeworldDetailAttribute.WorldSize => BuildWorldSizeDetail(candidate),
            HomeworldDetailAttribute.Atmosphere => BuildAtmosphereDetail(candidate),
            HomeworldDetailAttribute.Hydrographics => BuildHydrographicsDetail(candidate),
            _ => BuildSummaryPlaceholder(candidate)
        };
    }

    private void UpdateRequiredFieldBorders()
    {
        HomeworldNameTextBox.BorderBrush = string.IsNullOrWhiteSpace(HomeworldNameTextBox.Text)
            ? InvalidBorderBrush
            : ValidBorderBrush;
    }

    private static List<HomeworldCandidate> GenerateCandidates()
    {
        return CandidatePool
            .OrderBy(_ => Random.Shared.Next())
            .Take(3)
            .Select(candidate => new HomeworldCandidate
            {
                Name = candidate.Name,
                Archetype = candidate.Archetype,
                Summary = candidate.Summary
            })
            .Select(candidate =>
            {
                candidate.WorldSizeValue = HomeworldGenerator.GenerateWorldSizeValue();
                candidate.AtmosphereValue = HomeworldGenerator.GenerateAtmosphereValue(candidate.WorldSizeValue.Value);
                candidate.TemperatureKey = HomeworldGenerator.GenerateTemperatureKey(candidate.AtmosphereValue.Value);
                candidate.HydrographicsValue = HomeworldGenerator.GenerateHydrographicsValue(
                    candidate.WorldSizeValue.Value,
                    candidate.AtmosphereValue.Value,
                    candidate.TemperatureKey);
                return candidate;
            })
            .ToList();
    }

    private static bool IsValidWorldSize(int? value)
    {
        return value is >= 0 and <= 10;
    }

    private static bool IsValidAtmosphere(int? value)
    {
        return value is >= 0 and <= 15;
    }

    private static bool IsValidTemperature(string? key)
    {
        return TemperatureCatalog.IsValidKey(key);
    }

    private static bool IsValidHydrographics(int? value)
    {
        return value is >= 0 and <= 10;
    }

    private static string GetCandidateUwp(HomeworldCandidate candidate)
    {
        return Homeworld.BuildUwp(
            candidate.WorldSizeValue.GetValueOrDefault(),
            candidate.AtmosphereValue.GetValueOrDefault(),
            candidate.HydrographicsValue.GetValueOrDefault());
    }

    private static string BuildSummaryPlaceholder(HomeworldCandidate candidate)
    {
        return "SUMMARY DATA PENDING\n" +
               "Generated profile values are available from the UWP codes above. Final homeworld summary text will be assembled after the remaining world attributes are generated.";
    }

    private static string BuildWorldSizeDetail(HomeworldCandidate candidate)
    {
        var worldSize = WorldSizeCatalog.FromValue(candidate.WorldSizeValue.GetValueOrDefault());

        return $"WORLD SIZE: {worldSize.Name} ({worldSize.Code})\n" +
               $"DIAMETER: {worldSize.Diameter}\n" +
               $"GRAVITY: {worldSize.SurfaceGravity}";
    }

    private static string BuildAtmosphereDetail(HomeworldCandidate candidate)
    {
        var atmosphere = AtmosphereCatalog.FromValue(candidate.AtmosphereValue.GetValueOrDefault());

        return $"ATMOSPHERE: {atmosphere.Name} ({atmosphere.Code})\n" +
               $"PRESSURE: {atmosphere.Pressure}\n" +
               $"SURVIVAL GEAR: {atmosphere.SurvivalGear}";
    }

    private static string BuildHydrographicsDetail(HomeworldCandidate candidate)
    {
        var hydrographics = HydrographicsCatalog.FromValue(candidate.HydrographicsValue.GetValueOrDefault());

        return $"HYDROGRAPHICS: {hydrographics.Name} ({hydrographics.Code})\n" +
               $"COVERAGE: {hydrographics.Percentage}";
    }

    private void CommitSelectedCandidateAttributes()
    {
        if (_state.SelectedHomeworldCandidateIndex is not { } selectedIndex ||
            selectedIndex < 0 ||
            selectedIndex >= _state.HomeworldCandidates.Count)
        {
            return;
        }

        var selectedCandidate = _state.HomeworldCandidates[selectedIndex];

        if (selectedCandidate.WorldSizeValue.HasValue)
        {
            _state.Character.Homeworld.WorldSizeValue = selectedCandidate.WorldSizeValue.Value;
        }

        if (selectedCandidate.AtmosphereValue.HasValue)
        {
            _state.Character.Homeworld.AtmosphereValue = selectedCandidate.AtmosphereValue.Value;
        }

        if (TemperatureCatalog.IsValidKey(selectedCandidate.TemperatureKey))
        {
            _state.Character.Homeworld.TemperatureKey = selectedCandidate.TemperatureKey;
        }

        if (selectedCandidate.HydrographicsValue.HasValue)
        {
            _state.Character.Homeworld.HydrographicsValue = selectedCandidate.HydrographicsValue.Value;
        }
    }

    private enum HomeworldDetailAttribute
    {
        WorldSize,
        Atmosphere,
        Hydrographics
    }
}

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
    private static readonly Brush AmberCardBackground = (Brush)new BrushConverter().ConvertFromString("#D0443210")!;
    private static readonly Brush RedCardBackground = (Brush)new BrushConverter().ConvertFromString("#D0441515")!;
    private static readonly Brush DefaultCardBorder = (Brush)new BrushConverter().ConvertFromString("#5FD9FF")!;
    private static readonly Brush SelectedCardBackground = (Brush)new BrushConverter().ConvertFromString("#DD1A5574")!;
    private static readonly Brush SelectedAmberCardBackground = (Brush)new BrushConverter().ConvertFromString("#DD5A4418")!;
    private static readonly Brush SelectedRedCardBackground = (Brush)new BrushConverter().ConvertFromString("#DD5A1F1F")!;
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
            [CandidateOneStarportCodeButton, CandidateOneWorldSizeCodeButton, CandidateOneAtmosphereCodeButton, CandidateOneHydrographicsCodeButton, CandidateOnePopulationCodeButton, CandidateOneGovernmentCodeButton, CandidateOneLawLevelCodeButton, CandidateOneTechLevelCodeButton],
            [CandidateTwoStarportCodeButton, CandidateTwoWorldSizeCodeButton, CandidateTwoAtmosphereCodeButton, CandidateTwoHydrographicsCodeButton, CandidateTwoPopulationCodeButton, CandidateTwoGovernmentCodeButton, CandidateTwoLawLevelCodeButton, CandidateTwoTechLevelCodeButton],
            [CandidateThreeStarportCodeButton, CandidateThreeWorldSizeCodeButton, CandidateThreeAtmosphereCodeButton, CandidateThreeHydrographicsCodeButton, CandidateThreePopulationCodeButton, CandidateThreeGovernmentCodeButton, CandidateThreeLawLevelCodeButton, CandidateThreeTechLevelCodeButton]
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
                                                        !IsValidHydrographics(candidate.HydrographicsValue) ||
                                                        !IsValidPopulation(candidate.PopulationValue) ||
                                                        !IsValidStarport(candidate.StarportCode) ||
                                                        !IsValidGovernment(candidate.GovernmentValue) ||
                                                        !IsValidLawLevel(candidate.LawLevelValue) ||
                                                        !IsValidTechLevel(candidate.TechLevelValue) ||
                                                        !IsValidGasGiants(candidate.NumberOfGasGiants) ||
                                                        !IsValidPlanetoidBelts(candidate.NumberOfPlanetoidBelts) ||
                                                        !IsValidTravelCode(candidate.TravelCode) ||
                                                        !IsValidCulturalTags(candidate.PopulationValue, candidate.CulturalTagValues) ||
                                                        !IsValidFactions(candidate.PopulationValue, candidate.GovernmentValue, candidate.Factions) ||
                                                        !IsValidBases(candidate.Bases)))
        {
            _state.HomeworldCandidates = GenerateCandidates();
            _state.SelectedHomeworldCandidateIndex = null;
            _state.Character.Homeworld.WorldSizeValue = 0;
            _state.Character.Homeworld.AtmosphereValue = 0;
            _state.Character.Homeworld.TemperatureKey = TemperatureCatalog.Swinging.Key;
            _state.Character.Homeworld.HydrographicsValue = 0;
            _state.Character.Homeworld.PopulationValue = 0;
            _state.Character.Homeworld.StarportCode = StarportCatalog.None.Code;
            _state.Character.Homeworld.CulturalTagValues = [];
            _state.Character.Homeworld.GovernmentValue = 0;
            _state.Character.Homeworld.LawLevelValue = 0;
            _state.Character.Homeworld.TechLevelValue = 0;
            _state.Character.Homeworld.NumberOfGasGiants = 0;
            _state.Character.Homeworld.NumberOfPlanetoidBelts = 0;
            _state.Character.Homeworld.TravelCode = TravelCodeCatalog.Green.Code;
            _state.Character.Homeworld.Factions = [];
            _state.Character.Homeworld.Bases = new();
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
            var uwpCodes = GetCandidateUwpCodes(candidate);

            for (var codeIndex = 0; codeIndex < _candidateCodeButtons[index].Length; codeIndex++)
            {
                _candidateCodeButtons[index][codeIndex].Content = uwpCodes[codeIndex];
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
            var travelCode = index < _state.HomeworldCandidates.Count
                ? _state.HomeworldCandidates[index].TravelCode
                : TravelCodeCatalog.Green.Code;

            _candidateCards[index].Background = GetCardBackground(travelCode, isSelected);
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
            HomeworldDetailAttribute.Starport => BuildStarportDetail(candidate),
            HomeworldDetailAttribute.WorldSize => BuildWorldSizeDetail(candidate),
            HomeworldDetailAttribute.Atmosphere => BuildAtmosphereDetail(candidate),
            HomeworldDetailAttribute.Hydrographics => BuildHydrographicsDetail(candidate),
            HomeworldDetailAttribute.Population => BuildPopulationDetail(candidate),
            HomeworldDetailAttribute.Government => BuildGovernmentDetail(candidate),
            HomeworldDetailAttribute.LawLevel => BuildLawLevelDetail(candidate),
            HomeworldDetailAttribute.TechLevel => BuildTechLevelDetail(candidate),
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
                candidate.PopulationValue = HomeworldGenerator.GeneratePopulationValue(allowZero: false);
                candidate.StarportCode = HomeworldGenerator.GenerateStarportCode(candidate.PopulationValue.Value);
                candidate.CulturalTagValues = HomeworldGenerator.GenerateCulturalTagValues(candidate.PopulationValue.Value);
                candidate.GovernmentValue = HomeworldGenerator.GenerateGovernmentValue(candidate.PopulationValue.Value);
                candidate.LawLevelValue = HomeworldGenerator.GenerateLawLevelValue(
                    candidate.PopulationValue.Value,
                    candidate.GovernmentValue.Value);
                candidate.TechLevelValue = HomeworldGenerator.GenerateTechLevelValue(
                    candidate.StarportCode,
                    candidate.WorldSizeValue.Value,
                    candidate.AtmosphereValue.Value,
                    candidate.HydrographicsValue.Value,
                    candidate.PopulationValue.Value,
                    candidate.GovernmentValue.Value);
                candidate.Factions = HomeworldGenerator.GenerateFactions(
                    candidate.PopulationValue.Value,
                    candidate.GovernmentValue.Value);
                candidate.Bases = HomeworldGenerator.GenerateBases(
                    candidate.StarportCode,
                    candidate.PopulationValue.Value,
                    candidate.LawLevelValue.Value,
                    candidate.TechLevelValue.Value);
                candidate.NumberOfGasGiants = HomeworldGenerator.GenerateNumberOfGasGiants();
                candidate.NumberOfPlanetoidBelts = HomeworldGenerator.GenerateNumberOfPlanetoidBelts(
                    candidate.NumberOfGasGiants.Value);
                candidate.TravelCode = HomeworldGenerator.GenerateTravelCode(
                    candidate.AtmosphereValue.Value,
                    candidate.GovernmentValue.Value,
                    candidate.LawLevelValue.Value);
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

    private static bool IsValidPopulation(int? value)
    {
        return value is >= 0 and <= 12;
    }

    private static bool IsValidStarport(string? code)
    {
        return StarportCatalog.IsValidCode(code);
    }

    private static bool IsValidGovernment(int? value)
    {
        return value is >= 0 and <= 15;
    }

    private static bool IsValidLawLevel(int? value)
    {
        return value is >= 0 and <= 15;
    }

    private static bool IsValidTechLevel(int? value)
    {
        return value is >= 0 and <= 20;
    }

    private static bool IsValidGasGiants(int? value)
    {
        return value is >= 0 and <= 5;
    }

    private static bool IsValidPlanetoidBelts(int? value)
    {
        return value is >= 0 and <= 3;
    }

    private static bool IsValidTravelCode(string? code)
    {
        return TravelCodeCatalog.IsValidCode(code);
    }

    private static bool IsValidCulturalTags(int? populationValue, List<int>? culturalTagValues)
    {
        if (!populationValue.HasValue || culturalTagValues is null)
        {
            return false;
        }

        if (populationValue.Value == 0)
        {
            return culturalTagValues.Count == 0;
        }

        return culturalTagValues.Count > 0 &&
               culturalTagValues.Distinct().Count() == culturalTagValues.Count &&
               culturalTagValues.All(CulturalTagCatalog.IsValidValue);
    }

    private static bool IsValidFactions(int? populationValue, int? governmentValue, List<HomeworldFaction>? factions)
    {
        if (!populationValue.HasValue || !governmentValue.HasValue || factions is null)
        {
            return false;
        }

        if (populationValue.Value < 1 || governmentValue.Value < 1)
        {
            return factions.Count == 0;
        }

        return factions.Count <= 4 &&
               factions.All(faction =>
                   !string.IsNullOrWhiteSpace(faction.Name) &&
                   FactionCategoryCatalog.IsValidCode(faction.CategoryCode) &&
                   FactionStrengthCatalog.IsValidCode(faction.StrengthCode));
    }

    private static bool IsValidBases(HomeworldBases? bases)
    {
        return bases is not null &&
               (bases.NavalBase || !bases.NavalDepot) &&
               (bases.ScoutBase || !bases.ScoutWayStation);
    }

    private static string GetCandidateUwp(HomeworldCandidate candidate)
    {
        return Homeworld.BuildUwp(
            candidate.StarportCode,
            candidate.WorldSizeValue.GetValueOrDefault(),
            candidate.AtmosphereValue.GetValueOrDefault(),
            candidate.HydrographicsValue.GetValueOrDefault(),
            candidate.PopulationValue.GetValueOrDefault(),
            candidate.GovernmentValue.GetValueOrDefault(),
            candidate.LawLevelValue.GetValueOrDefault(),
            candidate.TechLevelValue.GetValueOrDefault());
    }

    private static string[] GetCandidateUwpCodes(HomeworldCandidate candidate)
    {
        var starport = StarportCatalog.IsValidCode(candidate.StarportCode)
            ? StarportCatalog.FromCode(candidate.StarportCode!)
            : StarportCatalog.None;
        var worldSize = WorldSizeCatalog.FromValue(candidate.WorldSizeValue.GetValueOrDefault());
        var atmosphere = AtmosphereCatalog.FromValue(candidate.AtmosphereValue.GetValueOrDefault());
        var hydrographics = HydrographicsCatalog.FromValue(candidate.HydrographicsValue.GetValueOrDefault());
        var population = PopulationCatalog.FromValue(candidate.PopulationValue.GetValueOrDefault());
        var government = GovernmentCatalog.FromValue(candidate.GovernmentValue.GetValueOrDefault());
        var lawLevel = LawLevelCatalog.FromValue(candidate.LawLevelValue.GetValueOrDefault());
        var techLevel = TechLevelCatalog.FromValue(candidate.TechLevelValue.GetValueOrDefault());

        return
        [
            starport.Code,
            worldSize.Code,
            atmosphere.Code,
            hydrographics.Code,
            population.Code,
            government.Code,
            lawLevel.Code,
            techLevel.Code
        ];
    }

    private static string BuildSummaryPlaceholder(HomeworldCandidate candidate)
    {
        return "SUMMARY DATA PENDING\n" +
               "Generated profile values are available from the UWP codes above. Final homeworld summary text will be assembled after the remaining world attributes are generated.";
    }

    private static string BuildStarportDetail(HomeworldCandidate candidate)
    {
        var starport = StarportCatalog.FromCode(candidate.StarportCode ?? StarportCatalog.None.Code);

        return $"STARPORT: {starport.Name} ({starport.Code})\n" +
               $"BERTHING COST: {starport.BerthingCost}\n" +
               $"FUEL: {starport.AvailableFuel}\n" +
               $"FACILITIES: {starport.Facilities}";
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

    private static string BuildPopulationDetail(HomeworldCandidate candidate)
    {
        var population = PopulationCatalog.FromValue(candidate.PopulationValue.GetValueOrDefault());

        return $"POPULATION: {population.Name} ({population.Code})\n" +
               $"RANGE: {population.Range}";
    }

    private static string BuildGovernmentDetail(HomeworldCandidate candidate)
    {
        var government = GovernmentCatalog.FromValue(candidate.GovernmentValue.GetValueOrDefault());

        return $"GOVERNMENT: {government.Name} ({government.Code})\n" +
               $"DESCRIPTION: {government.Description}\n" +
               $"EXAMPLES: {government.Examples}\n" +
               $"CONTRABAND: {government.Contraband}";
    }

    private static string BuildLawLevelDetail(HomeworldCandidate candidate)
    {
        var lawLevel = LawLevelCatalog.FromValue(candidate.LawLevelValue.GetValueOrDefault());

        return $"LAW LEVEL: {lawLevel.Name} ({lawLevel.Code})\n" +
               $"BANNED WEAPONS: {lawLevel.BannedWeapons}\n" +
               $"BANNED ARMOR: {lawLevel.BannedArmor}";
    }

    private static string BuildTechLevelDetail(HomeworldCandidate candidate)
    {
        var techLevel = TechLevelCatalog.FromValue(candidate.TechLevelValue.GetValueOrDefault());

        return $"TECH LEVEL: {techLevel.Name} ({techLevel.Code})\n" +
               $"ERA: {techLevel.Era}\n" +
               $"DESCRIPTION: {techLevel.Description}";
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

        if (selectedCandidate.PopulationValue.HasValue)
        {
            _state.Character.Homeworld.PopulationValue = selectedCandidate.PopulationValue.Value;
        }

        if (StarportCatalog.IsValidCode(selectedCandidate.StarportCode))
        {
            _state.Character.Homeworld.StarportCode = StarportCatalog.FromCode(selectedCandidate.StarportCode!).Code;
        }

        _state.Character.Homeworld.CulturalTagValues = [.. selectedCandidate.CulturalTagValues];

        if (selectedCandidate.GovernmentValue.HasValue)
        {
            _state.Character.Homeworld.GovernmentValue = selectedCandidate.GovernmentValue.Value;
        }

        if (selectedCandidate.LawLevelValue.HasValue)
        {
            _state.Character.Homeworld.LawLevelValue = selectedCandidate.LawLevelValue.Value;
        }

        if (selectedCandidate.TechLevelValue.HasValue)
        {
            _state.Character.Homeworld.TechLevelValue = selectedCandidate.TechLevelValue.Value;
        }

        if (selectedCandidate.NumberOfGasGiants.HasValue)
        {
            _state.Character.Homeworld.NumberOfGasGiants = selectedCandidate.NumberOfGasGiants.Value;
        }

        if (selectedCandidate.NumberOfPlanetoidBelts.HasValue)
        {
            _state.Character.Homeworld.NumberOfPlanetoidBelts = selectedCandidate.NumberOfPlanetoidBelts.Value;
        }

        if (TravelCodeCatalog.IsValidCode(selectedCandidate.TravelCode))
        {
            _state.Character.Homeworld.TravelCode = TravelCodeCatalog.FromCode(selectedCandidate.TravelCode!).Code;
        }

        _state.Character.Homeworld.Factions = selectedCandidate.Factions is null
            ? []
            : selectedCandidate.Factions
                .Select(faction => new HomeworldFaction
                {
                    Name = faction.Name,
                    CategoryCode = faction.CategoryCode,
                    StrengthCode = faction.StrengthCode
                })
                .ToList();
        _state.Character.Homeworld.Bases = CopyBases(selectedCandidate.Bases);
    }

    private static HomeworldBases CopyBases(HomeworldBases? bases)
    {
        return bases is null
            ? new()
            : new HomeworldBases
            {
                HighPort = bases.HighPort,
                MilitaryBase = bases.MilitaryBase,
                NavalBase = bases.NavalBase,
                NavalDepot = bases.NavalBase && bases.NavalDepot,
                ScoutBase = bases.ScoutBase,
                ScoutWayStation = bases.ScoutBase && bases.ScoutWayStation,
                CorsairBase = bases.CorsairBase
            };
    }

    private static Brush GetCardBackground(string? travelCode, bool isSelected)
    {
        if (!TravelCodeCatalog.IsValidCode(travelCode))
        {
            return isSelected ? SelectedCardBackground : DefaultCardBackground;
        }

        var normalizedCode = TravelCodeCatalog.FromCode(travelCode!).Code;

        return normalizedCode switch
        {
            "A" => isSelected ? SelectedAmberCardBackground : AmberCardBackground,
            "R" => isSelected ? SelectedRedCardBackground : RedCardBackground,
            _ => isSelected ? SelectedCardBackground : DefaultCardBackground
        };
    }

    private enum HomeworldDetailAttribute
    {
        Starport,
        WorldSize,
        Atmosphere,
        Hydrographics,
        Population,
        Government,
        LawLevel,
        TechLevel
    }
}

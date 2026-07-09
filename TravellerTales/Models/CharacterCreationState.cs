namespace TravellerTales.Models;

public sealed class CharacterCreationState
{
    public int CurrentStepIndex { get; set; }
    public Character Character { get; set; } = new();
    public List<HomeworldCandidate> HomeworldCandidates { get; set; } = [];
    public int? SelectedHomeworldCandidateIndex { get; set; }
}

public sealed class HomeworldCandidate
{
    public string Name { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int? WorldSizeValue { get; set; }
    public int? AtmosphereValue { get; set; }
    public string TemperatureKey { get; set; } = string.Empty;
    public int? HydrographicsValue { get; set; }
    public int? PopulationValue { get; set; }
    public string? StarportCode { get; set; }
    public List<int> CulturalTagValues { get; set; } = [];
    public int? GovernmentValue { get; set; }
    public int? LawLevelValue { get; set; }
    public int? TechLevelValue { get; set; }
    public int? NumberOfGasGiants { get; set; }
    public int? NumberOfPlanetoidBelts { get; set; }
    public string? TravelCode { get; set; }
    public List<HomeworldFaction>? Factions { get; set; }
    public HomeworldBases? Bases { get; set; }
}

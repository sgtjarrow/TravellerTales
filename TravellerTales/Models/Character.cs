using System.Text;
using System.Text.Json.Serialization;

namespace TravellerTales.Models;

public sealed class Character
{
    private string _legacyName = string.Empty;

    public string Name
    {
        get => DisplayName;
        set
        {
            _legacyName = value ?? string.Empty;
            ApplyLegacyNameIfNeeded();
        }
    }

    public string HumanFirstName { get; set; } = string.Empty;
    public string HumanMiddleName { get; set; } = string.Empty;
    public string HumanLastName { get; set; } = string.Empty;
    public string HumanSuffix { get; set; } = string.Empty;
    public string AslanFamilyName { get; set; } = string.Empty;
    public string AslanPersonalName { get; set; } = string.Empty;
    public string VargrClanName { get; set; } = string.Empty;
    public VargrRoleType? VargrRole { get; set; }
    public string VargrPersonalName { get; set; } = string.Empty;
    public RaceType Race { get; set; } = RaceType.Human;
    public HeritageType? Heritage { get; set; } = HeritageType.Anglo;
    public GenderType Gender { get; set; } = GenderType.Male;
    public int Age { get; set; } = 18;
    public int HeightInches { get; set; }
    public int WeightPounds { get; set; }
    public double HeightMeters => HeightInches * 0.0254;
    public double WeightKilograms => WeightPounds * 0.45359237;
    public EyeColorType EyeColor { get; set; } = EyeColorType.Brown;
    public SkinColorType SkinColor { get; set; } = SkinColorType.Tan;
    public HairColorType? HairColor { get; set; } = HairColorType.Brown;
    public FurPatternType? FurPattern { get; set; }
    public FurColorType? FurPrimaryColor { get; set; }
    public FurColorType? FurSecondaryColor { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string HomeworldId { get; set; } = string.Empty;
    public Homeworld Homeworld { get; set; } = new();
    public CharacterCreationMetadata CreationMetadata { get; set; } = new();

    [JsonIgnore]
    public string DisplayName => Race switch
    {
        RaceType.Aslan => JoinNameParts(AslanFamilyName, AslanPersonalName),
        RaceType.Vargr => BuildVargrDisplayName(),
        _ => JoinNameParts(HumanFirstName, HumanMiddleName, HumanLastName, HumanSuffix)
    };

    [JsonIgnore]
    public string SanitizedDisplayName => Sanitize(DisplayName);

    [JsonIgnore]
    public bool IsNameComplete => Race switch
    {
        RaceType.Aslan => !string.IsNullOrWhiteSpace(AslanFamilyName) &&
                          !string.IsNullOrWhiteSpace(AslanPersonalName),
        RaceType.Vargr => !string.IsNullOrWhiteSpace(VargrClanName) &&
                          VargrRole.HasValue &&
                          !string.IsNullOrWhiteSpace(VargrPersonalName),
        _ => !string.IsNullOrWhiteSpace(HumanFirstName) &&
             !string.IsNullOrWhiteSpace(HumanLastName)
    };

    public void NormalizeAfterLoad()
    {
        Homeworld ??= new();
        HomeworldId = string.IsNullOrWhiteSpace(HomeworldId) ? Homeworld.Id : HomeworldId;
        Homeworld.Id = string.IsNullOrWhiteSpace(Homeworld.Id) ? HomeworldId : Homeworld.Id;
        Homeworld.WorldSizeValue = Math.Clamp(Homeworld.WorldSizeValue, 0, 10);
        Homeworld.AtmosphereValue = Math.Clamp(Homeworld.AtmosphereValue, 0, 15);
        Homeworld.TemperatureKey = TemperatureCatalog.IsValidKey(Homeworld.TemperatureKey)
            ? Homeworld.TemperatureKey
            : TemperatureCatalog.Swinging.Key;
        Homeworld.HydrographicsValue = Math.Clamp(Homeworld.HydrographicsValue, 0, 10);
        Homeworld.PopulationValue = Math.Clamp(Homeworld.PopulationValue, 0, 12);
        Homeworld.StarportCode = StarportCatalog.IsValidCode(Homeworld.StarportCode)
            ? StarportCatalog.FromCode(Homeworld.StarportCode).Code
            : StarportCatalog.None.Code;
        Homeworld.GovernmentValue = Math.Clamp(Homeworld.GovernmentValue, 0, 15);
        Homeworld.LawLevelValue = Math.Clamp(Homeworld.LawLevelValue, 0, 15);
        Homeworld.TechLevelValue = Math.Clamp(Homeworld.TechLevelValue, 0, 20);
        Homeworld.CulturalTagValues = (Homeworld.CulturalTagValues ?? [])
            .Where(CulturalTagCatalog.IsValidValue)
            .Distinct()
            .ToList();
        Homeworld.Factions = NormalizeFactions(Homeworld.PopulationValue, Homeworld.GovernmentValue, Homeworld.Factions);
        ApplyLegacyNameIfNeeded();
    }

    private static List<HomeworldFaction> NormalizeFactions(int populationValue, int governmentValue, List<HomeworldFaction>? factions)
    {
        if (populationValue < 1 || governmentValue < 1 || factions is null)
        {
            return [];
        }

        var normalizedFactions = new List<HomeworldFaction>();

        foreach (var faction in factions)
        {
            if (!FactionCategoryCatalog.IsValidCode(faction.CategoryCode) ||
                !FactionStrengthCatalog.IsValidCode(faction.StrengthCode))
            {
                continue;
            }

            normalizedFactions.Add(new HomeworldFaction
            {
                Name = string.IsNullOrWhiteSpace(faction.Name)
                    ? $"Faction {normalizedFactions.Count + 1}"
                    : faction.Name.Trim(),
                CategoryCode = FactionCategoryCatalog.FromCode(faction.CategoryCode).Code,
                StrengthCode = FactionStrengthCatalog.FromCode(faction.StrengthCode).Code
            });

            if (normalizedFactions.Count == 4)
            {
                break;
            }
        }

        return normalizedFactions;
    }

    private void ApplyLegacyNameIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(_legacyName) || IsNameComplete)
        {
            return;
        }

        var parts = _legacyName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (Race == RaceType.Aslan)
        {
            AslanFamilyName = parts.Length > 0 ? parts[0] : string.Empty;
            AslanPersonalName = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;
            return;
        }

        if (Race == RaceType.Vargr)
        {
            ApplyLegacyVargrName(parts);
            return;
        }

        ApplyLegacyHumanName(parts);
    }

    private void ApplyLegacyHumanName(string[] parts)
    {
        if (parts.Length == 0)
        {
            return;
        }

        var suffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Jr",
            "Jr.",
            "Sr",
            "Sr.",
            "II",
            "III",
            "IV",
            "Esq",
            "Esq."
        };

        var lastIndex = parts.Length - 1;
        if (suffixes.Contains(parts[lastIndex]))
        {
            HumanSuffix = parts[lastIndex].TrimEnd('.');
            lastIndex--;
        }

        HumanFirstName = parts[0];
        HumanLastName = lastIndex > 0 ? parts[lastIndex] : string.Empty;
        HumanMiddleName = lastIndex > 1 ? string.Join(' ', parts.Skip(1).Take(lastIndex - 1)) : string.Empty;
    }

    private void ApplyLegacyVargrName(string[] parts)
    {
        if (parts.Length == 0)
        {
            return;
        }

        VargrClanName = parts[0];
        var roleAndPersonal = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;
        var split = roleAndPersonal.Split('-', 2, StringSplitOptions.TrimEntries);

        if (split.Length > 0)
        {
            VargrRole = VargrRoleCatalog.GetRoleBySound(split[0]);
        }

        if (split.Length > 1)
        {
            VargrPersonalName = split[1];
        }
    }

    private string BuildVargrDisplayName()
    {
        var roleSound = VargrRole.HasValue ? VargrRoleCatalog.GetSound(VargrRole.Value) : string.Empty;
        var roleAndPersonal = string.IsNullOrWhiteSpace(roleSound)
            ? VargrPersonalName
            : $"{roleSound}-{VargrPersonalName}".TrimEnd('-');

        return JoinNameParts(VargrClanName, roleAndPersonal);
    }

    private static string JoinNameParts(params string?[] parts)
    {
        return string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }

    private static string Sanitize(string name)
    {
        var builder = new StringBuilder();

        foreach (var character in name)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}

public sealed class Homeworld
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int WorldSizeValue { get; set; }
    public int AtmosphereValue { get; set; }
    public string TemperatureKey { get; set; } = TemperatureCatalog.SwingingKey;
    public int HydrographicsValue { get; set; }
    public int PopulationValue { get; set; }
    public string StarportCode { get; set; } = StarportCatalog.None.Code;
    public List<int> CulturalTagValues { get; set; } = [];
    public int GovernmentValue { get; set; }
    public int LawLevelValue { get; set; }
    public int TechLevelValue { get; set; }
    public List<HomeworldFaction> Factions { get; set; } = [];

    [JsonIgnore]
    public string Uwp => BuildUwp(StarportCode, WorldSizeValue, AtmosphereValue, HydrographicsValue, PopulationValue, GovernmentValue, LawLevelValue, TechLevelValue);

    public static string BuildUwp(string? starportCode, int worldSizeValue, int atmosphereValue, int hydrographicsValue, int populationValue, int governmentValue, int lawLevelValue, int techLevelValue)
    {
        var starport = StarportCatalog.IsValidCode(starportCode)
            ? StarportCatalog.FromCode(starportCode!)
            : StarportCatalog.None;
        var worldSize = WorldSizeCatalog.FromValue(Math.Clamp(worldSizeValue, 0, 10));
        var atmosphere = AtmosphereCatalog.FromValue(Math.Clamp(atmosphereValue, 0, 15));
        var hydrographics = HydrographicsCatalog.FromValue(Math.Clamp(hydrographicsValue, 0, 10));
        var population = PopulationCatalog.FromValue(Math.Clamp(populationValue, 0, 12));
        var government = GovernmentCatalog.FromValue(Math.Clamp(governmentValue, 0, 15));
        var lawLevel = LawLevelCatalog.FromValue(Math.Clamp(lawLevelValue, 0, 15));
        var techLevel = TechLevelCatalog.FromValue(Math.Clamp(techLevelValue, 0, 20));

        return $"{starport.Code}{worldSize.Code}{atmosphere.Code}{hydrographics.Code}{population.Code}{government.Code}{lawLevel.Code}-{techLevel.Code}";
    }
}

public sealed class HomeworldFaction
{
    public string Name { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string StrengthCode { get; set; } = string.Empty;
}

public sealed record WorldSizeDefinition(
    string Name,
    int Value,
    string Code,
    string Diameter,
    string SurfaceGravity,
    int TechLevelModifier);

public static class WorldSizeCatalog
{
    public static readonly WorldSizeDefinition AsteroidBelt = new("Asteroid Belt", 0, "0", "Less Than 1,000 km", "None", 2);
    public static readonly WorldSizeDefinition Minuscule = new("Minuscule", 1, "1", "1,600 km", "0.05 g", 2);
    public static readonly WorldSizeDefinition Tiny = new("Tiny", 2, "2", "3,200 km", "0.15 g", 1);
    public static readonly WorldSizeDefinition Small = new("Small", 3, "3", "4,800 km", "0.25 g", 1);
    public static readonly WorldSizeDefinition Modest = new("Modest", 4, "4", "6,400 km", "0.35 g", 1);
    public static readonly WorldSizeDefinition Medium = new("Medium", 5, "5", "8,000 km", "0.45 g", 0);
    public static readonly WorldSizeDefinition Large = new("Large", 6, "6", "9,600 km", "0.70 g", 0);
    public static readonly WorldSizeDefinition Huge = new("Huge", 7, "7", "11,200 km", "0.90 g", 0);
    public static readonly WorldSizeDefinition Vast = new("Vast", 8, "8", "12,800 km", "1.00 g", 0);
    public static readonly WorldSizeDefinition Enormous = new("Enormous", 9, "9", "14,400 km", "1.25 g", 0);
    public static readonly WorldSizeDefinition Colossal = new("Colossal", 10, "A", "16,000 km", "1.40 g", 0);

    public static IReadOnlyList<WorldSizeDefinition> All { get; } =
    [
        AsteroidBelt,
        Minuscule,
        Tiny,
        Small,
        Modest,
        Medium,
        Large,
        Huge,
        Vast,
        Enormous,
        Colossal
    ];

    public static WorldSizeDefinition FromValue(int value)
    {
        return value switch
        {
            0 => AsteroidBelt,
            1 => Minuscule,
            2 => Tiny,
            3 => Small,
            4 => Modest,
            5 => Medium,
            6 => Large,
            7 => Huge,
            8 => Vast,
            9 => Enormous,
            10 => Colossal,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "World size value must be 0..10.")
        };
    }
}

public sealed record AtmosphereDefinition(
    string Name,
    int Value,
    string Code,
    string Pressure,
    string SurvivalGear,
    int TemperatureModifier,
    int HydrographicsModifier,
    int MinimumTechLevel,
    int TechLevelModifier);

public static class AtmosphereCatalog
{
    public static readonly AtmosphereDefinition None = new("None", 0, "0", "0.00 bars", "Vacc Suit", 0, -4, 8, 1);
    public static readonly AtmosphereDefinition Trace = new("Trace", 1, "1", "0.05 bars", "Vacc Suit", 0, -4, 8, 1);
    public static readonly AtmosphereDefinition VeryThinTainted = new("Very Thin, Tainted", 2, "2", "0.25 bars", "Filter, Respirator", -2, 0, 5, 1);
    public static readonly AtmosphereDefinition VeryThin = new("Very Thin", 3, "3", "0.25 bars", "Respirator", -2, 0, 5, 1);
    public static readonly AtmosphereDefinition ThinTainted = new("Thin, Tainted", 4, "4", "0.60 bars", "Filter", -1, 0, 3, 0);
    public static readonly AtmosphereDefinition Thin = new("Thin", 5, "5", "0.60 bars", "None", -1, 0, 0, 0);
    public static readonly AtmosphereDefinition Standard = new("Standard", 6, "6", "1.00 bars", "None", 0, 0, 0, 0);
    public static readonly AtmosphereDefinition StandardTainted = new("Standard, Tainted", 7, "7", "1.00 bars", "Filter", 0, 0, 3, 0);
    public static readonly AtmosphereDefinition Dense = new("Dense", 8, "8", "2.00 bars", "None", 1, 0, 0, 0);
    public static readonly AtmosphereDefinition DenseTainted = new("Dense, Tainted", 9, "9", "2.00 bars", "Filter", 1, 0, 3, 0);
    public static readonly AtmosphereDefinition Exotic = new("Exotic", 10, "A", "Varies", "Air Supply", 2, -4, 8, 1);
    public static readonly AtmosphereDefinition Corrosive = new("Corrosive", 11, "B", "Varies", "Vacc Suit", 6, -4, 9, 1);
    public static readonly AtmosphereDefinition Insidious = new("Insidious", 12, "C", "Varies", "Vacc Suit", 6, -4, 10, 1);
    public static readonly AtmosphereDefinition VeryDense = new("Very Dense", 13, "D", "Greater Than 2.50 bars", "None", 2, -4, 5, 1);
    public static readonly AtmosphereDefinition Low = new("Low", 14, "E", "Less Than 0.50 bars", "None", -1, -4, 5, 1);
    public static readonly AtmosphereDefinition Unusual = new("Unusual", 15, "F", "Varies", "Varies", 2, -4, 8, 1);

    public static IReadOnlyList<AtmosphereDefinition> All { get; } =
    [
        None,
        Trace,
        VeryThinTainted,
        VeryThin,
        ThinTainted,
        Thin,
        Standard,
        StandardTainted,
        Dense,
        DenseTainted,
        Exotic,
        Corrosive,
        Insidious,
        VeryDense,
        Low,
        Unusual
    ];

    public static AtmosphereDefinition FromValue(int value)
    {
        return value switch
        {
            0 => None,
            1 => Trace,
            2 => VeryThinTainted,
            3 => VeryThin,
            4 => ThinTainted,
            5 => Thin,
            6 => Standard,
            7 => StandardTainted,
            8 => Dense,
            9 => DenseTainted,
            10 => Exotic,
            11 => Corrosive,
            12 => Insidious,
            13 => VeryDense,
            14 => Low,
            15 => Unusual,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Atmosphere value must be 0..15.")
        };
    }
}

public sealed record TemperatureDefinition(
    string Key,
    string Name,
    string Range,
    int HydrographicsModifier);

public static class TemperatureCatalog
{
    public const string FrozenKey = "Frozen";
    public const string ColdKey = "Cold";
    public const string TemperateKey = "Temperate";
    public const string HotKey = "Hot";
    public const string BoilingKey = "Boiling";
    public const string SwingingKey = "Swinging";

    public static readonly TemperatureDefinition Frozen = new(FrozenKey, "Frozen", "Less than -51 C", 0);
    public static readonly TemperatureDefinition Cold = new(ColdKey, "Cold", "-50 C to -1 C", 0);
    public static readonly TemperatureDefinition Temperate = new(TemperateKey, "Temperate", "0 C to 30 C", 0);
    public static readonly TemperatureDefinition Hot = new(HotKey, "Hot", "31 C to 80 C", -2);
    public static readonly TemperatureDefinition Boiling = new(BoilingKey, "Boiling", "Greater than 80 C", -6);
    public static readonly TemperatureDefinition Swinging = new(SwingingKey, "Swinging", "Swings wildly from day to night", 0);

    public static IReadOnlyList<TemperatureDefinition> All { get; } =
    [
        Frozen,
        Cold,
        Temperate,
        Hot,
        Boiling,
        Swinging
    ];

    public static TemperatureDefinition FromKey(string? key)
    {
        return key switch
        {
            FrozenKey => Frozen,
            ColdKey => Cold,
            TemperateKey => Temperate,
            HotKey => Hot,
            BoilingKey => Boiling,
            SwingingKey => Swinging,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Temperature key is invalid.")
        };
    }

    public static bool IsValidKey(string? key)
    {
        return All.Any(temperature => string.Equals(temperature.Key, key, StringComparison.Ordinal));
    }
}

public sealed record HydrographicsDefinition(
    string Name,
    int Value,
    string Code,
    string Percentage,
    int TechLevelModifier);

public static class HydrographicsCatalog
{
    public static readonly HydrographicsDefinition Desert = new("Desert", 0, "0", "0% to 5%", 1);
    public static readonly HydrographicsDefinition Desiccated = new("Desiccated", 1, "1", "6% to 15%", 0);
    public static readonly HydrographicsDefinition Arid = new("Arid", 2, "2", "16% to 25%", 0);
    public static readonly HydrographicsDefinition Sparse = new("Sparse", 3, "3", "26% to 35%", 0);
    public static readonly HydrographicsDefinition Limited = new("Limited", 4, "4", "36% to 45%", 0);
    public static readonly HydrographicsDefinition Partial = new("Partial", 5, "5", "46% to 55%", 0);
    public static readonly HydrographicsDefinition Balanced = new("Balanced", 6, "6", "56% to 65%", 0);
    public static readonly HydrographicsDefinition Abundant = new("Abundant", 7, "7", "66% to 75%", 0);
    public static readonly HydrographicsDefinition Dominant = new("Dominant", 8, "8", "76% to 85%", 0);
    public static readonly HydrographicsDefinition Oceanic = new("Oceanic", 9, "9", "86% to 95%", 1);
    public static readonly HydrographicsDefinition Aquaplanet = new("Aquaplanet", 10, "A", "96% to 100%", 1);

    public static IReadOnlyList<HydrographicsDefinition> All { get; } =
    [
        Desert,
        Desiccated,
        Arid,
        Sparse,
        Limited,
        Partial,
        Balanced,
        Abundant,
        Dominant,
        Oceanic,
        Aquaplanet
    ];

    public static HydrographicsDefinition FromValue(int value)
    {
        return value switch
        {
            0 => Desert,
            1 => Desiccated,
            2 => Arid,
            3 => Sparse,
            4 => Limited,
            5 => Partial,
            6 => Balanced,
            7 => Abundant,
            8 => Dominant,
            9 => Oceanic,
            10 => Aquaplanet,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Hydrographics value must be 0..10.")
        };
    }

    public static HydrographicsDefinition FromCode(string code)
    {
        return All.FirstOrDefault(hydrographics => string.Equals(hydrographics.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown hydrographics code: '{code}'.", nameof(code));
    }
}

public sealed record PopulationDefinition(
    string Name,
    int Value,
    string Code,
    string Range,
    int StarportModifier,
    int TechLevelModifier,
    int HighportModifier);

public static class PopulationCatalog
{
    public static readonly PopulationDefinition None = new("None", 0, "0", "0", -2, 0, -1);
    public static readonly PopulationDefinition Few = new("Few", 1, "1", "1 to 99", -2, 1, -1);
    public static readonly PopulationDefinition Hundreds = new("Hundreds", 2, "2", "100 to 999", -2, 1, -1);
    public static readonly PopulationDefinition Thousands = new("Thousands", 3, "3", "1,000 to 9,999", -1, 1, -1);
    public static readonly PopulationDefinition TensOfThousands = new("Tens of Thousands", 4, "4", "10,000 to 99,999", -1, 1, -1);
    public static readonly PopulationDefinition HundredsOfThousands = new("Hundreds of Thousands", 5, "5", "100,000 to 999,999", 0, 1, -1);
    public static readonly PopulationDefinition Millions = new("Millions", 6, "6", "1,000,000 to 9,999,999", 0, 0, -1);
    public static readonly PopulationDefinition TensOfMillions = new("Tens of Millions", 7, "7", "10,000,000 to 99,999,999", 0, 0, 0);
    public static readonly PopulationDefinition HundredsOfMillions = new("Hundreds of Millions", 8, "8", "100,000,000 to 999,999,999", 1, 1, 0);
    public static readonly PopulationDefinition Billions = new("Billions", 9, "9", "1,000,000,000 to 9,999,999,999", 1, 2, 1);
    public static readonly PopulationDefinition TensOfBillions = new("Tens of Billions", 10, "A", "10,000,000,000 to 99,999,999,999", 2, 3, 1);
    public static readonly PopulationDefinition HundredsOfBillions = new("Hundreds of Billions", 11, "B", "100,000,000,000 to 999,999,999,999", 2, 4, 1);
    public static readonly PopulationDefinition Trillions = new("Trillions", 12, "C", "1,000,000,000,000 to 9,999,999,999,999", 2, 5, 1);

    public static IReadOnlyList<PopulationDefinition> All { get; } =
    [
        None,
        Few,
        Hundreds,
        Thousands,
        TensOfThousands,
        HundredsOfThousands,
        Millions,
        TensOfMillions,
        HundredsOfMillions,
        Billions,
        TensOfBillions,
        HundredsOfBillions,
        Trillions
    ];

    public static PopulationDefinition FromValue(int value)
    {
        return value switch
        {
            0 => None,
            1 => Few,
            2 => Hundreds,
            3 => Thousands,
            4 => TensOfThousands,
            5 => HundredsOfThousands,
            6 => Millions,
            7 => TensOfMillions,
            8 => HundredsOfMillions,
            9 => Billions,
            10 => TensOfBillions,
            11 => HundredsOfBillions,
            12 => Trillions,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Population value must be 0..12.")
        };
    }

    public static PopulationDefinition FromCode(string code)
    {
        return All.FirstOrDefault(population => string.Equals(population.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown population code: '{code}'.", nameof(code));
    }
}

public sealed record StarportDefinition(
    string Name,
    string Code,
    string BerthingCost,
    string AvailableFuel,
    string Facilities,
    int TechLevelModifier,
    int HighportBaseChance,
    int MilitaryBaseChance,
    int NavalBaseChance,
    int ScoutBaseChance,
    int CorsairBaseChance);

public static class StarportCatalog
{
    public const int NoChance = 999;

    public static readonly StarportDefinition Excellent = new("Excellent", "A", "Cr 1,000 to Cr 6,000", "Refined", "Shipyard (All)", 6, 6, 8, 8, 10, NoChance);
    public static readonly StarportDefinition Good = new("Good", "B", "Cr 500 to Cr 3,000", "Refined", "Shipyard (Spacecraft)", 4, 8, 8, 8, 9, NoChance);
    public static readonly StarportDefinition Routine = new("Routine", "C", "Cr 100 to Cr 500", "Unrefined", "Shipyard (Small Craft)", 2, 10, 10, NoChance, 9, NoChance);
    public static readonly StarportDefinition Poor = new("Poor", "D", "Cr 10 to Cr 60", "Unrefined", "Limited Repair", 0, 12, NoChance, NoChance, 8, 12);
    public static readonly StarportDefinition Frontier = new("Frontier", "E", "Cr 0", "None", "None", 0, NoChance, NoChance, NoChance, NoChance, 10);
    public static readonly StarportDefinition None = new("None", "X", "None", "None", "None", -4, NoChance, NoChance, NoChance, NoChance, 10);
    public static readonly StarportDefinition Beacon = new("Beacon", "Z", "None", "None", "Beacon", -4, NoChance, NoChance, NoChance, NoChance, 10);

    public static IReadOnlyList<StarportDefinition> All { get; } =
    [
        Excellent,
        Good,
        Routine,
        Poor,
        Frontier,
        None,
        Beacon
    ];

    public static StarportDefinition FromCode(string code)
    {
        return All.FirstOrDefault(starport => string.Equals(starport.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown starport code: '{code}'.", nameof(code));
    }

    public static bool IsValidCode(string? code)
    {
        return !string.IsNullOrWhiteSpace(code) &&
               All.Any(starport => string.Equals(starport.Code, code, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record GovernmentDefinition(
    string Name,
    int Value,
    string Code,
    string Description,
    string Examples,
    string Contraband,
    int FactionCountModifier,
    int TechLevelModifier);

public static class GovernmentCatalog
{
    public static readonly GovernmentDefinition None = new("None", 0, "0", "No government structure", "Family, Clan, Anarchy", "None", 0, 0);
    public static readonly GovernmentDefinition CompanyCorporation = new("Company/Corporation", 1, "1", "Company managerial elite; most citizens are employees or dependants", "Corporate Outpost, Asteroid Mine", "Drugs, Travellers, Weapons", 0, 0);
    public static readonly GovernmentDefinition ParticipatingDemocracy = new("Participating Democracy", 2, "2", "Advice and consent of the citizens directly", "Collective, Tribal Council", "Drugs", 0, 0);
    public static readonly GovernmentDefinition SelfPerpetuatingOligarchy = new("Self-Perpetuating Oligarchy", 3, "3", "A restricted minority with little or no input from the citizenry", "Plutocracy, Hereditary Ruling Caste", "Technology, Travellers, Weapons", 0, 0);
    public static readonly GovernmentDefinition RepresentativeDemocracy = new("Representative Democracy", 4, "4", "Elected representatives", "Republic, Democracy", "Drugs, Psionics, Weapons", 0, 0);
    public static readonly GovernmentDefinition FeudalTechnocracy = new("Feudal Technocracy", 5, "5", "Ruled by those who control and advance technical expertise", "Access to Technology Grants Status", "Information, Technology, Weapons", 0, 1);
    public static readonly GovernmentDefinition CaptiveGovernment = new("Captive Government", 6, "6", "Leadership is answerable to an outside group", "Colony, Conquered Area", "Technology, Travellers, Weapons", 0, 0);
    public static readonly GovernmentDefinition Balkanization = new("Balkanization", 7, "7", "No central ruling authority", "Multiple Governments, Civil War", "Varies", 1, 2);
    public static readonly GovernmentDefinition CivilServiceBureaucracy = new("Civil Service Bureaucracy", 8, "8", "Agencies employing individuals selected for their expertise", "Technocracy, Communism", "Drugs, Weapons", 0, 0);
    public static readonly GovernmentDefinition ImpersonalBureaucracy = new("Impersonal Bureaucracy", 9, "9", "Agencies insulated from those they govern", "Entrenched Caste of Bureaucrats, Decaying Empire", "Drugs, Psionics, Technology, Travellers, Weapons", 0, 0);
    public static readonly GovernmentDefinition CharismaticDictator = new("Charismatic Dictator", 10, "A", "Agencies directed by a single leader with overwhelming confidence of the citizenry", "Revolutionary Leader, Messiah, Emperor", "None", -1, 0);
    public static readonly GovernmentDefinition NonCharismaticLeader = new("Non-Charismatic Leader", 11, "B", "A previous charismatic leader has been replaced by normal processes", "Military Dictatorship, Hereditary Kingship", "Information, Technology, Weapons", -1, 0);
    public static readonly GovernmentDefinition CharismaticOligarchy = new("Charismatic Oligarchy", 12, "C", "A select group of individuals who have overwhelming confidence of the citizenry", "Junta, Revolutionary Council", "Weapons", -1, 0);
    public static readonly GovernmentDefinition ReligiousDictatorship = new("Religious Dictatorship", 13, "D", "A religious organization ruling without regard for the needs of the citizenry", "Cult, Transcendent Philosophy, Psionic Group Mind", "Varies", -1, -2);
    public static readonly GovernmentDefinition ReligiousAutocracy = new("Religious Autocracy", 14, "E", "A single religious leader having absolute power", "Messiah", "Varies", -1, -2);
    public static readonly GovernmentDefinition TotalitarianOligarchy = new("Totalitarian Oligarchy", 15, "F", "An all-powerful minority that maintains absolute control through widespread coercion and oppression", "World Church, Ruthless Corporation", "Varies", -1, 0);

    public static IReadOnlyList<GovernmentDefinition> All { get; } =
    [
        None,
        CompanyCorporation,
        ParticipatingDemocracy,
        SelfPerpetuatingOligarchy,
        RepresentativeDemocracy,
        FeudalTechnocracy,
        CaptiveGovernment,
        Balkanization,
        CivilServiceBureaucracy,
        ImpersonalBureaucracy,
        CharismaticDictator,
        NonCharismaticLeader,
        CharismaticOligarchy,
        ReligiousDictatorship,
        ReligiousAutocracy,
        TotalitarianOligarchy
    ];

    public static GovernmentDefinition FromValue(int value)
    {
        return value switch
        {
            0 => None,
            1 => CompanyCorporation,
            2 => ParticipatingDemocracy,
            3 => SelfPerpetuatingOligarchy,
            4 => RepresentativeDemocracy,
            5 => FeudalTechnocracy,
            6 => CaptiveGovernment,
            7 => Balkanization,
            8 => CivilServiceBureaucracy,
            9 => ImpersonalBureaucracy,
            10 => CharismaticDictator,
            11 => NonCharismaticLeader,
            12 => CharismaticOligarchy,
            13 => ReligiousDictatorship,
            14 => ReligiousAutocracy,
            15 => TotalitarianOligarchy,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Government value must be 0..15.")
        };
    }

    public static GovernmentDefinition FromCode(string code)
    {
        return All.FirstOrDefault(government => string.Equals(government.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown government code: '{code}'.", nameof(code));
    }
}

public sealed record LawLevelDefinition(
    string Name,
    int Value,
    string Code,
    string BannedWeapons,
    string BannedArmor,
    int CorsairBaseModifier);

public static class LawLevelCatalog
{
    public static readonly LawLevelDefinition None = new("None", 0, "0", "None", "None", 2);
    public static readonly LawLevelDefinition Anarchic = new("Anarchic", 1, "1", "Poison Gas, Explosive, Undetectable Weapons, WMDs", "Battle Dress", 0);
    public static readonly LawLevelDefinition Loose = new("Loose", 2, "2", "Portable Energy and Laser Weapons", "Combat Armor", -2);
    public static readonly LawLevelDefinition Relaxed = new("Relaxed", 3, "3", "Military Weapons", "Flak", -2);
    public static readonly LawLevelDefinition Moderate = new("Moderate", 4, "4", "Light Assault Weapons and Submachineguns", "Cloth", -2);
    public static readonly LawLevelDefinition Structured = new("Structured", 5, "5", "Personal Concealable Weapons", "Mesh", -2);
    public static readonly LawLevelDefinition Restrictive = new("Restrictive", 6, "6", "All Firearms Except Shotguns and Stunners", "Mesh", -2);
    public static readonly LawLevelDefinition Controlled = new("Controlled", 7, "7", "Shotguns", "Mesh", -2);
    public static readonly LawLevelDefinition Regulated = new("Regulated", 8, "8", "All Bladed Weapons and Stunners", "All Visible Armor", -2);
    public static readonly LawLevelDefinition Intrusive = new("Intrusive", 9, "9", "All Weapons", "All Armor", -2);
    public static readonly LawLevelDefinition Authoritarian = new("Authoritarian", 10, "A", "All Weapons", "All Armor", -2);
    public static readonly LawLevelDefinition Repressive = new("Repressive", 11, "B", "All Weapons", "All Armor", -2);
    public static readonly LawLevelDefinition Oppressive = new("Oppressive", 12, "C", "All Weapons", "All Armor", -2);
    public static readonly LawLevelDefinition Tyrannical = new("Tyrannical", 13, "D", "All Weapons", "All Armor", -2);
    public static readonly LawLevelDefinition Totalitarian = new("Totalitarian", 14, "E", "All Weapons", "All Armor", -2);
    public static readonly LawLevelDefinition Absolute = new("Absolute", 15, "F", "All Weapons", "All Armor", -2);

    public static IReadOnlyList<LawLevelDefinition> All { get; } =
    [
        None,
        Anarchic,
        Loose,
        Relaxed,
        Moderate,
        Structured,
        Restrictive,
        Controlled,
        Regulated,
        Intrusive,
        Authoritarian,
        Repressive,
        Oppressive,
        Tyrannical,
        Totalitarian,
        Absolute
    ];

    public static LawLevelDefinition FromValue(int value)
    {
        return value switch
        {
            0 => None,
            1 => Anarchic,
            2 => Loose,
            3 => Relaxed,
            4 => Moderate,
            5 => Structured,
            6 => Restrictive,
            7 => Controlled,
            8 => Regulated,
            9 => Intrusive,
            10 => Authoritarian,
            11 => Repressive,
            12 => Oppressive,
            13 => Tyrannical,
            14 => Totalitarian,
            15 => Absolute,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Law level value must be 0..15.")
        };
    }

    public static LawLevelDefinition FromCode(string code)
    {
        return All.FirstOrDefault(lawLevel => string.Equals(lawLevel.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown law level code: '{code}'.", nameof(code));
    }
}

public sealed record TechLevelDefinition(
    string Name,
    int Value,
    string Code,
    string Era,
    string Description,
    int HighportModifier);

public static class TechLevelCatalog
{
    public static readonly TechLevelDefinition TL0 = new("TL0", 0, "0", "Primitive", "Simple tools and principles", 0);
    public static readonly TechLevelDefinition TL1 = new("TL1", 1, "1", "Primitive", "Bronze or iron materials; science is mostly superstition", 0);
    public static readonly TechLevelDefinition TL2 = new("TL2", 2, "2", "Primitive", "Renaissance knowledge with scientific method and greater understanding of the -ologies", 0);
    public static readonly TechLevelDefinition TL3 = new("TL3", 3, "3", "Primitive", "Steam power and primitive firearms", 0);
    public static readonly TechLevelDefinition TL4 = new("TL4", 4, "4", "Industrial", "Plastics and radio", 0);
    public static readonly TechLevelDefinition TL5 = new("TL5", 5, "5", "Industrial", "Worldwide telecommunications and internal combustion; some atomics and primitive computing", 0);
    public static readonly TechLevelDefinition TL6 = new("TL6", 6, "6", "Industrial", "Fission power and advanced computing, better materials and rocketry", 0);
    public static readonly TechLevelDefinition TL7 = new("TL7", 7, "7", "Pre-Stellar", "Can reach orbit easily and has a network of satellites; integrated circuitry is common", 0);
    public static readonly TechLevelDefinition TL8 = new("TL8", 8, "8", "Pre-Stellar", "Can travel within the solar system, has permanent space colonies, and fusion power is commercially available", 0);
    public static readonly TechLevelDefinition TL9 = new("TL9", 9, "9", "Pre-Stellar", "Gravity manipulation brings the Jump Drive; colonization is possible but usually a one-way voyage", 1);
    public static readonly TechLevelDefinition TL10 = new("TL10", 10, "A", "Early Stellar", "Nearby systems are reachable; colonization is much more viable; orbital habitats, factories, and interstellar trade bring economic boom", 1);
    public static readonly TechLevelDefinition TL11 = new("TL11", 11, "B", "Early Stellar", "The first true artificial intelligence is born and gravity-supported structures become possible; better Jump Drives allow faster and farther travel", 1);
    public static readonly TechLevelDefinition TL12 = new("TL12", 12, "C", "Average Stellar", "Planetwide weather control practices bring great terraforming opportunities; battlefields are dominated by plasma weapons", 2);
    public static readonly TechLevelDefinition TL13 = new("TL13", 13, "D", "Average Stellar", "Battle Dress is born to protect from harsh battlefields; cloning becomes possible", 2);
    public static readonly TechLevelDefinition TL14 = new("TL14", 14, "E", "Average Stellar", "Portable fusion weapons appear and flying cities are available", 2);
    public static readonly TechLevelDefinition TL15 = new("TL15", 15, "F", "High Stellar", "Black globe generators suggest new defensive capabilities; anagathics allow human lifespans to lengthen", 2);
    public static readonly TechLevelDefinition TL16 = new("TL16", 16, "G", "High Stellar", "Compact singularity power sources and zero-point energy begin replacing fusion; nanofabrication enables instant construction at the molecular level; ships can self-repair and adapt mid-flight", 2);
    public static readonly TechLevelDefinition TL17 = new("TL17", 17, "H", "Extreme Stellar", "Cognitive engineering merges organic and synthetic thought; civilizations harness micro-wormholes for communication; biological immortality becomes achievable through cellular quantum stabilization", 2);
    public static readonly TechLevelDefinition TL18 = new("TL18", 18, "J", "Extreme Stellar", "Technology approaches that of the fabled Ancients; matter conversion and directed spacetime manipulation become routine; true sentient starships and self-aware worlds emerge", 2);
    public static readonly TechLevelDefinition TL19 = new("TL19", 19, "K", "Extreme Stellar", "Post-singularity societies operate beyond many physical constraints; time dilation and partial temporal navigation are weaponized; civilizations begin sculpting entire star systems to purpose", 2);
    public static readonly TechLevelDefinition TL20 = new("TL20", 20, "L", "Extreme Stellar", "Technology equals or surpasses that of the Ancients; species engineer pocket universes and rewrite fundamental constants; few retain recognizable form, existing as energy or thought across dimensions", 2);

    public static IReadOnlyList<TechLevelDefinition> All { get; } =
    [
        TL0,
        TL1,
        TL2,
        TL3,
        TL4,
        TL5,
        TL6,
        TL7,
        TL8,
        TL9,
        TL10,
        TL11,
        TL12,
        TL13,
        TL14,
        TL15,
        TL16,
        TL17,
        TL18,
        TL19,
        TL20
    ];

    public static TechLevelDefinition FromValue(int value)
    {
        return value switch
        {
            0 => TL0,
            1 => TL1,
            2 => TL2,
            3 => TL3,
            4 => TL4,
            5 => TL5,
            6 => TL6,
            7 => TL7,
            8 => TL8,
            9 => TL9,
            10 => TL10,
            11 => TL11,
            12 => TL12,
            13 => TL13,
            14 => TL14,
            15 => TL15,
            16 => TL16,
            17 => TL17,
            18 => TL18,
            19 => TL19,
            20 => TL20,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Tech level value must be 0..20.")
        };
    }

    public static TechLevelDefinition FromCode(string code)
    {
        return All.FirstOrDefault(techLevel => string.Equals(techLevel.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown tech level code: '{code}'.", nameof(code));
    }
}

public sealed record FactionCategoryDefinition(
    string Code,
    string Name,
    string Description);

public static class FactionCategoryCatalog
{
    public static readonly FactionCategoryDefinition Political = new("POL", "Political", "Ideology-driven, chasing policy change or government control");
    public static readonly FactionCategoryDefinition Economic = new("ECO", "Economic", "Money and resources are the levers; trade, markets, and corruption");
    public static readonly FactionCategoryDefinition Religious = new("REL", "Religious", "Rooted in belief, ritual, or divine justification");
    public static readonly FactionCategoryDefinition Cultural = new("CUL", "Cultural", "Protects or pushes identity, heritage, language, and tradition");
    public static readonly FactionCategoryDefinition Military = new("MIL", "Military", "Uses armed force, intimidation, or mercenary power");
    public static readonly FactionCategoryDefinition Criminal = new("CRM", "Criminal", "Gangs, cartels, smugglers; thrives in illegality");
    public static readonly FactionCategoryDefinition Revolutionary = new("REV", "Revolutionary", "Explicitly out to overthrow the system entirely");
    public static readonly FactionCategoryDefinition Separatist = new("SEP", "Separatist", "Wants independence or autonomy from the main authority");
    public static readonly FactionCategoryDefinition Technocratic = new("TEC", "Technocratic", "Focused on technology, science, and expertise as power");
    public static readonly FactionCategoryDefinition Bureaucratic = new("BUR", "Bureaucratic", "Gains strength by drowning everything in paperwork and procedure");
    public static readonly FactionCategoryDefinition Populist = new("POP", "Populist", "Inflames public sentiment to steer politics through mass support");
    public static readonly FactionCategoryDefinition ForeignBacked = new("FRN", "Foreign-Backed", "Bankrolled or directed by outside powers");

    public static IReadOnlyList<FactionCategoryDefinition> All { get; } =
    [
        Political,
        Economic,
        Religious,
        Cultural,
        Military,
        Criminal,
        Revolutionary,
        Separatist,
        Technocratic,
        Bureaucratic,
        Populist,
        ForeignBacked
    ];

    public static FactionCategoryDefinition FromRoll(int roll)
    {
        return roll is >= 1 and <= 12
            ? All[roll - 1]
            : throw new ArgumentOutOfRangeException(nameof(roll), roll, "Faction category roll must be 1..12.");
    }

    public static FactionCategoryDefinition FromCode(string code)
    {
        return All.FirstOrDefault(category => string.Equals(category.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown faction category code: '{code}'.", nameof(code));
    }

    public static bool IsValidCode(string? code)
    {
        return !string.IsNullOrWhiteSpace(code) &&
               All.Any(category => string.Equals(category.Code, code, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record FactionStrengthDefinition(
    string Name,
    string Code,
    int SortOrder);

public static class FactionStrengthCatalog
{
    public static readonly FactionStrengthDefinition Obscure = new("Obscure", "OBS", 1);
    public static readonly FactionStrengthDefinition Fringe = new("Fringe", "FRI", 2);
    public static readonly FactionStrengthDefinition Minor = new("Minor", "MIN", 3);
    public static readonly FactionStrengthDefinition Notable = new("Notable", "NOT", 4);
    public static readonly FactionStrengthDefinition Significant = new("Significant", "SIG", 5);
    public static readonly FactionStrengthDefinition Overwhelming = new("Overwhelming", "OVR", 6);

    public static IReadOnlyList<FactionStrengthDefinition> All { get; } =
    [
        Obscure,
        Fringe,
        Minor,
        Notable,
        Significant,
        Overwhelming
    ];

    public static FactionStrengthDefinition FromRoll(int roll)
    {
        return roll is >= 1 and <= 6
            ? All[roll - 1]
            : throw new ArgumentOutOfRangeException(nameof(roll), roll, "Faction strength roll must be 1..6.");
    }

    public static FactionStrengthDefinition FromCode(string code)
    {
        return All.FirstOrDefault(strength => string.Equals(strength.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown faction strength code: '{code}'.", nameof(code));
    }

    public static bool IsValidCode(string? code)
    {
        return !string.IsNullOrWhiteSpace(code) &&
               All.Any(strength => string.Equals(strength.Code, code, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record CulturalTagDefinition(
    string Name,
    int Value,
    string Description);

public static class CulturalTagCatalog
{
    public static readonly CulturalTagDefinition Sexist = new("Sexist", 11, "One sex is considered subservient or inferior to the other");
    public static readonly CulturalTagDefinition Religious = new("Religious", 12, "Heavily influenced by a religious or belief system");
    public static readonly CulturalTagDefinition Artistic = new("Artistic", 13, "Art and culture are highly prized");
    public static readonly CulturalTagDefinition Ritualized = new("Ritualized", 14, "Social interaction and trade is highly formalized");
    public static readonly CulturalTagDefinition Conservative = new("Conservative", 15, "Resists change and outside influences");
    public static readonly CulturalTagDefinition Xenophobic = new("Xenophobic", 16, "Distrusts outsiders and alien influences");
    public static readonly CulturalTagDefinition Taboo = new("Taboo", 21, "A particular topic or idea is forbidden");
    public static readonly CulturalTagDefinition Deceptive = new("Deceptive", 22, "Trickery and equivocation are considered acceptable");
    public static readonly CulturalTagDefinition Liberal = new("Liberal", 23, "Welcomes change and offworld influences");
    public static readonly CulturalTagDefinition Honorable = new("Honorable", 24, "One's word is one's bond");
    public static readonly CulturalTagDefinition Influenced = new("Influenced", 25, "Influenced by a neighboring world");
    public static readonly CulturalTagDefinition Barbaric = new("Barbaric", 31, "Physical strength and combat prowess are highly valued");
    public static readonly CulturalTagDefinition Remnant = new("Remnant", 32, "A surviving remnant of a once-great and vibrant civilization");
    public static readonly CulturalTagDefinition Degenerate = new("Degenerate", 33, "Falling apart and on the brink of war or economic collapse");
    public static readonly CulturalTagDefinition Progressive = new("Progressive", 34, "Expanding and vibrant");
    public static readonly CulturalTagDefinition Recovering = new("Recovering", 35, "Recovering from a recent trauma such as war, disaster or despotic regime");
    public static readonly CulturalTagDefinition Nexus = new("Nexus", 36, "Members of many different cultures and species visit here");
    public static readonly CulturalTagDefinition TouristAttraction = new("Tourist Attraction", 41, "Some aspect of the world draws visitors from all over");
    public static readonly CulturalTagDefinition Violent = new("Violent", 42, "Physical conflict is common, taking the form of duels, brawls and trials by combat");
    public static readonly CulturalTagDefinition Peaceful = new("Peaceful", 43, "Physical conflict is almost unheard of and diplomacy reigns supreme");
    public static readonly CulturalTagDefinition Obsessed = new("Obsessed", 44, "Everyone is obsessed or addicted to a particular substance, personality or item");
    public static readonly CulturalTagDefinition Fashion = new("Fashion", 45, "Fine clothing and decorations are considered vitally important");
    public static readonly CulturalTagDefinition AtWar = new("At War", 46, "Society is engaged in some form of warfare");
    public static readonly CulturalTagDefinition UnusualCustomOffworlders = new("Unusual Custom: Offworlders", 51, "Travellers hold a unique position in the society");
    public static readonly CulturalTagDefinition UnusualCustomStarport = new("Unusual Custom: Starport", 52, "The planet's starport has more significance than just a commerce center");
    public static readonly CulturalTagDefinition UnusualCustomMedia = new("Unusual Custom: Media", 53, "News and telecommunication agencies are strange here");
    public static readonly CulturalTagDefinition UnusualCustomTechnology = new("Unusual Custom: Technology", 54, "Technology is handled in an unusual way");
    public static readonly CulturalTagDefinition UnusualCustomLifecycle = new("Unusual Custom: Lifecycle", 55, "The lifestyle or lifecycle is different than the normal");
    public static readonly CulturalTagDefinition UnusualCustomSocialStandings = new("Unusual Custom: Social Standings", 56, "A distinct caste system exists");
    public static readonly CulturalTagDefinition UnusualCustomTrade = new("Unusual Custom: Trade", 61, "There is an odd culture around trade within the society");
    public static readonly CulturalTagDefinition UnusualCustomNobility = new("Unusual Custom: Nobility", 62, "Individuals of high social standings have strange customs");
    public static readonly CulturalTagDefinition UnusualCustomSex = new("Unusual Custom: Sex", 63, "There is an unusual attitude towards sex");
    public static readonly CulturalTagDefinition UnusualCustomEating = new("Unusual Custom: Eating", 64, "Food and drink occupy a special place in the culture");
    public static readonly CulturalTagDefinition UnusualCustomTravel = new("Unusual Custom: Travel", 65, "Travel is viewed differently in the society");
    public static readonly CulturalTagDefinition UnusualCustomConspiracy = new("Unusual Custom: Conspiracy", 66, "Something strange and conspiratorial is going on");

    public static IReadOnlyList<CulturalTagDefinition> All { get; } =
    [
        Sexist,
        Religious,
        Artistic,
        Ritualized,
        Conservative,
        Xenophobic,
        Taboo,
        Deceptive,
        Liberal,
        Honorable,
        Influenced,
        Barbaric,
        Remnant,
        Degenerate,
        Progressive,
        Recovering,
        Nexus,
        TouristAttraction,
        Violent,
        Peaceful,
        Obsessed,
        Fashion,
        AtWar,
        UnusualCustomOffworlders,
        UnusualCustomStarport,
        UnusualCustomMedia,
        UnusualCustomTechnology,
        UnusualCustomLifecycle,
        UnusualCustomSocialStandings,
        UnusualCustomTrade,
        UnusualCustomNobility,
        UnusualCustomSex,
        UnusualCustomEating,
        UnusualCustomTravel,
        UnusualCustomConspiracy
    ];

    private static readonly IReadOnlyDictionary<int, CulturalTagDefinition> ByValue =
        All.ToDictionary(tag => tag.Value);

    public static CulturalTagDefinition FromValue(int value)
    {
        return ByValue.TryGetValue(value, out var tag)
            ? tag
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Cultural tag value is not recognized.");
    }

    public static bool IsValidValue(int value)
    {
        return ByValue.ContainsKey(value);
    }
}

public sealed class CharacterCreationMetadata
{
    public DateTime CreateStartDateTime { get; set; } = DateTime.Now;
    public List<DateTime> CreatePauseDateTimes { get; set; } = [];
    public List<DateTime> CreateContinueDateTimes { get; set; } = [];
    public DateTime? CreateFinalizedDateTime { get; set; }
}

public enum RaceType
{
    Human,
    Aslan,
    Vargr
}

public enum HeritageType
{
    Anglo,
    Arabic,
    Bantu,
    Celtic,
    Chinese,
    French,
    Germanic,
    Greek,
    Hispanic,
    Indian,
    Italian,
    Japanese,
    Korean,
    NativeAmerican,
    Nordic,
    Persian,
    Polynesian,
    Slavic,
    SoutheastAsian,
    WestAfrican
}

public enum GenderType
{
    Male,
    Female
}

public enum EyeColorType
{
    Amber,
    Black,
    Blue,
    Brown,
    Cyan,
    Emerald,
    Gold,
    Gray,
    Green,
    Hazel,
    Heterochromatic,
    NeonBlue,
    Red,
    Silver,
    Violet
}

public enum SkinColorType
{
    Black,
    BlueTint,
    Bronze,
    Brown,
    Chrome,
    Copper,
    DarkBrown,
    Ebony,
    EmeraldTint,
    Fair,
    Golden,
    Lavender,
    LightBrown,
    MediumBrown,
    Olive,
    Pale,
    Porcelain,
    Silver,
    Tan,
    Umber,
    Albino
}

public enum HairColorType
{
    Auburn,
    Black,
    Blonde,
    Brown,
    Chestnut,
    Copper,
    DarkBrown,
    Gray,
    Red,
    Silver,
    White
}

public enum FurColorType
{
    Black,
    Brown,
    Cream,
    DarkBrown,
    DarkGray,
    Golden,
    Gray,
    LightBrown,
    RedBrown,
    Sandy,
    Silver,
    Tan,
    Tawny,
    White
}

public enum FurPatternType
{
    Agouti,
    Blanket,
    Brindle,
    ManeDark,
    ManeLight,
    Masked,
    Merle,
    Rosetted,
    Sable,
    Solid,
    Spotted,
    Striped,
    Tufted,
    WolfGray,
    Albino
}

public enum VargrRoleType
{
    Alpha,
    Beta,
    Archivist,
    Breacher,
    Engineer,
    Envoy,
    Gunner,
    Handler,
    Hunter,
    Mechanic,
    Medic,
    Navigator,
    Pathfinder,
    Pilot,
    Quartermaster,
    Ritualist,
    Scout,
    Sentinel,
    Tactician,
    Trader
}

public static class VargrRoleCatalog
{
    private static readonly Dictionary<VargrRoleType, VargrRoleDefinition> Definitions = new()
    {
        [VargrRoleType.Alpha] = new("urrKhan", "Clan command"),
        [VargrRoleType.Beta] = new("vaRukh", "Second command"),
        [VargrRoleType.Archivist] = new("ekhTal", "Records and lineage"),
        [VargrRoleType.Breacher] = new("gralKesh", "Boarding and forced entry"),
        [VargrRoleType.Engineer] = new("makRrol", "Ship systems"),
        [VargrRoleType.Envoy] = new("vessAra", "External negotiations"),
        [VargrRoleType.Gunner] = new("tokRav", "Weapons fire control"),
        [VargrRoleType.Handler] = new("nakhOr", "Crew discipline and logistics"),
        [VargrRoleType.Hunter] = new("ipSec", "Hunter"),
        [VargrRoleType.Mechanic] = new("tikMarr", "Repairs and fabrication"),
        [VargrRoleType.Medic] = new("lenVok", "Medical care"),
        [VargrRoleType.Navigator] = new("shaVor", "Astrogation"),
        [VargrRoleType.Pathfinder] = new("rekSha", "Route discovery"),
        [VargrRoleType.Pilot] = new("aerRuk", "Small craft and helm"),
        [VargrRoleType.Quartermaster] = new("domKeth", "Supplies and stores"),
        [VargrRoleType.Ritualist] = new("orrVesh", "Clan rites"),
        [VargrRoleType.Scout] = new("sekVar", "Reconnaissance"),
        [VargrRoleType.Sentinel] = new("garRok", "Guard duty"),
        [VargrRoleType.Tactician] = new("zhekTor", "Battle planning"),
        [VargrRoleType.Trader] = new("merKhal", "Trade and barter")
    };

    public static IReadOnlyDictionary<VargrRoleType, VargrRoleDefinition> All => Definitions;

    public static string GetSound(VargrRoleType role)
    {
        return Definitions.TryGetValue(role, out var definition) ? definition.Sound : role.ToString();
    }

    public static VargrRoleType? GetRoleBySound(string sound)
    {
        foreach (var pair in Definitions)
        {
            if (string.Equals(pair.Value.Sound, sound, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Key;
            }
        }

        return null;
    }
}

public sealed record VargrRoleDefinition(string Sound, string Meaning);

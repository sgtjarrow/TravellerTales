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
        ApplyLegacyNameIfNeeded();
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

    [JsonIgnore]
    public string Uwp => BuildUwp(WorldSizeValue, AtmosphereValue, HydrographicsValue);

    public static string BuildUwp(int worldSizeValue, int atmosphereValue, int hydrographicsValue)
    {
        var worldSize = WorldSizeCatalog.FromValue(Math.Clamp(worldSizeValue, 0, 10));
        var atmosphere = AtmosphereCatalog.FromValue(Math.Clamp(atmosphereValue, 0, 15));
        var hydrographics = HydrographicsCatalog.FromValue(Math.Clamp(hydrographicsValue, 0, 10));

        return $"{worldSize.Code}{atmosphere.Code}{hydrographics.Code}";
    }
}

public sealed record WorldSizeDefinition(
    string Name,
    int Value,
    string Code,
    string Diameter,
    string SurfaceGravity);

public static class WorldSizeCatalog
{
    public static readonly WorldSizeDefinition AsteroidBelt = new("Asteroid Belt", 0, "0", "Less Than 1,000 km", "None");
    public static readonly WorldSizeDefinition Minuscule = new("Minuscule", 1, "1", "1,600 km", "0.05 g");
    public static readonly WorldSizeDefinition Tiny = new("Tiny", 2, "2", "3,200 km", "0.15 g");
    public static readonly WorldSizeDefinition Small = new("Small", 3, "3", "4,800 km", "0.25 g");
    public static readonly WorldSizeDefinition Modest = new("Modest", 4, "4", "6,400 km", "0.35 g");
    public static readonly WorldSizeDefinition Medium = new("Medium", 5, "5", "8,000 km", "0.45 g");
    public static readonly WorldSizeDefinition Large = new("Large", 6, "6", "9,600 km", "0.70 g");
    public static readonly WorldSizeDefinition Huge = new("Huge", 7, "7", "11,200 km", "0.90 g");
    public static readonly WorldSizeDefinition Vast = new("Vast", 8, "8", "12,800 km", "1.00 g");
    public static readonly WorldSizeDefinition Enormous = new("Enormous", 9, "9", "14,400 km", "1.25 g");
    public static readonly WorldSizeDefinition Colossal = new("Colossal", 10, "A", "16,000 km", "1.40 g");

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
    int HydrographicsModifier);

public static class AtmosphereCatalog
{
    public static readonly AtmosphereDefinition None = new("None", 0, "0", "0.00 bars", "Vacc Suit", 0, -4);
    public static readonly AtmosphereDefinition Trace = new("Trace", 1, "1", "0.05 bars", "Vacc Suit", 0, -4);
    public static readonly AtmosphereDefinition VeryThinTainted = new("Very Thin, Tainted", 2, "2", "0.25 bars", "Filter, Respirator", -2, 0);
    public static readonly AtmosphereDefinition VeryThin = new("Very Thin", 3, "3", "0.25 bars", "Respirator", -2, 0);
    public static readonly AtmosphereDefinition ThinTainted = new("Thin, Tainted", 4, "4", "0.60 bars", "Filter", -1, 0);
    public static readonly AtmosphereDefinition Thin = new("Thin", 5, "5", "0.60 bars", "None", -1, 0);
    public static readonly AtmosphereDefinition Standard = new("Standard", 6, "6", "1.00 bars", "None", 0, 0);
    public static readonly AtmosphereDefinition StandardTainted = new("Standard, Tainted", 7, "7", "1.00 bars", "Filter", 0, 0);
    public static readonly AtmosphereDefinition Dense = new("Dense", 8, "8", "2.00 bars", "None", 1, 0);
    public static readonly AtmosphereDefinition DenseTainted = new("Dense, Tainted", 9, "9", "2.00 bars", "Filter", 1, 0);
    public static readonly AtmosphereDefinition Exotic = new("Exotic", 10, "A", "Varies", "Air Supply", 2, -4);
    public static readonly AtmosphereDefinition Corrosive = new("Corrosive", 11, "B", "Varies", "Vacc Suit", 6, -4);
    public static readonly AtmosphereDefinition Insidious = new("Insidious", 12, "C", "Varies", "Vacc Suit", 6, -4);
    public static readonly AtmosphereDefinition VeryDense = new("Very Dense", 13, "D", "Greater Than 2.50 bars", "None", 2, -4);
    public static readonly AtmosphereDefinition Low = new("Low", 14, "E", "Less Than 0.50 bars", "None", -1, -4);
    public static readonly AtmosphereDefinition Unusual = new("Unusual", 15, "F", "Varies", "Varies", 2, -4);

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

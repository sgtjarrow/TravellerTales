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

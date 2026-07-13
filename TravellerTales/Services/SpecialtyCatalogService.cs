using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TravellerTales.Models;

namespace TravellerTales.Services;

public sealed class SkillSpecialtyCatalog
{
    public List<SkillSpecialtyDefinition> Specialties { get; set; } = [];

    public SkillSpecialtyDefinition GetOrAddCustom(SkillName skillName, string displayName)
    {
        var normalizedName = SkillSpecialtyCatalogRules.NormalizeDisplayName(displayName);
        var existing = FindByDisplayName(skillName, normalizedName);
        if (existing is not null)
        {
            return existing;
        }

        var specialty = new SkillSpecialtyDefinition
        {
            Id = SkillSpecialtyCatalogRules.CreateCustomId(skillName, normalizedName),
            SkillName = skillName,
            DisplayName = normalizedName,
            IsSeeded = false
        };

        Specialties.Add(specialty);
        return specialty;
    }

    public SkillSpecialtyDefinition? FindById(string specialtyId)
    {
        return Specialties.FirstOrDefault(specialty =>
            string.Equals(specialty.Id, specialtyId, StringComparison.OrdinalIgnoreCase));
    }

    public SkillSpecialtyDefinition? FindByDisplayName(SkillName skillName, string displayName)
    {
        var normalizedName = SkillSpecialtyCatalogRules.NormalizeDisplayName(displayName);
        return Specialties.FirstOrDefault(specialty =>
            specialty.SkillName == skillName &&
            string.Equals(specialty.DisplayName, normalizedName, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<SkillSpecialtyDefinition> GetForSkill(SkillName skillName)
    {
        return Specialties
            .Where(specialty => specialty.SkillName == skillName)
            .OrderByDescending(specialty => specialty.SelectedCount)
            .ThenBy(specialty => specialty.DisplayName)
            .ToList();
    }
}

public sealed class SkillSpecialtyDefinition
{
    public string Id { get; set; } = string.Empty;
    public SkillName SkillName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSeeded { get; set; }
    public int SelectedCount { get; set; }
}

public static class SkillSpecialtyCatalogRules
{
    public static string NormalizeDisplayName(string displayName)
    {
        var trimmed = string.Join(' ', (displayName ?? string.Empty)
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(trimmed.ToLower(CultureInfo.CurrentCulture));
    }

    public static string CreateSeededId(SkillName skillName, string displayName)
    {
        return $"{skillName}:{CreateSlug(displayName)}";
    }

    public static string CreateCustomId(SkillName skillName, string displayName)
    {
        return $"{skillName}:custom:{CreateSlug(displayName)}";
    }

    private static string CreateSlug(string displayName)
    {
        var normalizedName = NormalizeDisplayName(displayName);
        var characters = normalizedName
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        return string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}

public static class SpecialtyCatalogService
{
    private const string SpecialtyCatalogFileName = "SkillSpecialties.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static string SpecialtyCatalogPath => Path.Combine(AppPaths.ApplicationDataDirectory, SpecialtyCatalogFileName);

    public static SkillSpecialtyCatalog LoadCatalog()
    {
        var catalog = File.Exists(SpecialtyCatalogPath)
            ? JsonSerializer.Deserialize<SkillSpecialtyCatalog>(File.ReadAllText(SpecialtyCatalogPath), JsonOptions) ?? new()
            : new();

        MergeSeededSpecialties(catalog);
        return catalog;
    }

    public static void SaveCatalog(SkillSpecialtyCatalog catalog)
    {
        Directory.CreateDirectory(AppPaths.ApplicationDataDirectory);
        var json = JsonSerializer.Serialize(catalog, JsonOptions);
        File.WriteAllText(SpecialtyCatalogPath, json);
    }

    public static void IncrementSelectedCount(SkillSpecialtyCatalog catalog, string specialtyId)
    {
        var specialty = catalog.FindById(specialtyId);
        if (specialty is null)
        {
            return;
        }

        specialty.SelectedCount++;
        SaveCatalog(catalog);
    }

    public static void MergeSeededSpecialties(SkillSpecialtyCatalog catalog)
    {
        var seededIds = GetSeededSpecialties()
            .Select(specialty => specialty.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        catalog.Specialties.RemoveAll(specialty => specialty.IsSeeded && !seededIds.Contains(specialty.Id));

        foreach (var seeded in GetSeededSpecialties())
        {
            var existing = catalog.FindById(seeded.Id);
            if (existing is not null)
            {
                existing.SkillName = seeded.SkillName;
                existing.DisplayName = seeded.DisplayName;
                existing.IsSeeded = true;
                continue;
            }

            catalog.Specialties.Add(seeded);
        }
    }

    private static IEnumerable<SkillSpecialtyDefinition> GetSeededSpecialties()
    {
        return new (SkillName SkillName, string DisplayName)[]
        {
            (SkillName.Animals, "Handling"),
            (SkillName.Animals, "Training"),
            (SkillName.Animals, "Veterinary"),
            (SkillName.Art, "Performer"),
            (SkillName.Art, "Holography"),
            (SkillName.Art, "Instrument"),
            (SkillName.Art, "Visual Media"),
            (SkillName.Art, "Writing"),
            (SkillName.Athletics, "Dexterity"),
            (SkillName.Athletics, "Endurance"),
            (SkillName.Athletics, "Strength"),
            (SkillName.Drive, "Hovercraft"),
            (SkillName.Drive, "Mole"),
            (SkillName.Drive, "Track"),
            (SkillName.Drive, "Walker"),
            (SkillName.Drive, "Wheeled"),
            (SkillName.Electronics, "Comms"),
            (SkillName.Electronics, "Computers"),
            (SkillName.Electronics, "Remote Ops"),
            (SkillName.Electronics, "Sensors"),
            (SkillName.Engineer, "M-Drive"),
            (SkillName.Engineer, "J-Drive"),
            (SkillName.Engineer, "Life Support"),
            (SkillName.Engineer, "Power"),
            (SkillName.Flyer, "Airship"),
            (SkillName.Flyer, "Grav"),
            (SkillName.Flyer, "Ornithopter"),
            (SkillName.Flyer, "Rotor"),
            (SkillName.Flyer, "Wing"),
            (SkillName.Gunner, "Capital"),
            (SkillName.Gunner, "Ortillery"),
            (SkillName.Gunner, "Screen"),
            (SkillName.Gunner, "Turret"),
            (SkillName.GunCombat, "Archaic"),
            (SkillName.GunCombat, "Energy"),
            (SkillName.GunCombat, "Slug"),
            (SkillName.HeavyWeapons, "Artillery"),
            (SkillName.HeavyWeapons, "Portable"),
            (SkillName.HeavyWeapons, "Vehicle"),
            (SkillName.Language, "Galanglic"),
            (SkillName.Language, "Gvegh"),
            (SkillName.Language, "Trokh"),
            (SkillName.Language, "Vilani"),
            (SkillName.Language, "Zdetl"),
            (SkillName.Melee, "Blade"),
            (SkillName.Melee, "Bludgeon"),
            (SkillName.Melee, "Natural"),
            (SkillName.Melee, "Unarmed"),
            (SkillName.Pilot, "Capital Ships"),
            (SkillName.Pilot, "Small Craft"),
            (SkillName.Pilot, "Spacecraft"),
            (SkillName.Profession, "Belter"),
            (SkillName.Profession, "Biologicals"),
            (SkillName.Profession, "Civil Engineer"),
            (SkillName.Profession, "Construction"),
            (SkillName.Profession, "Hydroponics"),
            (SkillName.Profession, "Polymers"),
            (SkillName.Science, "Archaeology"),
            (SkillName.Science, "Astronomy"),
            (SkillName.Science, "Biology"),
            (SkillName.Science, "Chemistry"),
            (SkillName.Science, "Cosmology"),
            (SkillName.Science, "Cybernetics"),
            (SkillName.Science, "Economics"),
            (SkillName.Science, "Genetics"),
            (SkillName.Science, "History"),
            (SkillName.Science, "Linguistics"),
            (SkillName.Science, "Philosophy"),
            (SkillName.Science, "Physics"),
            (SkillName.Science, "Planetology"),
            (SkillName.Science, "Psionicology"),
            (SkillName.Science, "Psychology"),
            (SkillName.Science, "Robotic"),
            (SkillName.Science, "Sophontology"),
            (SkillName.Science, "Xenology"),
            (SkillName.Seafarer, "Ocean Ships"),
            (SkillName.Seafarer, "Personal"),
            (SkillName.Seafarer, "Sail"),
            (SkillName.Seafarer, "Submarine"),
            (SkillName.Tactics, "Military"),
            (SkillName.Tactics, "Naval")
        }
        .Select(specialty => new SkillSpecialtyDefinition
        {
            Id = SkillSpecialtyCatalogRules.CreateSeededId(specialty.SkillName, specialty.DisplayName),
            SkillName = specialty.SkillName,
            DisplayName = SkillSpecialtyCatalogRules.NormalizeDisplayName(specialty.DisplayName),
            IsSeeded = true
        });
    }
}

using TravellerTales.Models;

namespace TravellerTales.Services;

public sealed record SkillDefinition(SkillName SkillName, string DisplayName, SkillClassType ClassType);

public static class SkillCatalog
{
    public static readonly IReadOnlyDictionary<SkillName, SkillDefinition> Definitions =
        new Dictionary<SkillName, SkillDefinition>
        {
            [SkillName.Admin] = new(SkillName.Admin, "Admin", SkillClassType.ValueOnly),
            [SkillName.Advocate] = new(SkillName.Advocate, "Advocate", SkillClassType.ValueOnly),
            [SkillName.Animals] = new(SkillName.Animals, "Animals", SkillClassType.SpecialtiesOnly),
            [SkillName.Art] = new(SkillName.Art, "Art", SkillClassType.SpecialtiesOnly),
            [SkillName.Astrogation] = new(SkillName.Astrogation, "Astrogation", SkillClassType.ValueOnly),
            [SkillName.Athletics] = new(SkillName.Athletics, "Athletics", SkillClassType.SpecialtiesOnly),
            [SkillName.Broker] = new(SkillName.Broker, "Broker", SkillClassType.ValueOnly),
            [SkillName.Carouse] = new(SkillName.Carouse, "Carouse", SkillClassType.ValueOnly),
            [SkillName.Deception] = new(SkillName.Deception, "Deception", SkillClassType.ValueOnly),
            [SkillName.Diplomat] = new(SkillName.Diplomat, "Diplomat", SkillClassType.ValueOnly),
            [SkillName.Drive] = new(SkillName.Drive, "Drive", SkillClassType.SpecialtiesOnly),
            [SkillName.Electronics] = new(SkillName.Electronics, "Electronics", SkillClassType.SpecialtiesOnly),
            [SkillName.Engineer] = new(SkillName.Engineer, "Engineer", SkillClassType.SpecialtiesOnly),
            [SkillName.Explosives] = new(SkillName.Explosives, "Explosives", SkillClassType.ValueOnly),
            [SkillName.Flyer] = new(SkillName.Flyer, "Flyer", SkillClassType.SpecialtiesOnly),
            [SkillName.Gambler] = new(SkillName.Gambler, "Gambler", SkillClassType.ValueOnly),
            [SkillName.Gunner] = new(SkillName.Gunner, "Gunner", SkillClassType.SpecialtiesOnly),
            [SkillName.GunCombat] = new(SkillName.GunCombat, "Gun Combat", SkillClassType.SpecialtiesOnly),
            [SkillName.HeavyWeapons] = new(SkillName.HeavyWeapons, "Heavy Weapons", SkillClassType.SpecialtiesOnly),
            [SkillName.Investigate] = new(SkillName.Investigate, "Investigate", SkillClassType.ValueOnly),
            [SkillName.JackOfAllTrades] = new(SkillName.JackOfAllTrades, "Jack Of All Trades", SkillClassType.ValueOnly),
            [SkillName.Language] = new(SkillName.Language, "Language", SkillClassType.SpecialtiesOnly),
            [SkillName.Leadership] = new(SkillName.Leadership, "Leadership", SkillClassType.ValueOnly),
            [SkillName.Mechanic] = new(SkillName.Mechanic, "Mechanic", SkillClassType.ValueOnly),
            [SkillName.Medic] = new(SkillName.Medic, "Medic", SkillClassType.ValueOnly),
            [SkillName.Melee] = new(SkillName.Melee, "Melee", SkillClassType.SpecialtiesOnly),
            [SkillName.Navigation] = new(SkillName.Navigation, "Navigation", SkillClassType.ValueOnly),
            [SkillName.Persuade] = new(SkillName.Persuade, "Persuade", SkillClassType.ValueOnly),
            [SkillName.Pilot] = new(SkillName.Pilot, "Pilot", SkillClassType.SpecialtiesOnly),
            [SkillName.Profession] = new(SkillName.Profession, "Profession", SkillClassType.SpecialtiesOnly),
            [SkillName.Recon] = new(SkillName.Recon, "Recon", SkillClassType.ValueOnly),
            [SkillName.Science] = new(SkillName.Science, "Science", SkillClassType.SpecialtiesOnly),
            [SkillName.Seafarer] = new(SkillName.Seafarer, "Seafarer", SkillClassType.SpecialtiesOnly),
            [SkillName.Stealth] = new(SkillName.Stealth, "Stealth", SkillClassType.ValueOnly),
            [SkillName.Steward] = new(SkillName.Steward, "Steward", SkillClassType.ValueOnly),
            [SkillName.Streetwise] = new(SkillName.Streetwise, "Streetwise", SkillClassType.ValueOnly),
            [SkillName.Survival] = new(SkillName.Survival, "Survival", SkillClassType.ValueOnly),
            [SkillName.Tactics] = new(SkillName.Tactics, "Tactics", SkillClassType.SpecialtiesOnly),
            [SkillName.VaccSuit] = new(SkillName.VaccSuit, "Vacc Suit", SkillClassType.ValueOnly)
        };

    public static IEnumerable<SkillDefinition> All => Definitions.Values.OrderBy(definition => definition.DisplayName);

    public static string GetDisplayName(SkillName skillName) => Definitions[skillName].DisplayName;

    public static SkillClassType GetClassType(SkillName skillName) => Definitions[skillName].ClassType;

    public static bool IsValueOnly(SkillName skillName) => GetClassType(skillName) == SkillClassType.ValueOnly;

    public static bool IsSpecialtiesOnly(SkillName skillName) => GetClassType(skillName) == SkillClassType.SpecialtiesOnly;
}

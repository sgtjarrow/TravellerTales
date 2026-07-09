using TravellerTales.Models;

namespace TravellerTales.Services;

public static class HomeworldGenerator
{
    public static int GenerateWorldSizeValue()
    {
        return Math.Clamp(DiceRoller.Roll(2, 6, -2), 0, 10);
    }

    public static int GenerateAtmosphereValue(int worldSizeValue)
    {
        if (worldSizeValue is 0 or 1)
        {
            return 0;
        }

        return Math.Clamp(DiceRoller.Roll(2, 6, -7 + worldSizeValue), 0, 15);
    }

    public static string GenerateTemperatureKey(int atmosphereValue)
    {
        if (atmosphereValue is 0 or 1)
        {
            return TemperatureCatalog.Swinging.Key;
        }

        var atmosphere = AtmosphereCatalog.FromValue(atmosphereValue);
        var roll = DiceRoller.Roll(2, 6, atmosphere.TemperatureModifier);

        return roll switch
        {
            <= 2 => TemperatureCatalog.Frozen.Key,
            <= 4 => TemperatureCatalog.Cold.Key,
            <= 9 => TemperatureCatalog.Temperate.Key,
            <= 11 => TemperatureCatalog.Hot.Key,
            _ => TemperatureCatalog.Boiling.Key
        };
    }

    public static int GenerateHydrographicsValue(int worldSizeValue, int atmosphereValue, string temperatureKey)
    {
        if (worldSizeValue is 0 or 1)
        {
            return 0;
        }

        var atmosphere = AtmosphereCatalog.FromValue(atmosphereValue);
        var modifier = -7 + worldSizeValue + atmosphere.HydrographicsModifier;

        if (atmosphereValue is not 13 and not 15)
        {
            modifier += TemperatureCatalog.FromKey(temperatureKey).HydrographicsModifier;
        }

        return Math.Clamp(DiceRoller.Roll(2, 6, modifier), 0, 10);
    }

    public static int GeneratePopulationValue(bool allowZero = true)
    {
        int populationValue;

        do
        {
            populationValue = Math.Clamp(DiceRoller.Roll(2, 6, -2), 0, 10);
        }
        while (!allowZero && populationValue == 0);

        if (populationValue == 10 && Random.Shared.Next(100) < 5)
        {
            populationValue = 11;

            if (Random.Shared.Next(100) < 3)
            {
                populationValue = 12;
            }
        }

        return populationValue;
    }

    public static string GenerateStarportCode(int populationValue)
    {
        var population = PopulationCatalog.FromValue(populationValue);
        var roll = DiceRoller.Roll(2, 6, population.StarportModifier);
        var code = roll switch
        {
            <= 2 => StarportCatalog.None.Code,
            <= 4 => StarportCatalog.Frontier.Code,
            <= 6 => StarportCatalog.Poor.Code,
            <= 8 => StarportCatalog.Routine.Code,
            <= 10 => StarportCatalog.Good.Code,
            _ => StarportCatalog.Excellent.Code
        };

        if (populationValue == 0 && code is "A" or "B" or "C")
        {
            return StarportCatalog.Beacon.Code;
        }

        return code;
    }

    public static List<int> GenerateCulturalTagValues(int populationValue)
    {
        if (populationValue == 0)
        {
            return [];
        }

        var culturalTags = new List<int>();
        var targetCount = 1;
        var maximumUniqueTags = CulturalTagCatalog.All.Count;

        while (culturalTags.Count < targetCount && culturalTags.Count < maximumUniqueTags)
        {
            var roll = RollCulturalTagValue();

            if (roll == 26)
            {
                targetCount = Math.Min(targetCount + 1, maximumUniqueTags);
                continue;
            }

            if (!CulturalTagCatalog.IsValidValue(roll) || culturalTags.Contains(roll))
            {
                continue;
            }

            culturalTags.Add(roll);
        }

        return culturalTags;
    }

    public static int GenerateGovernmentValue(int populationValue)
    {
        if (populationValue == 0)
        {
            return 0;
        }

        return Math.Clamp(DiceRoller.Roll(2, 7, -7 + populationValue), 0, 15);
    }

    public static int GenerateLawLevelValue(int populationValue, int governmentValue)
    {
        if (populationValue == 0)
        {
            return 0;
        }

        return Math.Clamp(DiceRoller.Roll(2, 6, -7 + governmentValue), 0, 15);
    }

    public static int GenerateTechLevelValue(
        string? starportCode,
        int worldSizeValue,
        int atmosphereValue,
        int hydrographicsValue,
        int populationValue,
        int governmentValue)
    {
        if (populationValue == 0)
        {
            return 0;
        }

        var starport = StarportCatalog.IsValidCode(starportCode)
            ? StarportCatalog.FromCode(starportCode!)
            : StarportCatalog.None;
        var worldSize = WorldSizeCatalog.FromValue(worldSizeValue);
        var atmosphere = AtmosphereCatalog.FromValue(atmosphereValue);
        var hydrographics = HydrographicsCatalog.FromValue(hydrographicsValue);
        var population = PopulationCatalog.FromValue(populationValue);
        var government = GovernmentCatalog.FromValue(governmentValue);
        var modifier = starport.TechLevelModifier +
                       worldSize.TechLevelModifier +
                       atmosphere.TechLevelModifier +
                       hydrographics.TechLevelModifier +
                       population.TechLevelModifier +
                       government.TechLevelModifier;
        var techLevelValue = Math.Clamp(DiceRoller.Roll(6) + modifier, 0, 20);

        return Math.Clamp(Math.Max(techLevelValue, atmosphere.MinimumTechLevel), 0, 20);
    }

    public static List<HomeworldFaction> GenerateFactions(int populationValue, int governmentValue)
    {
        if (populationValue < 1 || governmentValue < 1)
        {
            return [];
        }

        var government = GovernmentCatalog.FromValue(governmentValue);
        var factionCount = Math.Clamp(DiceRoller.Roll(3) + government.FactionCountModifier, 0, 4);
        var factions = new List<HomeworldFaction>();

        for (var index = 0; index < factionCount; index++)
        {
            factions.Add(new HomeworldFaction
            {
                Name = $"Faction {index + 1}",
                CategoryCode = FactionCategoryCatalog.FromRoll(DiceRoller.Roll(12)).Code,
                StrengthCode = FactionStrengthCatalog.FromRoll(DiceRoller.Roll(6)).Code
            });
        }

        return factions;
    }

    private static int RollCulturalTagValue()
    {
        return (DiceRoller.Roll(6) * 10) + DiceRoller.Roll(6);
    }
}

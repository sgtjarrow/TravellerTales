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
}

using TravellerTales.Models;

namespace TravellerTales.Services;

public static class HomeworldCandidateSummaryGenerator
{
    public static string Generate(HomeworldCandidate candidate)
    {
        var worldSize = WorldSizeCatalog.FromValue(Clamp(candidate.WorldSizeValue, 0, 10));
        var atmosphere = AtmosphereCatalog.FromValue(Clamp(candidate.AtmosphereValue, 0, 15));
        var temperature = TemperatureCatalog.IsValidKey(candidate.TemperatureKey)
            ? TemperatureCatalog.FromKey(candidate.TemperatureKey)
            : TemperatureCatalog.Swinging;
        var hydrographics = HydrographicsCatalog.FromValue(Clamp(candidate.HydrographicsValue, 0, 10));
        var population = PopulationCatalog.FromValue(Clamp(candidate.PopulationValue, 0, 12));
        var starport = StarportCatalog.IsValidCode(candidate.StarportCode)
            ? StarportCatalog.FromCode(candidate.StarportCode!)
            : StarportCatalog.None;
        var techLevel = TechLevelCatalog.FromValue(Clamp(candidate.TechLevelValue, 0, 20));
        var travelCode = TravelCodeCatalog.IsValidCode(candidate.TravelCode)
            ? TravelCodeCatalog.FromCode(candidate.TravelCode!)
            : TravelCodeCatalog.Green;

        var sentences = new List<string>
        {
            BuildPhysicalIdentity(worldSize, atmosphere, hydrographics, temperature),
            BuildHabitability(atmosphere, hydrographics, temperature)
        };

        var notableDetail = BuildNotableDetail(candidate, starport, population, techLevel, travelCode);

        if (!string.IsNullOrWhiteSpace(notableDetail))
        {
            sentences.Add(notableDetail);
        }

        return string.Join(" ", sentences);
    }

    private static string BuildPhysicalIdentity(
        WorldSizeDefinition worldSize,
        AtmosphereDefinition atmosphere,
        HydrographicsDefinition hydrographics,
        TemperatureDefinition temperature)
    {
        if (worldSize.Value == 0)
        {
            return $"An asteroid-belt homeworld, this candidate is shaped by scattered rock habitats and {hydrographics.Name.ToLowerInvariant()} water reserves.";
        }

        if (hydrographics.Value == 10)
        {
            return $"A {worldSize.Name.ToLowerInvariant()} water world, this candidate is dominated by global seas under a {atmosphere.Name.ToLowerInvariant()} atmosphere.";
        }

        if (atmosphere.Value >= 10)
        {
            return $"A {worldSize.Name.ToLowerInvariant()} world with a hostile {atmosphere.Name.ToLowerInvariant()} atmosphere, this candidate has {hydrographics.Name.ToLowerInvariant()} surface water and a {temperature.Name.ToLowerInvariant()} climate.";
        }

        if (hydrographics.Value == 0)
        {
            return $"A {worldSize.Name.ToLowerInvariant()} dry world, this candidate has a {temperature.Name.ToLowerInvariant()} climate and almost no open surface water.";
        }

        return $"A {worldSize.Name.ToLowerInvariant()} world with {hydrographics.Name.ToLowerInvariant()} surface water, this candidate sits under a {atmosphere.Name.ToLowerInvariant()} atmosphere and a {temperature.Name.ToLowerInvariant()} climate.";
    }

    private static string BuildHabitability(
        AtmosphereDefinition atmosphere,
        HydrographicsDefinition hydrographics,
        TemperatureDefinition temperature)
    {
        var atmospherePhrase = atmosphere.SurvivalGear == "None"
            ? "Its atmosphere is broadly breathable without special gear"
            : $"Its atmosphere requires {atmosphere.SurvivalGear.ToLowerInvariant()}";

        return $"{atmospherePhrase}, while {hydrographics.Percentage} water coverage and {temperature.Range.ToLowerInvariant()} temperatures define most settlement patterns.";
    }

    private static string BuildNotableDetail(
        HomeworldCandidate candidate,
        StarportDefinition starport,
        PopulationDefinition population,
        TechLevelDefinition techLevel,
        TravelCodeDefinition travelCode)
    {
        if (travelCode.Code == TravelCodeCatalog.Red.Code)
        {
            return "A red travel advisory marks the world as dangerous enough to shape every arrival and departure.";
        }

        if (travelCode.Code == TravelCodeCatalog.Amber.Code)
        {
            return "An amber travel advisory signals local hazards or controls that visitors cannot ignore.";
        }

        var baseDetail = BuildBaseDetail(candidate.Bases);

        if (!string.IsNullOrWhiteSpace(baseDetail))
        {
            return baseDetail;
        }

        if (starport.Code is "A" or "B")
        {
            return $"Its {starport.Name.ToLowerInvariant()} starport gives the world unusually strong access to interstellar traffic.";
        }

        if (starport.Code is "X" or "Z")
        {
            return "With no true starport infrastructure, contact depends on limited beacons, local craft, or outside support.";
        }

        if (starport.Code == "E")
        {
            return "Frontier-grade port facilities make landings possible but leave visitors with few services.";
        }

        if (techLevel.Value >= 12)
        {
            return $"Technology is notably advanced at {techLevel.Name.ToLowerInvariant()} levels, giving settlements better tools for local extremes.";
        }

        if (techLevel.Value is > 0 and <= 5)
        {
            return $"Technology remains limited at {techLevel.Name.ToLowerInvariant()} levels, making the environment harder to manage.";
        }

        if (population.Value >= 9)
        {
            return $"A {population.Name.ToLowerInvariant()} population turns the physical environment into a heavily managed worldscape.";
        }

        if (population.Value <= 3)
        {
            return $"Only a {population.Name.ToLowerInvariant()} population is present, leaving much of the world thinly settled.";
        }

        return string.Empty;
    }

    private static string BuildBaseDetail(HomeworldBases? bases)
    {
        if (bases is null)
        {
            return string.Empty;
        }

        if (bases.CorsairBase)
        {
            return "A corsair presence adds a dangerous edge to traffic near the mainworld.";
        }

        if (bases.NavalDepot)
        {
            return "A naval depot makes the system an important logistics point beyond its surface conditions.";
        }

        if (bases.NavalBase)
        {
            return "A naval base gives the system a clear strategic role.";
        }

        if (bases.ScoutWayStation)
        {
            return "A scout way station makes the world a useful stop for survey and courier traffic.";
        }

        if (bases.ScoutBase)
        {
            return "A scout base keeps the world connected to survey routes and frontier traffic.";
        }

        if (bases.MilitaryBase)
        {
            return "A military base adds a firm security presence to the local system.";
        }

        if (bases.HighPort)
        {
            return "A high port shifts much of the world's traffic and commerce into orbit.";
        }

        return string.Empty;
    }

    private static int Clamp(int? value, int minimum, int maximum)
    {
        return Math.Clamp(value.GetValueOrDefault(), minimum, maximum);
    }
}

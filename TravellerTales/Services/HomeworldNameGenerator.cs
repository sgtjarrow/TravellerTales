namespace TravellerTales.Services;

public static class HomeworldNameGenerator
{
    private static readonly string[] ProperNames =
    [
        "Aurelion", "Caldera Blue", "Crownward", "Dawnfall", "Eidolon", "Far Meridian",
        "Helio Vale", "Kharon Drift", "Lumen Reach", "Nadir", "Orison", "Praxia",
        "Red Vesper", "Sable Gate", "Tarsis", "Umbral Sea", "Vespera", "Warden's Rise"
    ];
    private static readonly string[] SurveyPrefixes =
    [
        "Kepler", "Gliese", "HD", "HIP", "LHS", "TOI", "TRAPPIST", "Wolf"
    ];
    private static readonly string[] ProvisionalPrefixes =
    [
        "Astra", "RX", "TT", "VY", "XG", "Zeta"
    ];
    private static readonly string[] LocationPrefixes =
    [
        "Amber", "Ashen", "Black", "Bright", "Cinder", "Copper", "Iron", "Ivory",
        "Meridian", "Obsidian", "Sable", "Silver", "Umber", "Violet"
    ];
    private static readonly string[] LocationSuffixes =
    [
        "Belt", "Crown", "Drift", "Expanse", "Gate", "Harbor", "March", "Reach",
        "Steppe", "Tor", "Vale", "Ward"
    ];
    private static readonly char[] PlanetLetters = ['b', 'c', 'd', 'e', 'f', 'g', 'h'];
    private static readonly char[] HalfMonthLetters =
    [
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M',
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y'
    ];

    public static string Generate()
    {
        return Random.Shared.Next(100) switch
        {
            < 20 => GenerateCatalogName(),
            < 35 => GenerateProvisionalName(),
            < 65 => Pick(ProperNames),
            _ => GenerateLocationName()
        };
    }

    private static string GenerateCatalogName()
    {
        var prefix = Pick(SurveyPrefixes);
        var letter = Pick(PlanetLetters);

        return prefix switch
        {
            "HD" => $"HD {Random.Shared.Next(10000, 99999)} {letter}",
            "HIP" => $"HIP {Random.Shared.Next(1000, 99999)} {letter}",
            "TRAPPIST" => $"TRAPPIST-{Random.Shared.Next(1, 9)} {letter}",
            "Wolf" => $"Wolf {Random.Shared.Next(100, 999)} {letter}",
            _ => $"{prefix}-{Random.Shared.Next(10, 999)} {letter}"
        };
    }

    private static string GenerateProvisionalName()
    {
        var prefix = Pick(ProvisionalPrefixes);
        var year = Random.Shared.Next(2180, 2999);
        var halfMonth = Pick(HalfMonthLetters);
        var sequence = Random.Shared.Next(1, 99);

        return $"{prefix}-{year} {halfMonth}{sequence}";
    }

    private static string GenerateLocationName()
    {
        return $"{Pick(LocationPrefixes)} {Pick(LocationSuffixes)}";
    }

    private static T Pick<T>(IReadOnlyList<T> values)
    {
        return values[Random.Shared.Next(values.Count)];
    }
}

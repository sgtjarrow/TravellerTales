namespace TravellerTales.Models;

public sealed class Character
{
    public string Name { get; set; } = string.Empty;
    public RaceType Race { get; set; } = RaceType.Human;
    public HeritageType? Heritage { get; set; } = HeritageType.Anglo;
    public GenderType Gender { get; set; } = GenderType.Male;
    public int Age { get; set; } = 18;
    public int HeightInches { get; set; }
    public int WeightPounds { get; set; }
    public double HeightMeters => HeightInches * 0.0254;
    public double WeightKilograms => WeightPounds * 0.45359237;
    public string EyeColor { get; set; } = string.Empty;
    public string? HairColor { get; set; }
    public string? FurPattern { get; set; }
    public string? FurPrimaryColor { get; set; }
    public string? FurSecondaryColor { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public CharacterCreationMetadata CreationMetadata { get; set; } = new();
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

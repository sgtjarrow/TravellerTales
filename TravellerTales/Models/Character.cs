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
    public EyeColorType EyeColor { get; set; } = EyeColorType.Brown;
    public SkinColorType SkinColor { get; set; } = SkinColorType.Tan;
    public HairColorType? HairColor { get; set; } = HairColorType.Brown;
    public FurPatternType? FurPattern { get; set; }
    public FurColorType? FurPrimaryColor { get; set; }
    public FurColorType? FurSecondaryColor { get; set; }
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

namespace TravellerTales.Models;

public enum SkillName
{
    Admin,
    Advocate,
    Animals,
    Art,
    Astrogation,
    Athletics,
    Broker,
    Carouse,
    Deception,
    Diplomat,
    Drive,
    Electronics,
    Engineer,
    Explosives,
    Flyer,
    Gambler,
    Gunner,
    GunCombat,
    HeavyWeapons,
    Investigate,
    JackOfAllTrades,
    Language,
    Leadership,
    Mechanic,
    Medic,
    Melee,
    Navigation,
    Persuade,
    Pilot,
    Profession,
    Recon,
    Science,
    Seafarer,
    Stealth,
    Steward,
    Streetwise,
    Survival,
    Tactics,
    VaccSuit
}

public enum SkillClassType
{
    ValueOnly = 0,
    SpecialtiesOnly = 1
}

public sealed class CharacterSkillSet
{
    public List<CharacterSkill> Skills { get; set; } = [];
    public List<SkillHistoryRecord> History { get; set; } = [];
}

public sealed class CharacterSkill
{
    public SkillName SkillName { get; set; }
    public bool IsTrained { get; set; }
    public int Value { get; set; }
    public List<CharacterSkillSpecialty> Specialties { get; set; } = [];
}

public sealed class CharacterSkillSpecialty
{
    public string SpecialtyId { get; set; } = string.Empty;
    public int Value { get; set; }
}

public sealed class SkillHistoryRecord
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public int Sequence { get; set; }
    public string Source { get; set; } = string.Empty;
    public SkillAdjustmentOperation Operation { get; set; }
    public SkillAdjustmentChoiceKind ChoiceKind { get; set; }
    public SkillAdjustmentOutcome Outcome { get; set; }
    public SkillName SkillName { get; set; }
    public string SpecialtyId { get; set; } = string.Empty;
    public int PreviousValue { get; set; }
    public int ResultingValue { get; set; }
    public string Message { get; set; } = string.Empty;
}

public enum SkillAdjustmentOperation
{
    Train,
    Increment,
    Set
}

public enum SkillAdjustmentChoiceKind
{
    UserChoice,
    FixedChoice
}

public enum SkillAdjustmentOutcome
{
    Applied,
    Lost,
    Rejected
}

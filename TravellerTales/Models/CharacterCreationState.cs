namespace TravellerTales.Models;

public sealed class CharacterCreationState
{
    public int CurrentStepIndex { get; set; }
    public Character Character { get; set; } = new();
    public List<HomeworldCandidate> HomeworldCandidates { get; set; } = [];
    public int? SelectedHomeworldCandidateIndex { get; set; }
    public CharacteristicAssignmentState? CharacteristicAssignment { get; set; }
    public BackgroundSkillsState BackgroundSkills { get; set; } = new();
    public CareerTermsState CareerTerms { get; set; } = new();
}

public sealed class BackgroundSkillsState
{
    public List<BackgroundSkillSelection> Selections { get; set; } = [];
    public bool Applied { get; set; }
}

public sealed class BackgroundSkillSelection
{
    public bool IsSelected { get; set; }
    public SkillName SkillName { get; set; }
    public string SpecialtyId { get; set; } = string.Empty;
}

public sealed class CareerTermsState
{
    public List<CompletedCareerTerm> CompletedTerms { get; set; } = [];
    public List<CareerHistoryRecord> CareerHistory { get; set; } = [];
    public CareerTermProgress? ActiveTerm { get; set; }
    public string CurrentCareer { get; set; } = string.Empty;
    public string CurrentAssignment { get; set; } = string.Empty;
    public int CurrentRank { get; set; }
    public int TermsInCurrentCareer { get; set; }
    public int TotalCashBenefits { get; set; }
    public bool DraftUsed { get; set; }
    public bool ReadyToFinish { get; set; }
}

public sealed class CareerHistoryRecord
{
    public string Career { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int Terms { get; set; }
    public int EntryCount { get; set; }
}

public sealed class CompletedCareerTerm
{
    public int Sequence { get; set; }
    public string Career { get; set; } = string.Empty;
    public string Assignment { get; set; } = string.Empty;
    public int CareerTermNumber { get; set; }
    public string SurvivalSummary { get; set; } = string.Empty;
    public string MishapSummary { get; set; } = string.Empty;
    public string EventSummary { get; set; } = string.Empty;
    public string CommissionSummary { get; set; } = string.Empty;
    public string AdvancementSummary { get; set; } = string.Empty;
    public string MusteringOutSummary { get; set; } = string.Empty;
    public string AgingSummary { get; set; } = string.Empty;
    public int EndingRank { get; set; }
    public bool CareerEnded { get; set; }
}

public sealed class CareerTermProgress
{
    public int Sequence { get; set; }
    public string Career { get; set; } = string.Empty;
    public string Assignment { get; set; } = string.Empty;
    public int CareerTermNumber { get; set; }
    public int StartingRank { get; set; }
    public int EndingRank { get; set; }
    public CareerTermSubStep CurrentSubStep { get; set; } = CareerTermSubStep.Career;
    public TermRollRecord? QualificationRoll { get; set; }
    public TermSingleRollRecord? DraftRoll { get; set; }
    public TermRollRecord? SurvivalRoll { get; set; }
    public TermRollRecord? MishapRoll { get; set; }
    public TermRollRecord? EventRoll { get; set; }
    public TermRollRecord? CommissionRoll { get; set; }
    public TermRollRecord? AdvancementRoll { get; set; }
    public TermRollRecord? AgingRoll { get; set; }
    public bool Qualified { get; set; }
    public bool QualificationFailed { get; set; }
    public bool EnteredByDraft { get; set; }
    public bool EnteredByDirection { get; set; }
    public bool TrainingResolved { get; set; }
    public bool SurvivalPassed { get; set; }
    public bool MishapResolved { get; set; }
    public bool EventResolved { get; set; }
    public bool CommissionSucceeded { get; set; }
    public bool AdvancementSucceeded { get; set; }
    public bool ForceCareerEnd { get; set; }
    public bool PlayerChoseCareerEnd { get; set; }
    public bool MusteringOutResolved { get; set; }
    public bool AgingResolved { get; set; }
    public bool Completed { get; set; }
    public string SurvivalSummary { get; set; } = string.Empty;
    public string QualificationSummary { get; set; } = string.Empty;
    public string DraftSummary { get; set; } = string.Empty;
    public string MishapSummary { get; set; } = string.Empty;
    public string EventSummary { get; set; } = string.Empty;
    public string CommissionSummary { get; set; } = string.Empty;
    public string AdvancementSummary { get; set; } = string.Empty;
    public string MusteringOutSummary { get; set; } = string.Empty;
    public string AgingSummary { get; set; } = string.Empty;
    public string ForcedAssignment { get; set; } = string.Empty;
}

public sealed class TermRollRecord
{
    public int Die1 { get; set; }
    public int Die2 { get; set; }
    public string CharacteristicCode { get; set; } = string.Empty;
    public int Modifier { get; set; }
    public int TargetNumber { get; set; }
    public int NaturalTotal => Die1 + Die2;
    public int ModifiedTotal => NaturalTotal + Modifier;
    public bool IsNatural12 => Die1 == 6 && Die2 == 6;
    public bool IsSuccess => TargetNumber <= 0 || ModifiedTotal >= TargetNumber;
}

public sealed class TermSingleRollRecord
{
    public int Die { get; set; }
    public string Result { get; set; } = string.Empty;
}

public enum CareerTermSubStep
{
    Career,
    QualificationChoice,
    Assignment,
    Training,
    Survival,
    Mishap,
    Event,
    CommissionAdvancement,
    Leaving,
    MusteringOut,
    Aging,
    Complete
}

public sealed class CharacteristicAssignmentState
{
    public int StrengthBase { get; set; }
    public int DexterityBase { get; set; }
    public int EnduranceBase { get; set; }
    public int IntellectBase { get; set; }
    public int EducationBase { get; set; }
    public int SocialBase { get; set; }
}

public sealed class HomeworldCandidate
{
    public string Name { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int? WorldSizeValue { get; set; }
    public int? AtmosphereValue { get; set; }
    public string TemperatureKey { get; set; } = string.Empty;
    public int? HydrographicsValue { get; set; }
    public int? PopulationValue { get; set; }
    public string? StarportCode { get; set; }
    public List<int> CulturalTagValues { get; set; } = [];
    public int? GovernmentValue { get; set; }
    public int? LawLevelValue { get; set; }
    public int? TechLevelValue { get; set; }
    public int? NumberOfGasGiants { get; set; }
    public int? NumberOfPlanetoidBelts { get; set; }
    public string? TravelCode { get; set; }
    public List<HomeworldFaction>? Factions { get; set; }
    public HomeworldBases? Bases { get; set; }
}

using TravellerTales.Models;

namespace TravellerTales.Services;

public static class CareerTermService
{
    public const int MaximumCashBenefits = 3;
    private const string DrifterCareer = "Drifter";

    private static readonly IReadOnlyList<CareerDefinition> CareerDefinitions =
    [
        new("Agent", Requirement("INT", 6),
        [
            Assignment("Law Enforcement", Requirement("END", 6), Requirement("INT", 6)),
            Assignment("Intelligence", Requirement("INT", 7), Requirement("INT", 5)),
            Assignment("Corporate", Requirement("INT", 5), Requirement("INT", 7))
        ]),
        new("Army", Requirement("END", 5),
        [
            Assignment("Support", Requirement("END", 5), Requirement("EDU", 7)),
            Assignment("Infantry", Requirement("STR", 6), Requirement("EDU", 6)),
            Assignment("Cavalry", Requirement("DEX", 7), Requirement("INT", 5))
        ]),
        new("Citizen", Requirement("EDU", 5),
        [
            Assignment("Corporate", Requirement("SOC", 6), Requirement("INT", 6)),
            Assignment("Worker", Requirement("END", 4), Requirement("EDU", 8)),
            Assignment("Colonist", Requirement("INT", 7), Requirement("END", 5))
        ]),
        new(DrifterCareer, TermRollRequirement.Automatic,
        [
            Assignment("Barbarian", Requirement("END", 7), Requirement("STR", 7)),
            Assignment("Wanderer", Requirement("END", 7), Requirement("INT", 7)),
            Assignment("Scavenger", Requirement("DEX", 7), Requirement("END", 7))
        ]),
        new("Entertainer", Requirement(["DEX", "INT"], 5),
        [
            Assignment("Artist", Requirement("SOC", 6), Requirement("INT", 6)),
            Assignment("Journalist", Requirement("EDU", 7), Requirement("INT", 5)),
            Assignment("Performer", Requirement("INT", 5), Requirement("DEX", 7))
        ]),
        new("Marine", Requirement("END", 6),
        [
            Assignment("Support", Requirement("END", 5), Requirement("EDU", 7)),
            Assignment("Star Marine", Requirement("END", 6), Requirement("EDU", 6)),
            Assignment("Ground Assault", Requirement("END", 7), Requirement("EDU", 5))
        ]),
        new("Merchant", Requirement("INT", 4),
        [
            Assignment("Merchant Marine", Requirement("EDU", 5), Requirement("INT", 7)),
            Assignment("Free Trader", Requirement("DEX", 6), Requirement("INT", 6)),
            Assignment("Broker", Requirement("EDU", 5), Requirement("INT", 7))
        ]),
        new("Navy", Requirement("INT", 6),
        [
            Assignment("Line/Crew", Requirement("INT", 5), Requirement("EDU", 7)),
            Assignment("Engineer/Gunner", Requirement("INT", 6), Requirement("EDU", 6)),
            Assignment("Flight", Requirement("DEX", 7), Requirement("EDU", 5))
        ]),
        new("Noble", Requirement("SOC", 10),
        [
            Assignment("Administrator", Requirement("INT", 4), Requirement("EDU", 6)),
            Assignment("Diplomat", Requirement("INT", 5), Requirement("SOC", 7)),
            Assignment("Dilettante", Requirement("SOC", 5), Requirement("INT", 7))
        ]),
        new("Rogue", Requirement("DEX", 6),
        [
            Assignment("Thief", Requirement("INT", 6), Requirement("DEX", 6)),
            Assignment("Enforcer", Requirement("END", 6), Requirement("STR", 6)),
            Assignment("Pirate", Requirement("DEX", 6), Requirement("INT", 6))
        ]),
        new("Scholar", Requirement("INT", 6),
        [
            Assignment("Field Researcher", Requirement("END", 6), Requirement("INT", 6)),
            Assignment("Scientist", Requirement("EDU", 4), Requirement("INT", 8)),
            Assignment("Physician", Requirement("EDU", 4), Requirement("EDU", 8))
        ]),
        new("Scout", Requirement("INT", 5),
        [
            Assignment("Courier", Requirement("END", 5), Requirement("EDU", 9)),
            Assignment("Surveyor", Requirement("END", 6), Requirement("INT", 8)),
            Assignment("Explorer", Requirement("END", 7), Requirement("EDU", 7))
        ])
    ];

    public static IReadOnlyList<CareerDefinition> Careers => CareerDefinitions;

    public static void Normalize(CareerTermsState state)
    {
        state.CompletedTerms ??= [];
        state.CareerHistory ??= [];
        state.ActiveTerm ??= null;
        state.TotalCashBenefits = Math.Clamp(state.TotalCashBenefits, 0, MaximumCashBenefits);
        state.CurrentRank = Math.Max(0, state.CurrentRank);
        state.TermsInCurrentCareer = Math.Max(0, state.TermsInCurrentCareer);

        foreach (var history in state.CareerHistory)
        {
            history.Rank = Math.Max(0, history.Rank);
            history.Terms = Math.Max(0, history.Terms);
            history.EntryCount = Math.Max(0, history.EntryCount);
        }
    }

    public static bool IsComplete(CareerTermsState state)
    {
        Normalize(state);
        return state.ActiveTerm is null && state.ReadyToFinish && state.CompletedTerms.Count > 0;
    }

    public static bool CanSave(CareerTermsState state)
    {
        Normalize(state);
        return state.ActiveTerm is null;
    }

    public static IReadOnlyList<string> GetAvailableCareerNames(CareerTermsState state)
    {
        Normalize(state);
        var enteredCareers = state.CareerHistory
            .Where(history => history.EntryCount > 0)
            .Select(history => history.Career)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return CareerDefinitions
            .Where(career => IsCareerSelectable(state, career.Name, allowDirectedEntry: false, enteredCareers))
            .Select(career => career.Name)
            .ToList();
    }

    public static IReadOnlyList<string> GetAssignmentNames(string careerName)
    {
        return FindCareer(careerName).Assignments.Select(assignment => assignment.Name).ToList();
    }

    public static CareerDefinition FindCareer(string careerName)
    {
        return CareerDefinitions.FirstOrDefault(career =>
                   string.Equals(career.Name, careerName, StringComparison.OrdinalIgnoreCase)) ??
               throw new ArgumentException($"Unknown Career: {careerName}", nameof(careerName));
    }

    public static AssignmentDefinition FindAssignment(string careerName, string assignmentName)
    {
        return FindCareer(careerName).Assignments.FirstOrDefault(assignment =>
                   string.Equals(assignment.Name, assignmentName, StringComparison.OrdinalIgnoreCase)) ??
               throw new ArgumentException($"Unknown Assignment: {careerName} / {assignmentName}", nameof(assignmentName));
    }

    public static CareerTermProgress StartNextTerm(CareerTermsState state)
    {
        Normalize(state);

        if (state.ActiveTerm is not null)
        {
            return state.ActiveTerm;
        }

        state.ReadyToFinish = false;
        state.ActiveTerm = new CareerTermProgress
        {
            Sequence = state.CompletedTerms.Count + 1,
            Career = state.CurrentCareer,
            Assignment = state.CurrentAssignment,
            CareerTermNumber = string.IsNullOrWhiteSpace(state.CurrentCareer)
                ? 1
                : state.TermsInCurrentCareer + 1,
            StartingRank = state.CurrentRank,
            EndingRank = state.CurrentRank,
            Qualified = !string.IsNullOrWhiteSpace(state.CurrentCareer),
            CurrentSubStep = CareerTermSubStep.Career
        };

        return state.ActiveTerm;
    }

    public static void SelectCareer(CareerTermsState state, string career, bool allowDirectedEntry = false)
    {
        var term = RequireActiveTerm(state);
        var definition = FindCareer(career);
        if (!IsCareerSelectable(state, definition.Name, allowDirectedEntry))
        {
            throw new InvalidOperationException($"{definition.Name} has already been entered and cannot be selected again.");
        }

        var continuingCareer = IsContinuingCurrentCareer(state, definition.Name);
        var history = FindCareerHistory(state, definition.Name);
        term.Career = definition.Name;
        term.Assignment = string.Empty;
        term.ForcedAssignment = string.Empty;
        term.QualificationFailed = false;
        term.QualificationSummary = string.Empty;
        term.DraftSummary = string.Empty;
        term.DraftRoll = null;
        term.EnteredByDraft = false;
        term.EnteredByDirection = allowDirectedEntry && !continuingCareer;
        term.CareerTermNumber = continuingCareer ? state.TermsInCurrentCareer + 1 : history.Terms + 1;
        term.StartingRank = continuingCareer ? state.CurrentRank : history.Rank;
        term.EndingRank = term.StartingRank;
        term.QualificationRoll = null;
        term.Qualified = continuingCareer || definition.Qualification.IsAutomatic;

        if (definition.Qualification.IsAutomatic)
        {
            term.QualificationSummary = $"{definition.Name} qualification is Automatic.";
        }

        if (term.Qualified)
        {
            term.CurrentSubStep = CareerTermSubStep.Assignment;
        }
    }

    public static TermRollRecord RollQualification(Character character, CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        var career = FindCareer(term.Career);
        if (career.Qualification.IsAutomatic)
        {
            term.Qualified = true;
            term.QualificationSummary = $"{career.Name} qualification is Automatic.";
            term.CurrentSubStep = CareerTermSubStep.Assignment;
            return CreateRoll(1, 1, 0, string.Empty, 0);
        }

        var roll = Roll2D(character, career.Qualification);
        term.QualificationRoll = roll;
        term.Qualified = roll.IsSuccess;
        term.QualificationFailed = !roll.IsSuccess;
        term.QualificationSummary = term.Qualified
            ? $"Qualified for {career.Name}: {FormatRoll(roll)} vs {career.Qualification.DisplayText}."
            : $"Failed to qualify for {career.Name}: {FormatRoll(roll)} vs {career.Qualification.DisplayText}.";
        term.CurrentSubStep = term.Qualified ? CareerTermSubStep.Assignment : CareerTermSubStep.QualificationChoice;
        return roll;
    }

    public static void EnterDrifterAfterFailedQualification(CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.QualificationChoice);
        if (!term.QualificationFailed)
        {
            throw new InvalidOperationException("Drifter fallback is only available after failed qualification.");
        }

        var failedSummary = term.QualificationSummary;
        SelectCareer(state, DrifterCareer);
        term.QualificationSummary = string.IsNullOrWhiteSpace(failedSummary)
            ? "Entered Drifter after failed qualification."
            : $"{failedSummary} Entered Drifter.";
    }

    public static TermSingleRollRecord SubmitToDraft(CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.QualificationChoice);
        if (state.DraftUsed)
        {
            throw new InvalidOperationException("The Draft can only be used once.");
        }

        var die = DiceRoller.Roll(6);
        var draft = ResolveDraft(die);
        state.DraftUsed = true;
        term.DraftRoll = new TermSingleRollRecord
        {
            Die = die,
            Result = draft.ForcedAssignment is null
                ? $"{draft.Career} (any Assignment)"
                : $"{draft.Career} ({draft.ForcedAssignment})"
        };

        EnterDirectedCareer(state, draft.Career, draft.ForcedAssignment, enteredByDraft: true);
        term.DraftSummary = $"Draft roll {die}: {term.DraftRoll.Result}.";
        return term.DraftRoll;
    }

    public static void SelectAssignment(CareerTermsState state, string assignment)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.Assignment);
        if (!string.IsNullOrWhiteSpace(term.ForcedAssignment) &&
            !string.Equals(term.ForcedAssignment, assignment, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"This entry requires the {term.ForcedAssignment} Assignment.");
        }

        var definition = FindAssignment(term.Career, assignment);
        term.Assignment = definition.Name;
        term.CurrentSubStep = CareerTermSubStep.Training;
    }

    public static void ResolveTraining(CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.Training);
        term.TrainingResolved = true;
        term.CurrentSubStep = CareerTermSubStep.Survival;
    }

    public static TermRollRecord RollSurvival(Character character, CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.Survival);

        var assignment = FindAssignment(term.Career, term.Assignment);
        var roll = Roll2D(character, assignment.Survival);
        term.SurvivalRoll = roll;
        term.SurvivalPassed = roll.IsSuccess;
        term.SurvivalSummary = term.SurvivalPassed
            ? $"Survived: {FormatRoll(roll)} vs {assignment.Survival.DisplayText}."
            : $"Failed Survival: {FormatRoll(roll)} vs {assignment.Survival.DisplayText}.";
        term.CurrentSubStep = term.SurvivalPassed ? CareerTermSubStep.Event : CareerTermSubStep.Mishap;
        return roll;
    }

    public static TermRollRecord ResolveMishap(CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.Mishap);

        var roll = Roll2D();
        term.MishapRoll = roll;
        term.MishapResolved = true;
        term.ForceCareerEnd = true;
        term.MishapSummary = $"Placeholder Mishap {roll.NaturalTotal}: career ends after this Term.";
        term.CurrentSubStep = CareerTermSubStep.Leaving;
        return roll;
    }

    public static TermRollRecord RollEvent(CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.Event);

        var roll = Roll2D();
        term.EventRoll = roll;
        term.EventResolved = true;
        term.EventSummary = $"Placeholder Event {roll.NaturalTotal}: no additional effect.";
        term.CurrentSubStep = CareerTermSubStep.CommissionAdvancement;
        return roll;
    }

    public static (TermRollRecord? Commission, TermRollRecord Advancement) RollCommissionAndAdvancement(Character character, CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.CommissionAdvancement);

        TermRollRecord? commission = null;
        if (term.StartingRank == 0)
        {
            commission = Roll2D();
            term.CommissionRoll = commission;
            term.CommissionSucceeded = commission.ModifiedTotal >= 8;
            term.CommissionSummary = term.CommissionSucceeded
                ? $"Commissioned on {FormatRoll(commission)}; rank increases to 1."
                : $"Commission failed on {FormatRoll(commission)}.";

            if (term.CommissionSucceeded)
            {
                term.EndingRank = Math.Max(term.EndingRank, 1);
            }
        }
        else
        {
            term.CommissionSummary = "Not eligible for Commission.";
        }

        var assignment = FindAssignment(term.Career, term.Assignment);
        var advancement = Roll2D(character, assignment.Advancement);
        term.AdvancementRoll = advancement;
        term.AdvancementSucceeded = advancement.IsSuccess;
        if (term.AdvancementSucceeded)
        {
            term.EndingRank++;
        }

        var forcedByAdvancementRule = advancement.ModifiedTotal <= term.CareerTermNumber && !advancement.IsNatural12;
        term.ForceCareerEnd = term.ForceCareerEnd || forcedByAdvancementRule;
        term.AdvancementSummary = term.AdvancementSucceeded
            ? $"Advanced: {FormatRoll(advancement)} vs {assignment.Advancement.DisplayText}; rank increases to {term.EndingRank}."
            : $"Advancement failed: {FormatRoll(advancement)} vs {assignment.Advancement.DisplayText}.";

        if (forcedByAdvancementRule)
        {
            term.AdvancementSummary += " Advancement result forces career exit.";
        }

        term.CurrentSubStep = CareerTermSubStep.Leaving;
        return (commission, advancement);
    }

    public static void ResolveLeaving(CareerTermsState state, bool leaveCareer)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.Leaving);

        term.PlayerChoseCareerEnd = leaveCareer;
        term.CurrentSubStep = IsCareerEnding(term)
            ? CareerTermSubStep.MusteringOut
            : CareerTermSubStep.Aging;
    }

    public static void ResolveMusteringOut(CareerTermsState state, CareerBenefitKind benefitKind)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.MusteringOut);

        if (benefitKind == CareerBenefitKind.Cash && state.TotalCashBenefits < MaximumCashBenefits)
        {
            state.TotalCashBenefits++;
            term.MusteringOutSummary = $"Cash Benefit received ({state.TotalCashBenefits}/{MaximumCashBenefits}).";
        }
        else if (benefitKind == CareerBenefitKind.Cash)
        {
            term.MusteringOutSummary = "Cash Benefit limit reached; Material Benefit received instead.";
        }
        else
        {
            term.MusteringOutSummary = "Material Benefit received.";
        }

        term.MusteringOutResolved = true;
        term.CurrentSubStep = CareerTermSubStep.Aging;
    }

    public static TermRollRecord? ResolveAging(Character character, CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.Aging);

        var ageAfterTerm = character.Age + 4;
        TermRollRecord? roll = null;

        if (ageAfterTerm >= 34)
        {
            roll = Roll2D();
            term.AgingRoll = roll;
            term.AgingSummary = $"Aging check at age {ageAfterTerm}: {FormatRoll(roll)}; no placeholder effect.";
        }
        else
        {
            term.AgingSummary = $"No aging check at age {ageAfterTerm}.";
        }

        character.Age = ageAfterTerm;
        term.AgingResolved = true;
        term.CurrentSubStep = CareerTermSubStep.Complete;
        return roll;
    }

    public static CompletedCareerTerm CompleteActiveTerm(CareerTermsState state)
    {
        var term = RequireActiveTerm(state);
        RequireStep(term, CareerTermSubStep.Complete);

        var careerEnded = IsCareerEnding(term);
        var completed = new CompletedCareerTerm
        {
            Sequence = term.Sequence,
            Career = term.Career,
            Assignment = term.Assignment,
            CareerTermNumber = term.CareerTermNumber,
            SurvivalSummary = term.SurvivalSummary,
            MishapSummary = term.MishapSummary,
            EventSummary = term.EventSummary,
            CommissionSummary = term.CommissionSummary,
            AdvancementSummary = term.AdvancementSummary,
            MusteringOutSummary = term.MusteringOutSummary,
            AgingSummary = term.AgingSummary,
            EndingRank = term.EndingRank,
            CareerEnded = careerEnded
        };

        state.CompletedTerms.Add(completed);

        var history = FindCareerHistory(state, term.Career);
        history.Rank = term.EndingRank;
        history.Terms = Math.Max(history.Terms, term.CareerTermNumber);
        history.EntryCount = Math.Max(1, history.EntryCount);

        if (careerEnded)
        {
            state.CurrentCareer = string.Empty;
            state.CurrentAssignment = string.Empty;
            state.CurrentRank = 0;
            state.TermsInCurrentCareer = 0;
        }
        else
        {
            state.CurrentCareer = term.Career;
            state.CurrentAssignment = term.Assignment;
            state.CurrentRank = term.EndingRank;
            state.TermsInCurrentCareer = term.CareerTermNumber;
        }

        term.Completed = true;
        state.ActiveTerm = null;
        return completed;
    }

    public static void MarkReadyToFinish(CareerTermsState state)
    {
        Normalize(state);
        if (state.ActiveTerm is null && state.CompletedTerms.Count > 0)
        {
            state.ReadyToFinish = true;
        }
    }

    public static TermRollRecord CreateRoll(int die1, int die2, int modifier = 0, string characteristicCode = "", int targetNumber = 0)
    {
        if (die1 is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(die1), die1, "Die value must be between 1 and 6.");
        }

        if (die2 is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(die2), die2, "Die value must be between 1 and 6.");
        }

        return new TermRollRecord
        {
            Die1 = die1,
            Die2 = die2,
            CharacteristicCode = characteristicCode,
            Modifier = modifier,
            TargetNumber = targetNumber
        };
    }

    public static bool WouldAdvancementRollForceCareerExit(TermRollRecord roll, int careerTermNumber)
    {
        return roll.ModifiedTotal <= careerTermNumber && !roll.IsNatural12;
    }

    public static TermRollContext BuildRollContext(Character character, TermRollRequirement requirement)
    {
        if (requirement.IsAutomatic)
        {
            return new TermRollContext(requirement, string.Empty, 0, true);
        }

        var best = requirement.CharacteristicCodes
            .Select(code => new { Code = code, Modifier = GetCharacteristicModifier(character, code) })
            .OrderByDescending(item => item.Modifier)
            .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .First();

        return new TermRollContext(requirement, best.Code, best.Modifier, false);
    }

    private static void EnterDirectedCareer(CareerTermsState state, string career, string? forcedAssignment, bool enteredByDraft)
    {
        var term = RequireActiveTerm(state);
        var history = FindCareerHistory(state, career);
        term.Career = career;
        term.Assignment = forcedAssignment ?? string.Empty;
        term.ForcedAssignment = forcedAssignment ?? string.Empty;
        term.Qualified = true;
        term.QualificationFailed = false;
        term.EnteredByDraft = enteredByDraft;
        term.EnteredByDirection = true;
        term.CareerTermNumber = history.Terms + 1;
        term.StartingRank = history.Rank;
        term.EndingRank = history.Rank;
        term.CurrentSubStep = CareerTermSubStep.Assignment;
    }

    private static CareerHistoryRecord FindCareerHistory(CareerTermsState state, string career)
    {
        Normalize(state);
        var history = state.CareerHistory.FirstOrDefault(item =>
            string.Equals(item.Career, career, StringComparison.OrdinalIgnoreCase));
        if (history is not null)
        {
            return history;
        }

        history = new CareerHistoryRecord
        {
            Career = career
        };
        state.CareerHistory.Add(history);
        return history;
    }

    private static bool IsCareerSelectable(
        CareerTermsState state,
        string career,
        bool allowDirectedEntry,
        HashSet<string>? enteredCareers = null)
    {
        if (string.Equals(career, DrifterCareer, StringComparison.OrdinalIgnoreCase) ||
            allowDirectedEntry ||
            IsContinuingCurrentCareer(state, career))
        {
            return true;
        }

        enteredCareers ??= state.CareerHistory
            .Where(history => history.EntryCount > 0)
            .Select(history => history.Career)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return !enteredCareers.Contains(career);
    }

    private static bool IsContinuingCurrentCareer(CareerTermsState state, string career)
    {
        return !string.IsNullOrWhiteSpace(state.CurrentCareer) &&
               string.Equals(state.CurrentCareer, career, StringComparison.OrdinalIgnoreCase);
    }

    public static DraftResult ResolveDraft(int die)
    {
        return die switch
        {
            1 => new DraftResult("Navy"),
            2 => new DraftResult("Army"),
            3 => new DraftResult("Marine"),
            4 => new DraftResult("Merchant", "Merchant Marine"),
            5 => new DraftResult("Scout"),
            6 => new DraftResult("Agent", "Law Enforcement"),
            _ => throw new ArgumentOutOfRangeException(nameof(die), die, "Draft die must be between 1 and 6.")
        };
    }

    private static CareerTermProgress RequireActiveTerm(CareerTermsState state)
    {
        Normalize(state);
        return state.ActiveTerm ?? throw new InvalidOperationException("No active Career Term exists.");
    }

    private static void RequireStep(CareerTermProgress term, CareerTermSubStep step)
    {
        if (term.CurrentSubStep != step)
        {
            throw new InvalidOperationException($"Current Career Term step is {term.CurrentSubStep}, not {step}.");
        }
    }

    private static TermRollRecord Roll2D()
    {
        return CreateRoll(DiceRoller.Roll(6), DiceRoller.Roll(6));
    }

    private static TermRollRecord Roll2D(Character character, TermRollRequirement requirement)
    {
        var context = BuildRollContext(character, requirement);
        return CreateRoll(DiceRoller.Roll(6), DiceRoller.Roll(6), context.Modifier, context.CharacteristicCode, requirement.TargetNumber);
    }

    private static int GetCharacteristicModifier(Character character, string code)
    {
        var value = code.ToUpperInvariant() switch
        {
            "STR" => character.CurrentCharacteristics.Strength,
            "DEX" => character.CurrentCharacteristics.Dexterity,
            "END" => character.CurrentCharacteristics.Endurance,
            "INT" => character.CurrentCharacteristics.Intellect,
            "EDU" => character.CurrentCharacteristics.Education,
            "SOC" => character.CurrentCharacteristics.Social,
            _ => throw new ArgumentException($"Unknown characteristic code: {code}", nameof(code))
        };

        return CharacteristicRules.GetDiceModifier(value);
    }

    private static bool IsCareerEnding(CareerTermProgress term)
    {
        return term.ForceCareerEnd || term.PlayerChoseCareerEnd;
    }

    private static string FormatRoll(TermRollRecord roll)
    {
        var modifier = roll.Modifier == 0 ? string.Empty : roll.Modifier > 0 ? $" +{roll.Modifier}" : $" {roll.Modifier}";
        var target = roll.TargetNumber > 0 ? $" against {roll.TargetNumber}+" : string.Empty;
        var characteristic = string.IsNullOrWhiteSpace(roll.CharacteristicCode) ? string.Empty : $" {roll.CharacteristicCode}";
        return $"{roll.Die1}+{roll.Die2}{characteristic}{modifier} = {roll.ModifiedTotal}{target}";
    }

    private static AssignmentDefinition Assignment(string name, TermRollRequirement survival, TermRollRequirement advancement)
    {
        return new AssignmentDefinition(name, survival, advancement);
    }

    private static TermRollRequirement Requirement(string characteristicCode, int targetNumber)
    {
        return Requirement([characteristicCode], targetNumber);
    }

    private static TermRollRequirement Requirement(IReadOnlyList<string> characteristicCodes, int targetNumber)
    {
        var display = $"{string.Join(" or ", characteristicCodes)} {targetNumber}+";
        return new TermRollRequirement(characteristicCodes, targetNumber, false, display);
    }
}

public sealed record CareerDefinition(
    string Name,
    TermRollRequirement Qualification,
    IReadOnlyList<AssignmentDefinition> Assignments);

public sealed record AssignmentDefinition(
    string Name,
    TermRollRequirement Survival,
    TermRollRequirement Advancement);

public sealed record TermRollRequirement(
    IReadOnlyList<string> CharacteristicCodes,
    int TargetNumber,
    bool IsAutomatic,
    string DisplayText)
{
    public static TermRollRequirement Automatic { get; } = new([], 0, true, "Automatic");
}

public sealed record TermRollContext(
    TermRollRequirement Requirement,
    string CharacteristicCode,
    int Modifier,
    bool IsAutomatic);

public sealed record DraftResult(string Career, string? ForcedAssignment = null);

public enum CareerBenefitKind
{
    Cash,
    Material
}

using TravellerTales.Models;

namespace TravellerTales.Services;

public static class CharacteristicRules
{
    public static readonly CharacteristicDefinition Strength = new(CharacteristicKind.Strength, "Strength", "STR");
    public static readonly CharacteristicDefinition Dexterity = new(CharacteristicKind.Dexterity, "Dexterity", "DEX");
    public static readonly CharacteristicDefinition Endurance = new(CharacteristicKind.Endurance, "Endurance", "END");
    public static readonly CharacteristicDefinition Intellect = new(CharacteristicKind.Intellect, "Intellect", "INT");
    public static readonly CharacteristicDefinition Education = new(CharacteristicKind.Education, "Education", "EDU");
    public static readonly CharacteristicDefinition Social = new(CharacteristicKind.Social, "Social", "SOC");

    public static IReadOnlyList<CharacteristicDefinition> All { get; } =
    [
        Strength,
        Dexterity,
        Endurance,
        Intellect,
        Education,
        Social
    ];

    public static CharacteristicAssignmentState GenerateAssignment()
    {
        var rolls = Enumerable.Range(0, 7)
            .Select(_ => DiceRoller.Roll(2, 6))
            .OrderDescending()
            .Take(6)
            .OrderBy(_ => Random.Shared.Next())
            .ToArray();

        return new()
        {
            StrengthBase = rolls[0],
            DexterityBase = rolls[1],
            EnduranceBase = rolls[2],
            IntellectBase = rolls[3],
            EducationBase = rolls[4],
            SocialBase = rolls[5]
        };
    }

    public static int GetRaceAdjustment(RaceType race, CharacteristicKind kind)
    {
        return (race, kind) switch
        {
            (RaceType.Aslan, CharacteristicKind.Strength) => 2,
            (RaceType.Aslan, CharacteristicKind.Dexterity) => -2,
            (RaceType.Vargr, CharacteristicKind.Strength) => -1,
            (RaceType.Vargr, CharacteristicKind.Dexterity) => 1,
            (RaceType.Vargr, CharacteristicKind.Endurance) => -1,
            _ => 0
        };
    }

    public static int ApplyRaceAdjustment(int baseValue, RaceType race, CharacteristicKind kind)
    {
        return Math.Clamp(baseValue + GetRaceAdjustment(race, kind), 0, 15);
    }

    public static int GetDiceModifier(int value)
    {
        return value switch
        {
            <= 0 => -3,
            <= 2 => -2,
            <= 5 => -1,
            <= 8 => 0,
            <= 11 => 1,
            <= 14 => 2,
            _ => 3
        };
    }

    public static CharacteristicSet BuildAdjustedSet(CharacteristicAssignmentState assignment, RaceType race)
    {
        return new()
        {
            Strength = ApplyRaceAdjustment(assignment.StrengthBase, race, CharacteristicKind.Strength),
            Dexterity = ApplyRaceAdjustment(assignment.DexterityBase, race, CharacteristicKind.Dexterity),
            Endurance = ApplyRaceAdjustment(assignment.EnduranceBase, race, CharacteristicKind.Endurance),
            Intellect = ApplyRaceAdjustment(assignment.IntellectBase, race, CharacteristicKind.Intellect),
            Education = ApplyRaceAdjustment(assignment.EducationBase, race, CharacteristicKind.Education),
            Social = ApplyRaceAdjustment(assignment.SocialBase, race, CharacteristicKind.Social)
        };
    }

    public static int GetBaseValue(CharacteristicAssignmentState assignment, CharacteristicKind kind)
    {
        return kind switch
        {
            CharacteristicKind.Strength => assignment.StrengthBase,
            CharacteristicKind.Dexterity => assignment.DexterityBase,
            CharacteristicKind.Endurance => assignment.EnduranceBase,
            CharacteristicKind.Intellect => assignment.IntellectBase,
            CharacteristicKind.Education => assignment.EducationBase,
            CharacteristicKind.Social => assignment.SocialBase,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown characteristic.")
        };
    }

    public static void SetBaseValue(CharacteristicAssignmentState assignment, CharacteristicKind kind, int value)
    {
        switch (kind)
        {
            case CharacteristicKind.Strength:
                assignment.StrengthBase = value;
                break;
            case CharacteristicKind.Dexterity:
                assignment.DexterityBase = value;
                break;
            case CharacteristicKind.Endurance:
                assignment.EnduranceBase = value;
                break;
            case CharacteristicKind.Intellect:
                assignment.IntellectBase = value;
                break;
            case CharacteristicKind.Education:
                assignment.EducationBase = value;
                break;
            case CharacteristicKind.Social:
                assignment.SocialBase = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown characteristic.");
        }
    }

    public static int GetValue(CharacteristicSet characteristics, CharacteristicKind kind)
    {
        return kind switch
        {
            CharacteristicKind.Strength => characteristics.Strength,
            CharacteristicKind.Dexterity => characteristics.Dexterity,
            CharacteristicKind.Endurance => characteristics.Endurance,
            CharacteristicKind.Intellect => characteristics.Intellect,
            CharacteristicKind.Education => characteristics.Education,
            CharacteristicKind.Social => characteristics.Social,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown characteristic.")
        };
    }
}

public enum CharacteristicKind
{
    Strength,
    Dexterity,
    Endurance,
    Intellect,
    Education,
    Social
}

public sealed record CharacteristicDefinition(
    CharacteristicKind Kind,
    string Name,
    string Code);

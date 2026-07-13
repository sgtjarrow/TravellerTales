using TravellerTales.Models;

namespace TravellerTales.Services;

public static class BackgroundSkillService
{
    public const string Source = "Background Skills";

    public static readonly IReadOnlyList<SkillName> AllowedSkills =
    [
        SkillName.Admin,
        SkillName.Animals,
        SkillName.Art,
        SkillName.Athletics,
        SkillName.Carouse,
        SkillName.Drive,
        SkillName.Electronics,
        SkillName.Flyer,
        SkillName.Language,
        SkillName.Mechanic,
        SkillName.Medic,
        SkillName.Profession,
        SkillName.Science,
        SkillName.Seafarer,
        SkillName.Streetwise,
        SkillName.Survival,
        SkillName.VaccSuit
    ];

    public static int GetRequiredSelectionCount(Character character)
    {
        return Math.Max(0, 3 + CharacteristicRules.GetDiceModifier(character.CurrentCharacteristics.Education));
    }

    public static IReadOnlyList<SkillName> GetAllowedSkillsForSlot(
        IEnumerable<BackgroundSkillSelection> selections,
        int slotIndex)
    {
        var blockedSkills = selections
            .Select((selection, index) => new { selection, index })
            .Where(item => item.index != slotIndex &&
                           IsFilled(item.selection) &&
                           item.selection.SkillName != SkillName.Profession)
            .Select(item => item.selection.SkillName)
            .ToHashSet();

        return AllowedSkills
            .Where(skillName => !blockedSkills.Contains(skillName))
            .ToList();
    }

    public static bool IsComplete(Character character, BackgroundSkillsState state)
    {
        var requiredCount = GetRequiredSelectionCount(character);
        EnsureSlotCount(state, requiredCount);
        return state.Selections.Count == requiredCount &&
               state.Selections.All(selection => IsFilled(selection)) &&
               GetValidationErrors(state.Selections).Count == 0;
    }

    public static IReadOnlyList<string> GetValidationErrors(IEnumerable<BackgroundSkillSelection> selections)
    {
        var selectionList = selections.Where(IsFilled).ToList();
        var errors = new List<string>();

        var duplicateSkills = selectionList
            .Where(selection => selection.SkillName != SkillName.Profession)
            .GroupBy(selection => selection.SkillName)
            .Where(group => group.Count() > 1)
            .Select(group => SkillCatalog.GetDisplayName(group.Key));

        errors.AddRange(duplicateSkills.Select(skillName => $"{skillName} can only be selected once."));

        var duplicateProfessionSpecialties = selectionList
            .Where(selection => selection.SkillName == SkillName.Profession)
            .GroupBy(selection => selection.SpecialtyId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        errors.AddRange(duplicateProfessionSpecialties.Select(_ => "Each Profession Specialty can only be selected once."));
        return errors;
    }

    public static IReadOnlyList<SkillAdjustmentResult> ApplySelections(
        Character character,
        BackgroundSkillsState state,
        SkillSpecialtyCatalog catalog)
    {
        if (state.Applied)
        {
            return [];
        }

        var results = new List<SkillAdjustmentResult>();
        foreach (var selection in state.Selections.Where(IsFilled))
        {
            var result = SkillAdjustmentService.ApplyAndTrackSpecialtySelection(
                new SkillAdjustmentRequest(
                    character,
                    selection.SkillName,
                    SkillAdjustmentOperation.Train,
                    0,
                    Source,
                    SkillAdjustmentChoiceKind.UserChoice,
                    SpecialtyId: selection.SpecialtyId),
                catalog);

            results.Add(result);
        }

        state.Applied = true;
        return results;
    }

    public static void EnsureSlotCount(BackgroundSkillsState state, int requiredCount)
    {
        state.Selections ??= [];

        while (state.Selections.Count < requiredCount)
        {
            state.Selections.Add(new BackgroundSkillSelection());
        }

        if (state.Selections.Count > requiredCount)
        {
            state.Selections.RemoveRange(requiredCount, state.Selections.Count - requiredCount);
        }
    }

    public static bool IsFilled(BackgroundSkillSelection selection)
    {
        return selection.IsSelected &&
               Enum.IsDefined(selection.SkillName) &&
               (SkillCatalog.IsValueOnly(selection.SkillName) ||
                selection.SkillName != SkillName.Profession ||
                !string.IsNullOrWhiteSpace(selection.SpecialtyId));
    }
}

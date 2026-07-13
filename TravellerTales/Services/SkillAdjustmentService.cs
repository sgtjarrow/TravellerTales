using TravellerTales.Models;

namespace TravellerTales.Services;

public sealed record SkillAdjustmentRequest(
    Character Character,
    SkillName SkillName,
    SkillAdjustmentOperation Operation,
    int Value,
    string Source,
    SkillAdjustmentChoiceKind ChoiceKind,
    bool EnforceCreationCap = true,
    string SpecialtyId = "");

public sealed record SkillAdjustmentResult(
    SkillAdjustmentOutcome Outcome,
    string Message,
    int PreviousValue,
    int ResultingValue,
    SkillHistoryRecord HistoryRecord)
{
    public bool Applied => Outcome == SkillAdjustmentOutcome.Applied;
}

public sealed record SkillSelectionOption(
    SkillName SkillName,
    string DisplayName,
    bool IsEnabled,
    string DisabledReason);

public sealed record SpecialtySelectionOption(
    string SpecialtyId,
    string DisplayName,
    int CurrentValue,
    int SelectedCount,
    bool IsEnabled,
    string DisabledReason);

public static class SkillAdjustmentService
{
    public const int UntrainedValue = -3;
    public const int TrainedValue = 0;
    public const int CharacterCreationMaximum = 4;

    public static SkillAdjustmentResult Apply(SkillAdjustmentRequest request)
    {
        request.Character.Skills ??= new();

        var result = SkillCatalog.IsValueOnly(request.SkillName)
            ? ApplyValueOnly(request)
            : ApplySpecialty(request);

        request.Character.Skills.History.Add(result.HistoryRecord);
        return result;
    }

    public static SkillAdjustmentResult ApplyAndTrackSpecialtySelection(
        SkillAdjustmentRequest request,
        SkillSpecialtyCatalog catalog)
    {
        var result = Apply(request);
        if (result.Applied && !string.IsNullOrWhiteSpace(request.SpecialtyId))
        {
            SpecialtyCatalogService.IncrementSelectedCount(catalog, request.SpecialtyId);
        }

        return result;
    }

    public static int GetEffectiveValue(Character character, SkillName skillName)
    {
        var skill = FindSkill(character, skillName);
        return skill is null || !skill.IsTrained ? UntrainedValue : skill.Value;
    }

    public static int GetEffectiveSpecialtyValue(Character character, SkillName skillName, string specialtyId)
    {
        var skill = FindSkill(character, skillName);
        if (skill is null || !skill.IsTrained)
        {
            return UntrainedValue;
        }

        var specialty = skill.Specialties.FirstOrDefault(item =>
            string.Equals(item.SpecialtyId, specialtyId, StringComparison.OrdinalIgnoreCase));
        if (skillName == SkillName.Profession)
        {
            return specialty?.Value ?? UntrainedValue;
        }

        return specialty?.Value ?? TrainedValue;
    }

    public static IReadOnlyList<SkillSelectionOption> BuildSkillOptions(
        Character character,
        IEnumerable<SkillName> allowedSkills,
        SkillAdjustmentOperation operation,
        int value,
        SkillAdjustmentChoiceKind choiceKind,
        bool enforceCreationCap = true)
    {
        return allowedSkills
            .Distinct()
            .Select(skillName =>
            {
                var wouldExceed = SkillCatalog.IsValueOnly(skillName) &&
                                  WouldExceedCreationMaximum(character, skillName, string.Empty, operation, value);
                var isEnabled = !enforceCreationCap ||
                                choiceKind == SkillAdjustmentChoiceKind.FixedChoice ||
                                !wouldExceed;

                return new SkillSelectionOption(
                    skillName,
                    SkillCatalog.GetDisplayName(skillName),
                    isEnabled,
                    isEnabled ? string.Empty : "Already at the character creation maximum.");
            })
            .OrderBy(option => option.DisplayName)
            .ToList();
    }

    public static IReadOnlyList<SpecialtySelectionOption> BuildSpecialtyOptions(
        Character character,
        SkillName skillName,
        SkillSpecialtyCatalog catalog,
        SkillAdjustmentOperation operation,
        int value,
        SkillAdjustmentChoiceKind choiceKind,
        bool enforceCreationCap = true)
    {
        var skill = FindSkill(character, skillName);
        var valuedSpecialties = (skill?.Specialties ?? [])
            .Select(specialty =>
            {
                var definition = catalog.FindById(specialty.SpecialtyId);
                return definition is null
                    ? null
                    : new SpecialtySelectionOption(
                        specialty.SpecialtyId,
                        definition.DisplayName,
                        specialty.Value,
                        definition.SelectedCount,
                        IsSpecialtyEnabled(character, skillName, specialty.SpecialtyId, operation, value, choiceKind, enforceCreationCap),
                        GetSpecialtyDisabledReason(character, skillName, specialty.SpecialtyId, operation, value, choiceKind, enforceCreationCap));
            })
            .Where(option => option is not null)
            .Cast<SpecialtySelectionOption>()
            .OrderByDescending(option => option.CurrentValue)
            .ThenBy(option => option.DisplayName)
            .ToList();

        var currentIds = valuedSpecialties.Select(option => option.SpecialtyId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var remaining = catalog.GetForSkill(skillName)
            .Where(specialty => !currentIds.Contains(specialty.Id))
            .Select(specialty => new SpecialtySelectionOption(
                specialty.Id,
                specialty.DisplayName,
                TrainedValue,
                specialty.SelectedCount,
                IsSpecialtyEnabled(character, skillName, specialty.Id, operation, value, choiceKind, enforceCreationCap),
                GetSpecialtyDisabledReason(character, skillName, specialty.Id, operation, value, choiceKind, enforceCreationCap)))
            .OrderByDescending(option => option.SelectedCount)
            .ThenBy(option => option.DisplayName);

        return valuedSpecialties.Concat(remaining).ToList();
    }

    public static bool HasCreationCapViolation(Character character)
    {
        return GetCreationCapViolations(character).Count > 0;
    }

    public static IReadOnlyList<string> GetCreationCapViolations(Character character)
    {
        var violations = new List<string>();

        foreach (var skill in character.Skills?.Skills ?? [])
        {
            if (SkillCatalog.IsValueOnly(skill.SkillName) && skill.Value > CharacterCreationMaximum)
            {
                violations.Add($"{SkillCatalog.GetDisplayName(skill.SkillName)} {skill.Value}");
            }

            foreach (var specialty in skill.Specialties.Where(item => item.Value > CharacterCreationMaximum))
            {
                violations.Add($"{SkillCatalog.GetDisplayName(skill.SkillName)} specialty {specialty.SpecialtyId} {specialty.Value}");
            }
        }

        return violations;
    }

    public static CharacterSkillSet NormalizeSkillSet(CharacterSkillSet? skillSet)
    {
        skillSet ??= new();
        skillSet.History ??= [];
        skillSet.Skills ??= [];

        var normalizedSkills = new List<CharacterSkill>();

        foreach (var skill in skillSet.Skills)
        {
            if (!Enum.IsDefined(skill.SkillName))
            {
                continue;
            }

            skill.Specialties ??= [];

            if (SkillCatalog.IsValueOnly(skill.SkillName))
            {
                if (!skill.IsTrained || skill.Value < TrainedValue)
                {
                    continue;
                }

                skill.Specialties.Clear();
                normalizedSkills.Add(skill);
                continue;
            }

            skill.Value = TrainedValue;
            skill.Specialties = skill.Specialties
                .Where(specialty => !string.IsNullOrWhiteSpace(specialty.SpecialtyId) &&
                                    (skill.SkillName == SkillName.Profession
                                        ? specialty.Value >= TrainedValue
                                        : specialty.Value > TrainedValue))
                .GroupBy(specialty => specialty.SpecialtyId, StringComparer.OrdinalIgnoreCase)
                .Select(group => new CharacterSkillSpecialty
                {
                    SpecialtyId = group.First().SpecialtyId,
                    Value = group.Max(specialty => specialty.Value)
                })
                .OrderBy(specialty => specialty.SpecialtyId)
                .ToList();

            if (skill.IsTrained || skill.Specialties.Count > 0)
            {
                skill.IsTrained = true;
                normalizedSkills.Add(skill);
            }
        }

        skillSet.Skills = normalizedSkills
            .GroupBy(skill => skill.SkillName)
            .Select(group => group.First())
            .OrderBy(skill => SkillCatalog.GetDisplayName(skill.SkillName))
            .ToList();

        for (var index = 0; index < skillSet.History.Count; index++)
        {
            if (skillSet.History[index].Sequence <= 0)
            {
                skillSet.History[index].Sequence = index + 1;
            }
        }

        return skillSet;
    }

    private static SkillAdjustmentResult ApplyValueOnly(SkillAdjustmentRequest request)
    {
        var previousValue = GetEffectiveValue(request.Character, request.SkillName);
        var resultingValue = GetResultingValue(previousValue, request.Operation, request.Value);
        var capOutcome = CheckCreationCap(request, previousValue, resultingValue);
        if (capOutcome is not null)
        {
            return capOutcome;
        }

        if (resultingValue < TrainedValue)
        {
            RemoveSkill(request.Character, request.SkillName);
            return CreateResult(request, SkillAdjustmentOutcome.Applied, previousValue, UntrainedValue, "Skill is now untrained.");
        }

        var skill = GetOrCreateSkill(request.Character, request.SkillName);
        skill.IsTrained = true;
        skill.Value = resultingValue;
        skill.Specialties.Clear();
        return CreateResult(request, SkillAdjustmentOutcome.Applied, previousValue, resultingValue, string.Empty);
    }

    private static SkillAdjustmentResult ApplySpecialty(SkillAdjustmentRequest request)
    {
        if (request.SkillName == SkillName.Profession && string.IsNullOrWhiteSpace(request.SpecialtyId))
        {
            var previousValue = GetEffectiveValue(request.Character, request.SkillName);
            return CreateResult(request, SkillAdjustmentOutcome.Rejected, previousValue, previousValue, "Profession requires a Specialty.");
        }

        if (request.Operation == SkillAdjustmentOperation.Train)
        {
            if (request.SkillName == SkillName.Profession && !string.IsNullOrWhiteSpace(request.SpecialtyId))
            {
                return TrainProfessionSpecialty(request);
            }

            var previousValue = GetEffectiveValue(request.Character, request.SkillName);
            var trainedSkill = GetOrCreateSkill(request.Character, request.SkillName);
            trainedSkill.IsTrained = true;
            trainedSkill.Value = TrainedValue;
            return CreateResult(request, SkillAdjustmentOutcome.Applied, previousValue, TrainedValue, string.Empty);
        }

        if (string.IsNullOrWhiteSpace(request.SpecialtyId))
        {
            return ApplySpecialtyBaseAdjustment(request);
        }

        var previousSpecialtyValue = GetEffectiveSpecialtyValue(request.Character, request.SkillName, request.SpecialtyId);
        var resultingValue = GetResultingValue(previousSpecialtyValue, request.Operation, request.Value);
        var capOutcome = CheckCreationCap(request, previousSpecialtyValue, resultingValue);
        if (capOutcome is not null)
        {
            return capOutcome;
        }

        var skill = GetOrCreateSkill(request.Character, request.SkillName);
        skill.IsTrained = true;
        skill.Value = TrainedValue;

        if (request.SkillName == SkillName.Profession && resultingValue < TrainedValue)
        {
            skill.Specialties.RemoveAll(specialty =>
                string.Equals(specialty.SpecialtyId, request.SpecialtyId, StringComparison.OrdinalIgnoreCase));

            if (skill.Specialties.Count == 0)
            {
                RemoveSkill(request.Character, request.SkillName);
                return CreateResult(request, SkillAdjustmentOutcome.Applied, previousSpecialtyValue, UntrainedValue, "Skill is now untrained.");
            }

            return CreateResult(request, SkillAdjustmentOutcome.Applied, previousSpecialtyValue, UntrainedValue, "Specialty removed.");
        }

        if (request.SkillName != SkillName.Profession && resultingValue <= TrainedValue)
        {
            skill.Specialties.RemoveAll(specialty =>
                string.Equals(specialty.SpecialtyId, request.SpecialtyId, StringComparison.OrdinalIgnoreCase));

            if (skill.Specialties.Count == 0 && resultingValue < TrainedValue)
            {
                RemoveSkill(request.Character, request.SkillName);
                return CreateResult(request, SkillAdjustmentOutcome.Applied, previousSpecialtyValue, UntrainedValue, "Skill is now untrained.");
            }

            return CreateResult(request, SkillAdjustmentOutcome.Applied, previousSpecialtyValue, TrainedValue, "Specialty removed.");
        }

        var specialty = skill.Specialties.FirstOrDefault(item =>
            string.Equals(item.SpecialtyId, request.SpecialtyId, StringComparison.OrdinalIgnoreCase));
        if (specialty is null)
        {
            skill.Specialties.Add(new CharacterSkillSpecialty
            {
                SpecialtyId = request.SpecialtyId,
                Value = resultingValue
            });
        }
        else
        {
            specialty.Value = resultingValue;
        }

        return CreateResult(request, SkillAdjustmentOutcome.Applied, previousSpecialtyValue, resultingValue, string.Empty);
    }

    private static SkillAdjustmentResult TrainProfessionSpecialty(SkillAdjustmentRequest request)
    {
        var previousSpecialtyValue = GetEffectiveSpecialtyValue(request.Character, request.SkillName, request.SpecialtyId);
        var skill = GetOrCreateSkill(request.Character, request.SkillName);
        skill.IsTrained = true;
        skill.Value = TrainedValue;

        var specialty = skill.Specialties.FirstOrDefault(item =>
            string.Equals(item.SpecialtyId, request.SpecialtyId, StringComparison.OrdinalIgnoreCase));
        if (specialty is null)
        {
            skill.Specialties.Add(new CharacterSkillSpecialty
            {
                SpecialtyId = request.SpecialtyId,
                Value = TrainedValue
            });
        }
        else if (specialty.Value < TrainedValue)
        {
            specialty.Value = TrainedValue;
        }

        return CreateResult(request, SkillAdjustmentOutcome.Applied, previousSpecialtyValue, TrainedValue, string.Empty);
    }

    private static SkillAdjustmentResult ApplySpecialtyBaseAdjustment(SkillAdjustmentRequest request)
    {
        var previousValue = GetEffectiveValue(request.Character, request.SkillName);
        var resultingValue = GetResultingValue(previousValue, request.Operation, request.Value);

        if (resultingValue > TrainedValue)
        {
            return CreateResult(request, SkillAdjustmentOutcome.Rejected, previousValue, previousValue, "A Specialty is required.");
        }

        if (resultingValue < TrainedValue)
        {
            var existingSkill = FindSkill(request.Character, request.SkillName);
            if (existingSkill?.Specialties.Count > 0)
            {
                return CreateResult(request, SkillAdjustmentOutcome.Rejected, previousValue, previousValue, "Choose a Specialty to decrease.");
            }

            RemoveSkill(request.Character, request.SkillName);
            return CreateResult(request, SkillAdjustmentOutcome.Applied, previousValue, UntrainedValue, "Skill is now untrained.");
        }

        var skill = GetOrCreateSkill(request.Character, request.SkillName);
        skill.IsTrained = true;
        skill.Value = TrainedValue;
        return CreateResult(request, SkillAdjustmentOutcome.Applied, previousValue, TrainedValue, string.Empty);
    }

    private static SkillAdjustmentResult? CheckCreationCap(SkillAdjustmentRequest request, int previousValue, int resultingValue)
    {
        if (!request.EnforceCreationCap || resultingValue <= CharacterCreationMaximum)
        {
            return null;
        }

        var message = "This adjustment would exceed the character creation maximum of 4.";
        return request.ChoiceKind == SkillAdjustmentChoiceKind.FixedChoice
            ? CreateResult(request, SkillAdjustmentOutcome.Lost, previousValue, previousValue, "Fixed-choice adjustment lost. " + message)
            : CreateResult(request, SkillAdjustmentOutcome.Rejected, previousValue, previousValue, message);
    }

    private static int GetResultingValue(int previousValue, SkillAdjustmentOperation operation, int value)
    {
        return operation switch
        {
            SkillAdjustmentOperation.Train => TrainedValue,
            SkillAdjustmentOperation.Set => value,
            SkillAdjustmentOperation.Increment => previousValue < TrainedValue && value > 0
                ? TrainedValue + value - 1
                : previousValue + value,
            _ => previousValue
        };
    }

    private static bool WouldExceedCreationMaximum(
        Character character,
        SkillName skillName,
        string specialtyId,
        SkillAdjustmentOperation operation,
        int value)
    {
        var previousValue = string.IsNullOrWhiteSpace(specialtyId)
            ? GetEffectiveValue(character, skillName)
            : GetEffectiveSpecialtyValue(character, skillName, specialtyId);
        return GetResultingValue(previousValue, operation, value) > CharacterCreationMaximum;
    }

    private static bool IsSpecialtyEnabled(
        Character character,
        SkillName skillName,
        string specialtyId,
        SkillAdjustmentOperation operation,
        int value,
        SkillAdjustmentChoiceKind choiceKind,
        bool enforceCreationCap)
    {
        return !enforceCreationCap ||
               choiceKind == SkillAdjustmentChoiceKind.FixedChoice ||
               !WouldExceedCreationMaximum(character, skillName, specialtyId, operation, value);
    }

    private static string GetSpecialtyDisabledReason(
        Character character,
        SkillName skillName,
        string specialtyId,
        SkillAdjustmentOperation operation,
        int value,
        SkillAdjustmentChoiceKind choiceKind,
        bool enforceCreationCap)
    {
        return IsSpecialtyEnabled(character, skillName, specialtyId, operation, value, choiceKind, enforceCreationCap)
            ? string.Empty
            : "Already at the character creation maximum.";
    }

    private static CharacterSkill? FindSkill(Character character, SkillName skillName)
    {
        character.Skills ??= new();
        return character.Skills.Skills.FirstOrDefault(skill => skill.SkillName == skillName);
    }

    private static CharacterSkill GetOrCreateSkill(Character character, SkillName skillName)
    {
        character.Skills ??= new();
        var skill = FindSkill(character, skillName);
        if (skill is not null)
        {
            return skill;
        }

        skill = new CharacterSkill { SkillName = skillName };
        character.Skills.Skills.Add(skill);
        return skill;
    }

    private static void RemoveSkill(Character character, SkillName skillName)
    {
        character.Skills?.Skills.RemoveAll(skill => skill.SkillName == skillName);
    }

    private static SkillAdjustmentResult CreateResult(
        SkillAdjustmentRequest request,
        SkillAdjustmentOutcome outcome,
        int previousValue,
        int resultingValue,
        string message)
    {
        var historyRecord = new SkillHistoryRecord
        {
            Timestamp = DateTime.Now,
            Sequence = (request.Character.Skills?.History.Count ?? 0) + 1,
            Source = request.Source,
            Operation = request.Operation,
            ChoiceKind = request.ChoiceKind,
            Outcome = outcome,
            SkillName = request.SkillName,
            SpecialtyId = request.SpecialtyId,
            PreviousValue = previousValue,
            ResultingValue = resultingValue,
            Message = message
        };

        return new SkillAdjustmentResult(outcome, message, previousValue, resultingValue, historyRecord);
    }
}

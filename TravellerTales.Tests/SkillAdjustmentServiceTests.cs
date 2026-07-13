using TravellerTales.Models;
using TravellerTales.Services;
using Xunit;

namespace TravellerTales.Tests;

public sealed class SkillAdjustmentServiceTests
{
    [Fact]
    public void ValueOnlySkill_DefaultsToUntrained()
    {
        var character = new Character();

        Assert.Equal(SkillAdjustmentService.UntrainedValue, SkillAdjustmentService.GetEffectiveValue(character, SkillName.Admin));
    }

    [Fact]
    public void ValueOnlyIncrement_TrainsBeforeIncreasing()
    {
        var character = new Character();

        var firstGain = SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Admin,
            SkillAdjustmentOperation.Increment,
            1,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));
        var secondGain = SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Admin,
            SkillAdjustmentOperation.Increment,
            1,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));

        Assert.Equal(SkillAdjustmentOutcome.Applied, firstGain.Outcome);
        Assert.Equal(0, firstGain.ResultingValue);
        Assert.Equal(SkillAdjustmentOutcome.Applied, secondGain.Outcome);
        Assert.Equal(1, SkillAdjustmentService.GetEffectiveValue(character, SkillName.Admin));
    }

    [Fact]
    public void ValueOnlyDecreaseBelowZero_UntrainsSkill()
    {
        var character = new Character();
        SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Admin,
            SkillAdjustmentOperation.Train,
            0,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));

        SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Admin,
            SkillAdjustmentOperation.Increment,
            -1,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));

        Assert.Equal(SkillAdjustmentService.UntrainedValue, SkillAdjustmentService.GetEffectiveValue(character, SkillName.Admin));
        Assert.Empty(character.Skills.Skills);
    }

    [Fact]
    public void UserChoiceAboveCreationCap_IsRejectedAndDoesNotChangeState()
    {
        var character = new Character();
        SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Admin,
            SkillAdjustmentOperation.Set,
            4,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));

        var result = SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Admin,
            SkillAdjustmentOperation.Increment,
            1,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));

        Assert.Equal(SkillAdjustmentOutcome.Rejected, result.Outcome);
        Assert.Equal(4, SkillAdjustmentService.GetEffectiveValue(character, SkillName.Admin));
        Assert.Equal(2, character.Skills.History.Count);
    }

    [Fact]
    public void FixedChoiceAboveCreationCap_IsLostAndDoesNotChangeState()
    {
        var character = new Character();
        SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Admin,
            SkillAdjustmentOperation.Set,
            4,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));

        var result = SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Admin,
            SkillAdjustmentOperation.Increment,
            1,
            "Die Roll",
            SkillAdjustmentChoiceKind.FixedChoice));

        Assert.Equal(SkillAdjustmentOutcome.Lost, result.Outcome);
        Assert.Equal(4, SkillAdjustmentService.GetEffectiveValue(character, SkillName.Admin));
        Assert.Equal(SkillAdjustmentOutcome.Lost, character.Skills.History.Last().Outcome);
    }

    [Fact]
    public void SpecialtySetAboveZero_TrainsBaseAndSetsSpecialty()
    {
        var character = new Character();
        const string specialtyId = "GunCombat:slug";

        var result = SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.GunCombat,
            SkillAdjustmentOperation.Set,
            1,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice,
            SpecialtyId: specialtyId));

        Assert.Equal(SkillAdjustmentOutcome.Applied, result.Outcome);
        Assert.Equal(0, SkillAdjustmentService.GetEffectiveValue(character, SkillName.GunCombat));
        Assert.Equal(1, SkillAdjustmentService.GetEffectiveSpecialtyValue(character, SkillName.GunCombat, specialtyId));
    }

    [Fact]
    public void SpecialtyReducedToZero_IsRemovedButBaseStaysTrained()
    {
        var character = new Character();
        const string specialtyId = "GunCombat:slug";
        SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.GunCombat,
            SkillAdjustmentOperation.Set,
            1,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice,
            SpecialtyId: specialtyId));

        SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.GunCombat,
            SkillAdjustmentOperation.Increment,
            -1,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice,
            SpecialtyId: specialtyId));

        Assert.Equal(0, SkillAdjustmentService.GetEffectiveValue(character, SkillName.GunCombat));
        Assert.Equal(0, SkillAdjustmentService.GetEffectiveSpecialtyValue(character, SkillName.GunCombat, specialtyId));
        Assert.Empty(character.Skills.Skills.Single().Specialties);
    }

    [Fact]
    public void SpecialtyBaseDecreaseBelowZeroWithNoSpecialties_UntrainsBase()
    {
        var character = new Character();
        SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.GunCombat,
            SkillAdjustmentOperation.Train,
            0,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));

        SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.GunCombat,
            SkillAdjustmentOperation.Increment,
            -1,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));

        Assert.Equal(SkillAdjustmentService.UntrainedValue, SkillAdjustmentService.GetEffectiveValue(character, SkillName.GunCombat));
        Assert.Empty(character.Skills.Skills);
    }

    [Fact]
    public void CreationValidation_FindsValuesAboveCreationCap()
    {
        var character = new Character();
        SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Admin,
            SkillAdjustmentOperation.Set,
            5,
            "Post Creation",
            SkillAdjustmentChoiceKind.UserChoice,
            EnforceCreationCap: false));

        Assert.True(SkillAdjustmentService.HasCreationCapViolation(character));
    }

    [Fact]
    public void SpecialtyCatalog_NormalizesCustomNamesAndReusesIds()
    {
        var catalog = new SkillSpecialtyCatalog();

        var first = catalog.GetOrAddCustom(SkillName.Language, "  old   vilani ");
        var second = catalog.GetOrAddCustom(SkillName.Language, "Old Vilani");

        Assert.Equal("Old Vilani", first.DisplayName);
        Assert.Equal(first.Id, second.Id);
        Assert.Single(catalog.Specialties);
    }

    [Fact]
    public void SpecialtyCatalog_MergeSeedsCanonicalDefaultsAndRemovesObsoleteSeeds()
    {
        var catalog = new SkillSpecialtyCatalog
        {
            Specialties =
            [
                new()
                {
                    Id = SkillSpecialtyCatalogRules.CreateSeededId(SkillName.Drive, "Tracked"),
                    SkillName = SkillName.Drive,
                    DisplayName = "Tracked",
                    IsSeeded = true
                },
                new()
                {
                    Id = SkillSpecialtyCatalogRules.CreateCustomId(SkillName.Drive, "Tracked"),
                    SkillName = SkillName.Drive,
                    DisplayName = "Tracked",
                    IsSeeded = false
                }
            ]
        };

        SpecialtyCatalogService.MergeSeededSpecialties(catalog);

        var driveDefaults = catalog.GetForSkill(SkillName.Drive).Select(specialty => specialty.DisplayName).ToList();
        Assert.Contains("Track", driveDefaults);
        Assert.Contains("Walker", driveDefaults);
        Assert.DoesNotContain(catalog.Specialties, specialty => specialty.IsSeeded && specialty.DisplayName == "Tracked");
        Assert.Contains(catalog.Specialties, specialty => !specialty.IsSeeded && specialty.DisplayName == "Tracked");

        var scienceDefaults = catalog.GetForSkill(SkillName.Science).Select(specialty => specialty.DisplayName).ToList();
        Assert.Contains("Archaeology", scienceDefaults);
        Assert.Contains("Sophontology", scienceDefaults);
        Assert.Contains("Xenology", scienceDefaults);
        Assert.DoesNotContain("Life Sciences", scienceDefaults);
    }

    [Fact]
    public void SpecialtyOptions_DefaultSpecialtiesAreAlphabeticalWhenSelectedCountsMatch()
    {
        var character = new Character();
        var catalog = new SkillSpecialtyCatalog();
        SpecialtyCatalogService.MergeSeededSpecialties(catalog);

        var options = SkillAdjustmentService.BuildSpecialtyOptions(
            character,
            SkillName.Language,
            catalog,
            SkillAdjustmentOperation.Train,
            0,
            SkillAdjustmentChoiceKind.UserChoice);

        Assert.Equal(
            ["Galanglic", "Gvegh", "Trokh", "Vilani", "Zdetl"],
            options.Select(option => option.DisplayName).ToList());
    }

    [Fact]
    public void BackgroundSkillCount_UsesEducationModifierAndClampsAtZero()
    {
        var lowEducation = new Character();
        lowEducation.CurrentCharacteristics.Education = 0;
        var averageEducation = new Character();
        averageEducation.CurrentCharacteristics.Education = 8;
        var highEducation = new Character();
        highEducation.CurrentCharacteristics.Education = 15;

        Assert.Equal(0, BackgroundSkillService.GetRequiredSelectionCount(lowEducation));
        Assert.Equal(3, BackgroundSkillService.GetRequiredSelectionCount(averageEducation));
        Assert.Equal(6, BackgroundSkillService.GetRequiredSelectionCount(highEducation));
    }

    [Fact]
    public void BackgroundSkills_BlockDuplicateNonProfessionSkills()
    {
        var selections = new[]
        {
            new BackgroundSkillSelection { IsSelected = true, SkillName = SkillName.Admin },
            new BackgroundSkillSelection { IsSelected = true, SkillName = SkillName.Admin }
        };

        var errors = BackgroundSkillService.GetValidationErrors(selections);

        Assert.Contains(errors, error => error.Contains("Admin"));
    }

    [Fact]
    public void BackgroundSpecialtySkillWithoutSpecialty_CountsAsFilled()
    {
        var selection = new BackgroundSkillSelection { IsSelected = true, SkillName = SkillName.Pilot };

        Assert.True(BackgroundSkillService.IsFilled(selection));
    }

    [Fact]
    public void BackgroundProfessionWithoutSpecialty_IsNotFilled()
    {
        var selection = new BackgroundSkillSelection { IsSelected = true, SkillName = SkillName.Profession };

        Assert.False(BackgroundSkillService.IsFilled(selection));
    }

    [Fact]
    public void BackgroundPilotTraining_AppliesBaseSkillWithoutSpecialty()
    {
        var character = new Character();
        var state = new BackgroundSkillsState
        {
            Selections =
            [
                new() { IsSelected = true, SkillName = SkillName.Pilot }
            ]
        };

        var results = BackgroundSkillService.ApplySelections(character, state, new SkillSpecialtyCatalog());

        Assert.Single(results);
        Assert.Equal(SkillAdjustmentOutcome.Applied, results.Single().Outcome);
        var skill = Assert.Single(character.Skills.Skills);
        Assert.Equal(SkillName.Pilot, skill.SkillName);
        Assert.True(skill.IsTrained);
        Assert.Equal(0, skill.Value);
        Assert.Empty(skill.Specialties);
    }

    [Fact]
    public void BackgroundProfessionTraining_AppliesSelectedSpecialty()
    {
        var character = new Character();
        var catalog = new SkillSpecialtyCatalog();
        var specialty = catalog.GetOrAddCustom(SkillName.Profession, "Belter");
        var state = new BackgroundSkillsState
        {
            Selections =
            [
                new() { IsSelected = true, SkillName = SkillName.Profession, SpecialtyId = specialty.Id }
            ]
        };

        var results = BackgroundSkillService.ApplySelections(character, state, catalog);

        Assert.Single(results);
        Assert.Equal(SkillAdjustmentOutcome.Applied, results.Single().Outcome);
        var skill = Assert.Single(character.Skills.Skills);
        Assert.Equal(SkillName.Profession, skill.SkillName);
        var storedSpecialty = Assert.Single(skill.Specialties);
        Assert.Equal(specialty.Id, storedSpecialty.SpecialtyId);
        Assert.Equal(0, storedSpecialty.Value);
    }

    [Fact]
    public void BackgroundSkills_AllowProfessionWithDifferentSpecialties()
    {
        var state = new BackgroundSkillsState
        {
            Selections =
            [
                new() { IsSelected = true, SkillName = SkillName.Profession, SpecialtyId = "Profession:custom:store-clerk" },
                new() { IsSelected = true, SkillName = SkillName.Profession, SpecialtyId = "Profession:custom:aircraft-mechanic" }
            ]
        };

        Assert.Empty(BackgroundSkillService.GetValidationErrors(state.Selections));
    }

    [Fact]
    public void BackgroundSkills_BlockDuplicateProfessionSpecialty()
    {
        var state = new BackgroundSkillsState
        {
            Selections =
            [
                new() { IsSelected = true, SkillName = SkillName.Profession, SpecialtyId = "Profession:custom:store-clerk" },
                new() { IsSelected = true, SkillName = SkillName.Profession, SpecialtyId = "Profession:custom:store-clerk" }
            ]
        };

        var errors = BackgroundSkillService.GetValidationErrors(state.Selections);

        Assert.Contains(errors, error => error.Contains("Profession Specialty"));
    }

    [Fact]
    public void ProfessionTrainWithSpecialty_CreatesSpecialtyAtZero()
    {
        var character = new Character();
        const string specialtyId = "Profession:custom:store-clerk";

        var result = SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Profession,
            SkillAdjustmentOperation.Train,
            0,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice,
            SpecialtyId: specialtyId));

        Assert.Equal(SkillAdjustmentOutcome.Applied, result.Outcome);
        Assert.Equal(0, SkillAdjustmentService.GetEffectiveSpecialtyValue(character, SkillName.Profession, specialtyId));
        Assert.Single(character.Skills.Skills.Single().Specialties);
        Assert.Equal(0, character.Skills.Skills.Single().Specialties.Single().Value);
    }

    [Fact]
    public void ProfessionTrainWithoutSpecialty_IsRejected()
    {
        var character = new Character();

        var result = SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Profession,
            SkillAdjustmentOperation.Train,
            0,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice));

        Assert.Equal(SkillAdjustmentOutcome.Rejected, result.Outcome);
        Assert.Empty(character.Skills.Skills);
    }

    [Fact]
    public void ProfessionIncrementWithUntrainedSpecialty_TrainsThenIncrements()
    {
        var character = new Character();
        const string specialtyId = "Profession:custom:store-clerk";

        var result = SkillAdjustmentService.Apply(new SkillAdjustmentRequest(
            character,
            SkillName.Profession,
            SkillAdjustmentOperation.Increment,
            1,
            "Test",
            SkillAdjustmentChoiceKind.UserChoice,
            SpecialtyId: specialtyId));

        Assert.Equal(SkillAdjustmentOutcome.Applied, result.Outcome);
        Assert.Equal(0, SkillAdjustmentService.GetEffectiveSpecialtyValue(character, SkillName.Profession, specialtyId));
    }
}

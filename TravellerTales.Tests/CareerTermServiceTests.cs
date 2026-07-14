using TravellerTales.Models;
using TravellerTales.Services;
using Xunit;

namespace TravellerTales.Tests;

public sealed class CareerTermServiceTests
{
    [Fact]
    public void TermState_BlocksOutOfOrderSections()
    {
        var state = new CareerTermsState();
        CareerTermService.StartNextTerm(state);

        Assert.Throws<InvalidOperationException>(() =>
            CareerTermService.SelectAssignment(state, "Line/Crew"));
    }

    [Fact]
    public void RollRecord_PreservesNaturalDiceAndModifiedTotal()
    {
        var roll = CareerTermService.CreateRoll(6, 6, -2);

        Assert.Equal(12, roll.NaturalTotal);
        Assert.Equal(10, roll.ModifiedTotal);
        Assert.True(roll.IsNatural12);
    }

    [Fact]
    public void MishapResolution_ForcesCareerExit()
    {
        var state = new CareerTermsState
        {
            ActiveTerm = new CareerTermProgress
            {
                CurrentSubStep = CareerTermSubStep.Mishap
            }
        };

        CareerTermService.ResolveMishap(state);

        Assert.True(state.ActiveTerm!.ForceCareerEnd);
        Assert.Equal(CareerTermSubStep.Leaving, state.ActiveTerm.CurrentSubStep);
    }

    [Fact]
    public void AdvancementRule_ForcedExitHonorsNatural12Exception()
    {
        var lowRoll = CareerTermService.CreateRoll(1, 2);
        var naturalTwelve = CareerTermService.CreateRoll(6, 6, -20);

        Assert.True(CareerTermService.WouldAdvancementRollForceCareerExit(lowRoll, careerTermNumber: 3));
        Assert.False(CareerTermService.WouldAdvancementRollForceCareerExit(naturalTwelve, careerTermNumber: 3));
    }

    [Fact]
    public void CashBenefits_AreCappedAcrossCareers()
    {
        var state = new CareerTermsState
        {
            TotalCashBenefits = CareerTermService.MaximumCashBenefits,
            ActiveTerm = new CareerTermProgress
            {
                CurrentSubStep = CareerTermSubStep.MusteringOut
            }
        };

        CareerTermService.ResolveMusteringOut(state, CareerBenefitKind.Cash);

        Assert.Equal(CareerTermService.MaximumCashBenefits, state.TotalCashBenefits);
        Assert.Contains("Material Benefit", state.ActiveTerm!.MusteringOutSummary);
    }

    [Fact]
    public void SaveGate_AllowsOnlyBetweenTerms()
    {
        var state = new CareerTermsState();

        Assert.True(CareerTermService.CanSave(state));

        CareerTermService.StartNextTerm(state);

        Assert.False(CareerTermService.CanSave(state));
    }

    [Fact]
    public void CareerTable_LoadsAllCareersWithThreeAssignments()
    {
        Assert.Equal(12, CareerTermService.Careers.Count);
        Assert.All(CareerTermService.Careers, career => Assert.Equal(3, career.Assignments.Count));
    }

    [Fact]
    public void CareerSelection_BlocksPreviouslyEnteredNonDrifterCareer()
    {
        var state = new CareerTermsState
        {
            CareerHistory =
            [
                new() { Career = "Agent", EntryCount = 1 }
            ]
        };

        var available = CareerTermService.GetAvailableCareerNames(state);

        Assert.DoesNotContain("Agent", available);
        Assert.Contains("Drifter", available);

        CareerTermService.StartNextTerm(state);
        Assert.Throws<InvalidOperationException>(() => CareerTermService.SelectCareer(state, "Agent"));
    }

    [Fact]
    public void DirectedEntry_AllowsPreviouslyEnteredCareerAndContinuesOldRank()
    {
        var state = new CareerTermsState
        {
            CareerHistory =
            [
                new() { Career = "Agent", EntryCount = 1, Rank = 2, Terms = 1 }
            ]
        };
        CareerTermService.StartNextTerm(state);

        CareerTermService.SelectCareer(state, "Agent", allowDirectedEntry: true);

        Assert.Equal(2, state.ActiveTerm!.StartingRank);
        Assert.Equal(2, state.ActiveTerm.EndingRank);
        Assert.Equal(2, state.ActiveTerm.CareerTermNumber);
    }

    [Fact]
    public void MultiCharacteristicRequirement_UsesBestModifier()
    {
        var character = new Character();
        character.CurrentCharacteristics.Dexterity = 15;
        character.CurrentCharacteristics.Intellect = 3;
        var requirement = CareerTermService.FindCareer("Entertainer").Qualification;

        var context = CareerTermService.BuildRollContext(character, requirement);

        Assert.Equal("DEX", context.CharacteristicCode);
        Assert.Equal(3, context.Modifier);
    }

    [Fact]
    public void Draft_CanOnlyBeUsedOnce()
    {
        var state = new CareerTermsState
        {
            DraftUsed = true,
            ActiveTerm = new CareerTermProgress
            {
                CurrentSubStep = CareerTermSubStep.QualificationChoice,
                QualificationFailed = true
            }
        };

        Assert.Throws<InvalidOperationException>(() => CareerTermService.SubmitToDraft(state));
    }

    [Fact]
    public void DraftMapping_MatchesCareerAndAssignmentRules()
    {
        Assert.Equal("Navy", CareerTermService.ResolveDraft(1).Career);
        Assert.Equal("Army", CareerTermService.ResolveDraft(2).Career);
        Assert.Equal("Marine", CareerTermService.ResolveDraft(3).Career);

        var merchant = CareerTermService.ResolveDraft(4);
        Assert.Equal("Merchant", merchant.Career);
        Assert.Equal("Merchant Marine", merchant.ForcedAssignment);

        Assert.Equal("Scout", CareerTermService.ResolveDraft(5).Career);

        var agent = CareerTermService.ResolveDraft(6);
        Assert.Equal("Agent", agent.Career);
        Assert.Equal("Law Enforcement", agent.ForcedAssignment);
    }
}

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public partial class CareerTermDialogWindow : Window
{
    private readonly CharacterCreationState _state;
    private bool _allowClose;
    private bool _isRendering;

    public CareerTermDialogWindow(CharacterCreationState state)
    {
        _state = state;
        InitializeComponent();

        CareerComboBox.ItemsSource = CareerTermService.GetAvailableCareerNames(_state.CareerTerms);
        CareerComboBox.SelectedItem = string.IsNullOrWhiteSpace(_state.CareerTerms.ActiveTerm?.Career)
            ? CareerTermService.GetAvailableCareerNames(_state.CareerTerms).FirstOrDefault()
            : _state.CareerTerms.ActiveTerm.Career;
        RefreshAssignmentOptions();

        Render();
    }

    public event EventHandler<CharacterCreationCancelEventArgs>? CancelCharacterCreationRequested;

    private CareerTermProgress Term => _state.CareerTerms.ActiveTerm ??
                                       throw new InvalidOperationException("No active Career Term exists.");

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        ValidationText.Text = "A Career Term must be completed before closing this window.";
    }

    private void OnSelectCareer(object sender, RoutedEventArgs e)
    {
        if (CareerComboBox.SelectedItem is string career)
        {
            try
            {
                CareerTermService.SelectCareer(_state.CareerTerms, career);
            }
            catch (InvalidOperationException exception)
            {
                ValidationText.Text = exception.Message;
                return;
            }

            RefreshAssignmentOptions();
            Render();
        }
    }

    private void OnRollQualification(object sender, RoutedEventArgs e)
    {
        CareerTermService.RollQualification(_state.Character, _state.CareerTerms);
        RefreshAssignmentOptions();
        Render();
    }

    private void OnSubmitToDraft(object sender, RoutedEventArgs e)
    {
        try
        {
            CareerTermService.SubmitToDraft(_state.CareerTerms);
        }
        catch (InvalidOperationException exception)
        {
            ValidationText.Text = exception.Message;
            return;
        }

        RefreshAssignmentOptions();
        Render();
    }

    private void OnEnterDrifter(object sender, RoutedEventArgs e)
    {
        CareerTermService.EnterDrifterAfterFailedQualification(_state.CareerTerms);
        CareerComboBox.ItemsSource = CareerTermService.GetAvailableCareerNames(_state.CareerTerms);
        CareerComboBox.SelectedItem = Term.Career;
        RefreshAssignmentOptions();
        Render();
    }

    private void OnSelectAssignment(object sender, RoutedEventArgs e)
    {
        if (AssignmentComboBox.SelectedItem is string assignment)
        {
            try
            {
                CareerTermService.SelectAssignment(_state.CareerTerms, assignment);
            }
            catch (InvalidOperationException exception)
            {
                ValidationText.Text = exception.Message;
                return;
            }

            Render();
        }
    }

    private void OnResolveTraining(object sender, RoutedEventArgs e)
    {
        CareerTermService.ResolveTraining(_state.CareerTerms);
        Render();
    }

    private void OnRollSurvival(object sender, RoutedEventArgs e)
    {
        CareerTermService.RollSurvival(_state.Character, _state.CareerTerms);
        Render();
    }

    private void OnResolveMishap(object sender, RoutedEventArgs e)
    {
        CareerTermService.ResolveMishap(_state.CareerTerms);
        Render();
    }

    private void OnRollEvent(object sender, RoutedEventArgs e)
    {
        CareerTermService.RollEvent(_state.CareerTerms);
        Render();
    }

    private void OnRollAdvancement(object sender, RoutedEventArgs e)
    {
        CareerTermService.RollCommissionAndAdvancement(_state.Character, _state.CareerTerms);
        Render();
    }

    private void OnContinueCareer(object sender, RoutedEventArgs e)
    {
        CareerTermService.ResolveLeaving(_state.CareerTerms, leaveCareer: false);
        Render();
    }

    private void OnLeaveCareer(object sender, RoutedEventArgs e)
    {
        CareerTermService.ResolveLeaving(_state.CareerTerms, leaveCareer: true);
        Render();
    }

    private void OnCashBenefit(object sender, RoutedEventArgs e)
    {
        CareerTermService.ResolveMusteringOut(_state.CareerTerms, CareerBenefitKind.Cash);
        Render();
    }

    private void OnMaterialBenefit(object sender, RoutedEventArgs e)
    {
        CareerTermService.ResolveMusteringOut(_state.CareerTerms, CareerBenefitKind.Material);
        Render();
    }

    private void OnResolveAging(object sender, RoutedEventArgs e)
    {
        CareerTermService.ResolveAging(_state.Character, _state.CareerTerms);
        Render();
    }

    private void OnCompleteTerm(object sender, RoutedEventArgs e)
    {
        CareerTermService.CompleteActiveTerm(_state.CareerTerms);
        _allowClose = true;
        DialogResult = true;
        Close();
    }

    private void OnCancelCharacterCreation(object sender, RoutedEventArgs e)
    {
        var args = new CharacterCreationCancelEventArgs();
        CancelCharacterCreationRequested?.Invoke(this, args);
        if (!args.Cancelled)
        {
            return;
        }

        _allowClose = true;
        DialogResult = false;
        Close();
    }

    private void OnCareerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRendering)
        {
            return;
        }

        RefreshCareerPreview();
    }

    private void OnAssignmentSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRendering)
        {
            return;
        }

        RefreshAssignmentPreview();
    }

    private void Render()
    {
        _isRendering = true;
        var term = Term;
        StepText.Text = $"Term {term.Sequence} | {term.CurrentSubStep}";
        TitleText.Text = $"CAREER TERM {term.Sequence}";

        SetSection(CareerSection, true);
        SetSection(AssignmentSection, term.CurrentSubStep >= CareerTermSubStep.Assignment);
        SetSection(TrainingSection, term.CurrentSubStep >= CareerTermSubStep.Training);
        SetSection(SurvivalSection, term.CurrentSubStep >= CareerTermSubStep.Survival);
        SetSection(MishapSection, term.CurrentSubStep == CareerTermSubStep.Mishap || term.MishapResolved);
        SetSection(EventSection, term.CurrentSubStep >= CareerTermSubStep.Event && !term.MishapResolved);
        SetSection(AdvancementSection, term.CurrentSubStep >= CareerTermSubStep.CommissionAdvancement && !term.MishapResolved);
        SetSection(LeavingSection, term.CurrentSubStep >= CareerTermSubStep.Leaving);
        SetSection(MusteringSection, term.CurrentSubStep >= CareerTermSubStep.MusteringOut);
        SetSection(AgingSection, term.CurrentSubStep >= CareerTermSubStep.Aging);

        CareerComboBox.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Career && term.QualificationRoll is null;
        CareerButton.IsEnabled = CareerComboBox.IsEnabled;
        QualificationButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Career &&
                                        !term.Qualified &&
                                        !string.IsNullOrWhiteSpace(term.Career);
        DraftButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.QualificationChoice && !term.EnteredByDraft && !_state.CareerTerms.DraftUsed;
        DrifterButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.QualificationChoice;

        AssignmentButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Assignment;
        AssignmentComboBox.IsEnabled = AssignmentButton.IsEnabled;
        TrainingButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Training;
        SurvivalButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Survival;
        MishapButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Mishap;
        EventButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Event;
        AdvancementButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.CommissionAdvancement;
        ContinueCareerButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Leaving && !term.ForceCareerEnd;
        LeaveCareerButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Leaving;
        CashBenefitButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.MusteringOut &&
                                      _state.CareerTerms.TotalCashBenefits < CareerTermService.MaximumCashBenefits;
        MaterialBenefitButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.MusteringOut;
        AgingButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Aging;
        CompleteButton.IsEnabled = term.CurrentSubStep == CareerTermSubStep.Complete;

        RefreshCareerPreview();
        RefreshAssignmentPreview();
        CareerResultText.Text = BuildCareerText(term);
        AssignmentResultText.Text = string.IsNullOrWhiteSpace(term.Assignment)
            ? "Select an Assignment after Career qualification is resolved."
            : $"Assignment selected: {term.Assignment}.";
        TrainingTitleText.Text = term.CareerTermNumber == 1 ? "Basic Training" : "Skill";
        TrainingResultText.Text = term.TrainingResolved
            ? "Training placeholder resolved. Future tables will apply Basic Training or a Skill choice here."
            : term.CareerTermNumber == 1
                ? "First Term in this Career: Basic Training placeholder pending."
                : "Continuing Career: Skill training placeholder pending.";
        SurvivalResultText.Text = string.IsNullOrWhiteSpace(term.SurvivalSummary)
            ? "Roll Survival when this section unlocks."
            : term.SurvivalSummary;
        MishapResultText.Text = string.IsNullOrWhiteSpace(term.MishapSummary)
            ? "A failed Survival roll unlocks Mishap resolution."
            : term.MishapSummary;
        EventResultText.Text = string.IsNullOrWhiteSpace(term.EventSummary)
            ? "Roll Event when this section unlocks."
            : term.EventSummary;
        AdvancementResultText.Text =
            $"{ValueOrPending(term.CommissionSummary, "Commission pending.")}\n" +
            $"{ValueOrPending(term.AdvancementSummary, "Advancement pending.")}";
        LeavingResultText.Text = BuildLeavingText(term);
        MusteringResultText.Text = string.IsNullOrWhiteSpace(term.MusteringOutSummary)
            ? $"Choose a Cash Benefit or Material Benefit. Cash Benefits used: {_state.CareerTerms.TotalCashBenefits}/{CareerTermService.MaximumCashBenefits}."
            : term.MusteringOutSummary;
        AgingResultText.Text = string.IsNullOrWhiteSpace(term.AgingSummary)
            ? "Aging is resolved after the career decision and any Mustering Out."
            : term.AgingSummary;

        ValidationText.Text = term.CurrentSubStep == CareerTermSubStep.Complete
            ? "Term complete. Commit it to return to Career Terms."
            : "Resolve the highlighted available action to continue.";
        _isRendering = false;
    }

    private void RefreshAssignmentOptions()
    {
        var term = Term;
        if (string.IsNullOrWhiteSpace(term.Career))
        {
            AssignmentComboBox.ItemsSource = null;
            AssignmentComboBox.SelectedItem = null;
            return;
        }

        IReadOnlyList<string> assignments = string.IsNullOrWhiteSpace(term.ForcedAssignment)
            ? CareerTermService.GetAssignmentNames(term.Career)
            : [term.ForcedAssignment];
        AssignmentComboBox.ItemsSource = assignments;
        AssignmentComboBox.SelectedItem = !string.IsNullOrWhiteSpace(term.Assignment)
            ? term.Assignment
            : assignments.FirstOrDefault();
    }

    private void RefreshCareerPreview()
    {
        if (CareerComboBox.SelectedItem is not string careerName)
        {
            CareerDetailText.Text = "Select a Career to see qualification and assignment options.";
            return;
        }

        var career = CareerTermService.FindCareer(careerName);
        var qualification = FormatRequirementPreview(career.Qualification);
        var assignments = string.Join(", ", career.Assignments.Select(assignment => assignment.Name));
        CareerDetailText.Text =
            $"Qualification: {qualification}\n" +
            $"Assignments: {assignments}";
    }

    private void RefreshAssignmentPreview()
    {
        var careerName = !string.IsNullOrWhiteSpace(Term.Career)
            ? Term.Career
            : CareerComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(careerName) || AssignmentComboBox.SelectedItem is not string assignmentName)
        {
            AssignmentDetailText.Text = "Select an Assignment to see Survival and Advancement rolls.";
            return;
        }

        var assignment = CareerTermService.FindAssignment(careerName, assignmentName);
        AssignmentDetailText.Text =
            $"Survival: {FormatRequirementPreview(assignment.Survival)}\n" +
            $"Advancement: {FormatRequirementPreview(assignment.Advancement)}";
    }

    private static void SetSection(Border section, bool isEnabled)
    {
        section.IsEnabled = isEnabled;
        section.BorderBrush = (Brush)new BrushConverter().ConvertFromString(isEnabled ? "#5FD9FF" : "#31596D")!;
        section.Opacity = isEnabled ? 1.0 : 0.58;
    }

    private static string BuildCareerText(CareerTermProgress term)
    {
        if (string.IsNullOrWhiteSpace(term.Career))
        {
            return "Select a Career.";
        }

        if (term.QualificationRoll is null && !term.Qualified)
        {
            return $"Career selected: {term.Career}. Roll Qualification.";
        }

        if (!string.IsNullOrWhiteSpace(term.DraftSummary))
        {
            return term.DraftSummary;
        }

        if (!string.IsNullOrWhiteSpace(term.QualificationSummary))
        {
            return term.QualificationSummary;
        }

        if (term.QualificationRoll is null)
        {
            return $"Continuing Career: {term.Career}. Qualification not required.";
        }

        return term.Career == "Drifter"
            ? $"Qualification failed on {FormatRoll(term.QualificationRoll)}; placeholder fallback Career is Drifter."
            : $"Qualification succeeded on {FormatRoll(term.QualificationRoll)}.";
    }

    private static string BuildLeavingText(CareerTermProgress term)
    {
        if (term.CurrentSubStep < CareerTermSubStep.Leaving)
        {
            return "Career decision unlocks after Mishap or Advancement resolution.";
        }

        if (term.ForceCareerEnd)
        {
            return "Career exit is forced by this Term's result.";
        }

        return "Choose whether to continue this Career or muster out.";
    }

    private static string ValueOrPending(string value, string pending)
    {
        return string.IsNullOrWhiteSpace(value) ? pending : value;
    }

    private static string FormatRoll(TermRollRecord roll)
    {
        return $"{roll.Die1}+{roll.Die2}={roll.NaturalTotal}, total {roll.ModifiedTotal}";
    }

    private string FormatRequirementPreview(TermRollRequirement requirement)
    {
        if (requirement.IsAutomatic)
        {
            return "Automatic";
        }

        var context = CareerTermService.BuildRollContext(_state.Character, requirement);
        var options = string.Join(", ", requirement.CharacteristicCodes.Select(code =>
            $"{code} {FormatSigned(GetModifierForDisplay(code))}"));
        return $"{requirement.DisplayText} | current modifiers: {options} | using {context.CharacteristicCode} {FormatSigned(context.Modifier)}";
    }

    private int GetModifierForDisplay(string code)
    {
        var value = code.ToUpperInvariant() switch
        {
            "STR" => _state.Character.CurrentCharacteristics.Strength,
            "DEX" => _state.Character.CurrentCharacteristics.Dexterity,
            "END" => _state.Character.CurrentCharacteristics.Endurance,
            "INT" => _state.Character.CurrentCharacteristics.Intellect,
            "EDU" => _state.Character.CurrentCharacteristics.Education,
            "SOC" => _state.Character.CurrentCharacteristics.Social,
            _ => 0
        };

        return CharacteristicRules.GetDiceModifier(value);
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }
}

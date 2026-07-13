using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public partial class SkillSelectorDialogWindow : Window
{
    private readonly SkillSelectorRequest _request;
    private readonly SkillSpecialtyCatalog _catalog;
    private readonly Dictionary<SkillName, string> _selectedSpecialtyIds = [];

    public SkillSelectorDialogWindow(SkillSelectorRequest request, SkillSpecialtyCatalog catalog)
    {
        _request = request;
        _catalog = catalog;

        InitializeComponent();

        ActionText.Text = BuildActionText(_request);
        SkillList.SelectionMode = _request.AllowMultiple ? SelectionMode.Multiple : SelectionMode.Single;
        SkillList.ItemsSource = SkillAdjustmentService.BuildSkillOptions(
            _request.Character,
            _request.AllowedSkills,
            _request.Operation,
            _request.Value,
            _request.ChoiceKind,
            _request.EnforceCreationCap);

        SpecialtyPromptText.Text = "Select a specialty for specialty-based skills. Text entered below takes priority.";
        RefreshSpecialtyPanel();
    }

    public SkillSelectorResult? Result { get; private set; }

    private void OnSkillSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshSpecialtyPanel();
    }

    private void OnSpecialtySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GetCurrentSelectedSkill() is not { } skillName ||
            SpecialtyComboBox.SelectedItem is not SpecialtySelectionOption option)
        {
            return;
        }

        _selectedSpecialtyIds[skillName] = option.SpecialtyId;
    }

    private void OnCustomSpecialtyChanged(object sender, TextChangedEventArgs e)
    {
        if (GetCurrentSelectedSkill() is { } skillName)
        {
            _selectedSpecialtyIds.Remove(skillName);
        }
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;

        var selectedOptions = SkillList.SelectedItems
            .Cast<SkillSelectionOption>()
            .Where(option => option.IsEnabled)
            .ToList();

        if (selectedOptions.Count == 0)
        {
            ValidationText.Text = "Select a Skill.";
            return;
        }

        var targets = new List<SelectedSkillTarget>();
        foreach (var selectedOption in selectedOptions)
        {
            if (!RequiresSpecialty(selectedOption.SkillName))
            {
                targets.Add(new SelectedSkillTarget(selectedOption.SkillName, string.Empty));
                continue;
            }

            var specialtyId = ResolveSpecialtyId(selectedOption.SkillName);
            if (string.IsNullOrWhiteSpace(specialtyId))
            {
                ValidationText.Text = $"Select or enter a Specialty for {selectedOption.DisplayName}.";
                return;
            }

            if (SpecialtyComboBox.SelectedItem is SpecialtySelectionOption option && !option.IsEnabled)
            {
                ValidationText.Text = option.DisabledReason;
                return;
            }

            targets.Add(new SelectedSkillTarget(selectedOption.SkillName, specialtyId));
        }

        Result = new SkillSelectorResult(targets);
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        e.Handled = true;
        DialogResult = false;
        Close();
    }

    private void RefreshSpecialtyPanel()
    {
        var skillName = GetCurrentSelectedSkill();
        var requiresSpecialty = skillName.HasValue && RequiresSpecialty(skillName.Value);

        SpecialtyComboBox.IsEnabled = requiresSpecialty;
        CustomSpecialtyTextBox.IsEnabled = requiresSpecialty;
        SpecialtyComboBox.Visibility = requiresSpecialty ? Visibility.Visible : Visibility.Collapsed;
        CustomSpecialtyTextBox.Visibility = requiresSpecialty ? Visibility.Visible : Visibility.Collapsed;
        SpecialtyComboBox.ItemsSource = null;
        CustomSpecialtyTextBox.Text = string.Empty;

        if (!requiresSpecialty || skillName is null)
        {
            SpecialtyPromptText.Text = "No Specialty is needed for the current Skill selection.";
            return;
        }

        SpecialtyPromptText.Text = $"Choose a {SkillCatalog.GetDisplayName(skillName.Value)} Specialty.";
        var options = SkillAdjustmentService.BuildSpecialtyOptions(
            _request.Character,
            skillName.Value,
            _catalog,
            _request.Operation,
            _request.Value,
            _request.ChoiceKind,
            _request.EnforceCreationCap);

        SpecialtyComboBox.ItemsSource = options;
        if (_selectedSpecialtyIds.TryGetValue(skillName.Value, out var selectedSpecialtyId))
        {
            SpecialtyComboBox.SelectedItem = options.FirstOrDefault(option =>
                string.Equals(option.SpecialtyId, selectedSpecialtyId, StringComparison.OrdinalIgnoreCase));
        }
    }

    private SkillName? GetCurrentSelectedSkill()
    {
        if (SkillList.SelectedItem is SkillSelectionOption option)
        {
            return option.SkillName;
        }

        return null;
    }

    private bool RequiresSpecialty(SkillName skillName)
    {
        if (!SkillCatalog.IsSpecialtiesOnly(skillName))
        {
            return false;
        }

        if (_request.Operation != SkillAdjustmentOperation.Train)
        {
            return true;
        }

        return _request.RequireSpecialtyForTraining || skillName == SkillName.Profession;
    }

    private static string BuildActionText(SkillSelectorRequest request)
    {
        return request.Operation switch
        {
            SkillAdjustmentOperation.Train => "Action: Train selected Skill",
            SkillAdjustmentOperation.Increment when request.Value >= 0 =>
                $"Action: Increase selected Skill by +{request.Value.ToString(CultureInfo.InvariantCulture)}",
            SkillAdjustmentOperation.Increment =>
                $"Action: Decrease selected Skill by {request.Value.ToString(CultureInfo.InvariantCulture)}",
            SkillAdjustmentOperation.Set =>
                $"Action: Set selected Skill to {request.Value.ToString(CultureInfo.InvariantCulture)}",
            _ => "Action: Adjust selected Skill"
        };
    }

    private string ResolveSpecialtyId(SkillName skillName)
    {
        var customName = CustomSpecialtyTextBox.Text;
        if (!string.IsNullOrWhiteSpace(customName))
        {
            var specialty = _catalog.GetOrAddCustom(skillName, customName);
            return specialty.Id;
        }

        return _selectedSpecialtyIds.TryGetValue(skillName, out var selectedSpecialtyId)
            ? selectedSpecialtyId
            : string.Empty;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public partial class BackgroundSkillsStepControl : UserControl
{
    private readonly Brush _slotBorderBrush = (Brush)new BrushConverter().ConvertFromString("#5FD9FF")!;
    private readonly Brush _emptyBorderBrush = (Brush)new BrushConverter().ConvertFromString("#31596D")!;
    private readonly Brush _slotBackgroundBrush = (Brush)new BrushConverter().ConvertFromString("#99071523")!;
    private readonly Brush _slotTextBrush = (Brush)new BrushConverter().ConvertFromString("#EAFBFF")!;
    private readonly Brush _mutedTextBrush = (Brush)new BrushConverter().ConvertFromString("#7897A8")!;

    private CharacterCreationState _state = new();
    private SkillSpecialtyCatalog _catalog = new();

    public BackgroundSkillsStepControl()
    {
        InitializeComponent();
    }

    public event EventHandler? ValidityChanged;

    public void LoadState(CharacterCreationState state)
    {
        _state = state;
        _state.BackgroundSkills ??= new();
        _catalog = SpecialtyCatalogService.LoadCatalog();

        var requiredCount = BackgroundSkillService.GetRequiredSelectionCount(_state.Character);
        BackgroundSkillService.EnsureSlotCount(_state.BackgroundSkills, requiredCount);
        Render();
    }

    public bool IsComplete()
    {
        return BackgroundSkillService.IsComplete(_state.Character, _state.BackgroundSkills);
    }

    public bool TryCommitRequired()
    {
        ValidationText.Text = string.Empty;

        if (!IsComplete())
        {
            ValidationText.Text = "Choose every required Background Skill before continuing.";
            return false;
        }

        var errors = BackgroundSkillService.GetValidationErrors(_state.BackgroundSkills.Selections);
        if (errors.Count > 0)
        {
            ValidationText.Text = string.Join(" ", errors);
            return false;
        }

        var results = BackgroundSkillService.ApplySelections(_state.Character, _state.BackgroundSkills, _catalog);
        var failedResult = results.FirstOrDefault(result => !result.Applied);
        if (failedResult is not null)
        {
            ValidationText.Text = string.IsNullOrWhiteSpace(failedResult.Message)
                ? "The Background Skill selection could not be applied."
                : failedResult.Message;
            return false;
        }

        SpecialtyCatalogService.SaveCatalog(_catalog);
        return true;
    }

    private void Render()
    {
        SlotsPanel.Children.Clear();
        ValidationText.Text = string.Empty;

        var requiredCount = BackgroundSkillService.GetRequiredSelectionCount(_state.Character);
        var eduModifier = CharacteristicRules.GetDiceModifier(_state.Character.CurrentCharacteristics.Education);
        SummaryText.Text =
            $"Education modifier {FormatSigned(eduModifier)} grants {requiredCount} Background Skill selection(s).\n" +
            "Each selection trains one Skill; Profession may be selected more than once with different Specialties.";

        if (requiredCount == 0)
        {
            SlotsPanel.Children.Add(new TextBlock
            {
                Text = "No Background Skill selections are required.",
                Style = (Style)FindResource("BodyTextStyle"),
                FontSize = 24,
                Foreground = _slotTextBrush,
                TextWrapping = TextWrapping.Wrap
            });

            ValidityChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        for (var index = 0; index < _state.BackgroundSkills.Selections.Count; index++)
        {
            SlotsPanel.Children.Add(BuildSlotRow(index, _state.BackgroundSkills.Selections[index]));
        }

        ValidityChanged?.Invoke(this, EventArgs.Empty);
    }

    private UIElement BuildSlotRow(int index, BackgroundSkillSelection selection)
    {
        var row = new Grid
        {
            Margin = new Thickness(0, 0, 0, 12)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var border = new Border
        {
            Background = _slotBackgroundBrush,
            BorderBrush = BackgroundSkillService.IsFilled(selection) ? _slotBorderBrush : _emptyBorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18, 12, 18, 12)
        };
        Grid.SetColumnSpan(border, 4);
        row.Children.Add(border);

        var slotText = new TextBlock
        {
            Text = $"Slot {index + 1}",
            Style = (Style)FindResource("SectionHeaderTextStyle"),
            FontSize = 22,
            Foreground = BackgroundSkillService.IsFilled(selection) ? _slotTextBrush : _mutedTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(18, 0, 0, 0)
        };
        row.Children.Add(slotText);

        var valueText = new TextBlock
        {
            Text = FormatSelection(selection),
            Style = (Style)FindResource("DataFieldTextStyle"),
            FontSize = 21,
            Foreground = BackgroundSkillService.IsFilled(selection) ? _slotTextBrush : _mutedTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 14, 0)
        };
        Grid.SetColumn(valueText, 1);
        row.Children.Add(valueText);

        var chooseButton = new Button
        {
            Content = BackgroundSkillService.IsFilled(selection) ? "Change" : "Choose",
            Style = (Style)FindResource("BackgroundSkillButtonStyle"),
            Tag = index
        };
        chooseButton.Click += OnChooseSlot;
        Grid.SetColumn(chooseButton, 2);
        row.Children.Add(chooseButton);

        var clearButton = new Button
        {
            Content = "Clear",
            Style = (Style)FindResource("BackgroundSkillButtonStyle"),
            Tag = index,
            IsEnabled = BackgroundSkillService.IsFilled(selection)
        };
        clearButton.Click += OnClearSlot;
        Grid.SetColumn(clearButton, 3);
        row.Children.Add(clearButton);

        return row;
    }

    private void OnChooseSlot(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int slotIndex })
        {
            return;
        }

        var owner = Window.GetWindow(this);
        if (owner is null)
        {
            return;
        }

        var allowedSkills = BackgroundSkillService.GetAllowedSkillsForSlot(
            _state.BackgroundSkills.Selections,
            slotIndex);
        var result = SkillSelectorDialog.Show(owner, new SkillSelectorRequest(
            _state.Character,
            allowedSkills,
            SkillAdjustmentOperation.Train,
            0,
            SkillAdjustmentChoiceKind.UserChoice,
            RequireSpecialtyForTraining: false));

        var target = result?.Targets.FirstOrDefault();
        if (target is null)
        {
            return;
        }

        if (target.SkillName == SkillName.Profession &&
            _state.BackgroundSkills.Selections
                .Where((_, index) => index != slotIndex)
                .Any(selection => BackgroundSkillService.IsFilled(selection) &&
                                  selection.SkillName == SkillName.Profession &&
                                  string.Equals(selection.SpecialtyId, target.SpecialtyId, StringComparison.OrdinalIgnoreCase)))
        {
            ValidationText.Text = "Each Profession Specialty can only be selected once.";
            return;
        }

        _state.BackgroundSkills.Selections[slotIndex] = new BackgroundSkillSelection
        {
            IsSelected = true,
            SkillName = target.SkillName,
            SpecialtyId = target.SpecialtyId
        };
        _state.BackgroundSkills.Applied = false;
        Render();
    }

    private void OnClearSlot(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int slotIndex })
        {
            return;
        }

        _state.BackgroundSkills.Selections[slotIndex] = new BackgroundSkillSelection();
        _state.BackgroundSkills.Applied = false;
        Render();
    }

    private string FormatSelection(BackgroundSkillSelection selection)
    {
        if (!BackgroundSkillService.IsFilled(selection))
        {
            return "Unassigned";
        }

        var skillName = SkillCatalog.GetDisplayName(selection.SkillName);
        if (SkillCatalog.IsValueOnly(selection.SkillName) || string.IsNullOrWhiteSpace(selection.SpecialtyId))
        {
            return $"{skillName} Trained";
        }

        var specialtyName = _catalog.FindById(selection.SpecialtyId)?.DisplayName ?? selection.SpecialtyId;
        return $"{skillName} ({specialtyName}) Trained";
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }
}

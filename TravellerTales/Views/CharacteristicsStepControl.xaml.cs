using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public partial class CharacteristicsStepControl : UserControl
{
    private static readonly Brush BaseCellBrush = (Brush)new BrushConverter().ConvertFromString("#CC0D2A3C")!;
    private static readonly Brush SelectedCellBrush = (Brush)new BrushConverter().ConvertFromString("#DD1A5574")!;
    private static readonly Brush HoverCellBrush = (Brush)new BrushConverter().ConvertFromString("#CC12344A")!;
    private static readonly Brush BaseBorderBrush = (Brush)new BrushConverter().ConvertFromString("#5FD9FF")!;
    private static readonly Brush SelectedBorderBrush = (Brush)new BrushConverter().ConvertFromString("#C99A45")!;

    private CharacterCreationState _state = new();
    private Point _dragStartPoint;
    private CharacteristicKind? _selectedKind;

    public event EventHandler? ValidityChanged;

    public CharacteristicsStepControl()
    {
        InitializeComponent();
        SetCellKinds();
    }

    public void LoadState(CharacterCreationState state)
    {
        _state = state;
        _state.CharacteristicAssignment ??= CharacteristicRules.GenerateAssignment();
        Render();
    }

    public bool TryCommitRequired()
    {
        if (!IsComplete())
        {
            ValidationMessage.Text = "Characteristic assignments are required.";
            return false;
        }

        CommitAcceptedCharacteristics();
        ValidationMessage.Text = string.Empty;
        return true;
    }

    public void CommitPartial()
    {
        Render();
    }

    public bool IsComplete()
    {
        return _state.CharacteristicAssignment is not null &&
               CharacteristicRules.All.All(definition =>
                   CharacteristicRules.GetBaseValue(_state.CharacteristicAssignment, definition.Kind) > 0);
    }

    public void ResetView()
    {
        _selectedKind = null;
        Render();
    }

    private void CommitAcceptedCharacteristics()
    {
        var accepted = CharacteristicRules.BuildAdjustedSet(_state.CharacteristicAssignment!, _state.Character.Race);
        _state.Character.StartingCharacteristics = accepted.Clone();
        _state.Character.CurrentCharacteristics = accepted.Clone();
    }

    private void OnBaseCellMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border cell || cell.Tag is not CharacteristicKind kind)
        {
            return;
        }

        _dragStartPoint = e.GetPosition(this);

        if (_selectedKind.HasValue)
        {
            if (_selectedKind.Value != kind)
            {
                SwapBaseValues(_selectedKind.Value, kind);
            }

            _selectedKind = null;
        }
        else
        {
            _selectedKind = kind;
        }

        Render();
    }

    private void OnBaseCellMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            sender is not Border cell ||
            cell.Tag is not CharacteristicKind kind)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        if (Math.Abs(currentPosition.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(cell, kind, DragDropEffects.Move);
    }

    private void OnBaseCellDrop(object sender, DragEventArgs e)
    {
        ResetHoverBrushes();

        if (sender is not Border targetCell ||
            targetCell.Tag is not CharacteristicKind targetKind ||
            !e.Data.GetDataPresent(typeof(CharacteristicKind)))
        {
            return;
        }

        var sourceKind = (CharacteristicKind)e.Data.GetData(typeof(CharacteristicKind))!;
        if (sourceKind != targetKind)
        {
            SwapBaseValues(sourceKind, targetKind);
            _selectedKind = null;
            Render();
        }
    }

    private void OnBaseCellDragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border cell && cell.Tag is CharacteristicKind kind && _selectedKind != kind)
        {
            cell.Background = HoverCellBrush;
            cell.BorderBrush = SelectedBorderBrush;
        }
    }

    private void OnBaseCellDragLeave(object sender, DragEventArgs e)
    {
        ResetHoverBrushes();
    }

    private void SwapBaseValues(CharacteristicKind firstKind, CharacteristicKind secondKind)
    {
        if (_state.CharacteristicAssignment is null)
        {
            return;
        }

        var firstValue = CharacteristicRules.GetBaseValue(_state.CharacteristicAssignment, firstKind);
        var secondValue = CharacteristicRules.GetBaseValue(_state.CharacteristicAssignment, secondKind);
        CharacteristicRules.SetBaseValue(_state.CharacteristicAssignment, firstKind, secondValue);
        CharacteristicRules.SetBaseValue(_state.CharacteristicAssignment, secondKind, firstValue);
        ValidationMessage.Text = string.Empty;
        ValidityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Render()
    {
        if (_state.CharacteristicAssignment is null)
        {
            return;
        }

        RenderRow(CharacteristicKind.Strength, StrengthBaseCell, StrengthRaceText, StrengthResultText);
        RenderRow(CharacteristicKind.Dexterity, DexterityBaseCell, DexterityRaceText, DexterityResultText);
        RenderRow(CharacteristicKind.Endurance, EnduranceBaseCell, EnduranceRaceText, EnduranceResultText);
        RenderRow(CharacteristicKind.Intellect, IntellectBaseCell, IntellectRaceText, IntellectResultText);
        RenderRow(CharacteristicKind.Education, EducationBaseCell, EducationRaceText, EducationResultText);
        RenderRow(CharacteristicKind.Social, SocialBaseCell, SocialRaceText, SocialResultText);
    }

    private void RenderRow(CharacteristicKind kind, Border baseCell, TextBlock raceText, TextBlock resultText)
    {
        var baseValue = CharacteristicRules.GetBaseValue(_state.CharacteristicAssignment!, kind);
        var adjustment = CharacteristicRules.GetRaceAdjustment(_state.Character.Race, kind);
        var result = CharacteristicRules.ApplyRaceAdjustment(baseValue, _state.Character.Race, kind);
        var modifier = CharacteristicRules.GetDiceModifier(result);

        baseCell.Child = new TextBlock
        {
            Text = baseValue.ToString(CultureInfo.InvariantCulture),
            Style = (Style)FindResource("CharacteristicValueStyle")
        };
        baseCell.Background = _selectedKind == kind ? SelectedCellBrush : BaseCellBrush;
        baseCell.BorderBrush = _selectedKind == kind ? SelectedBorderBrush : BaseBorderBrush;
        baseCell.BorderThickness = _selectedKind == kind ? new Thickness(2) : new Thickness(1);
        raceText.Text = FormatAdjustment(adjustment);
        resultText.Text = $"{result} ({FormatModifier(modifier)})";
    }

    private void ResetHoverBrushes()
    {
        Render();
    }

    private void SetCellKinds()
    {
        StrengthBaseCell.Tag = CharacteristicKind.Strength;
        DexterityBaseCell.Tag = CharacteristicKind.Dexterity;
        EnduranceBaseCell.Tag = CharacteristicKind.Endurance;
        IntellectBaseCell.Tag = CharacteristicKind.Intellect;
        EducationBaseCell.Tag = CharacteristicKind.Education;
        SocialBaseCell.Tag = CharacteristicKind.Social;
    }

    private static string FormatAdjustment(int value)
    {
        return value > 0
            ? $"+{value}"
            : value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatModifier(int value)
    {
        return value > 0
            ? $"+{value}"
            : value.ToString(CultureInfo.InvariantCulture);
    }
}

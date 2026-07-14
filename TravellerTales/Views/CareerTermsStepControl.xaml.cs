using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TravellerTales.Models;
using TravellerTales.Services;

namespace TravellerTales.Views;

public partial class CareerTermsStepControl : UserControl
{
    private readonly Brush _slotBorderBrush = (Brush)new BrushConverter().ConvertFromString("#5FD9FF")!;
    private readonly Brush _slotBackgroundBrush = (Brush)new BrushConverter().ConvertFromString("#99071523")!;
    private readonly Brush _slotTextBrush = (Brush)new BrushConverter().ConvertFromString("#EAFBFF")!;
    private readonly Brush _mutedTextBrush = (Brush)new BrushConverter().ConvertFromString("#7897A8")!;

    private CharacterCreationState _state = new();

    public CareerTermsStepControl()
    {
        InitializeComponent();
    }

    public event EventHandler? ValidityChanged;
    public event EventHandler? TermActivityChanged;
    public event EventHandler<CharacterCreationCancelEventArgs>? CancelCharacterCreationRequested;

    public void LoadState(CharacterCreationState state)
    {
        _state = state;
        _state.CareerTerms ??= new();
        CareerTermService.Normalize(_state.CareerTerms);
        Render();
    }

    public bool IsComplete()
    {
        return CareerTermService.IsComplete(_state.CareerTerms);
    }

    public bool CanSave()
    {
        return CareerTermService.CanSave(_state.CareerTerms);
    }

    private void OnStartNextTerm(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        CareerTermService.StartNextTerm(_state.CareerTerms);
        TermActivityChanged?.Invoke(this, EventArgs.Empty);

        var owner = Window.GetWindow(this);
        if (owner is null)
        {
            return;
        }

        var window = new CareerTermDialogWindow(_state)
        {
            Owner = owner
        };
        window.CancelCharacterCreationRequested += OnTermCancelCharacterCreationRequested;
        window.ShowDialog();

        CareerTermService.Normalize(_state.CareerTerms);
        TermActivityChanged?.Invoke(this, EventArgs.Empty);
        Render();
    }

    private void OnTermCancelCharacterCreationRequested(object? sender, CharacterCreationCancelEventArgs e)
    {
        CancelCharacterCreationRequested?.Invoke(this, e);
    }

    private void OnFinish(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;
        CareerTermService.MarkReadyToFinish(_state.CareerTerms);
        Render();
    }

    private void Render()
    {
        TermsPanel.Children.Clear();

        SummaryText.Text =
            "Complete one four-year Career Term at a time. A Term must be finished before saving or moving on.";

        var career = string.IsNullOrWhiteSpace(_state.CareerTerms.CurrentCareer)
            ? "No active career"
            : $"{_state.CareerTerms.CurrentCareer} / {_state.CareerTerms.CurrentAssignment}";
        CurrentCareerText.Text =
            $"{career}\n" +
            $"Rank {_state.CareerTerms.CurrentRank} | Terms in Career {_state.CareerTerms.TermsInCurrentCareer} | " +
            $"Cash Benefits {_state.CareerTerms.TotalCashBenefits}/{CareerTermService.MaximumCashBenefits}";

        StartTermButton.IsEnabled = _state.CareerTerms.ActiveTerm is null;
        FinishButton.IsEnabled = _state.CareerTerms.ActiveTerm is null && _state.CareerTerms.CompletedTerms.Count > 0;

        if (_state.CareerTerms.CompletedTerms.Count == 0)
        {
            TermsPanel.Children.Add(new TextBlock
            {
                Text = "No Career Terms completed.",
                Style = (Style)FindResource("BodyTextStyle"),
                FontSize = 24,
                Foreground = _mutedTextBrush,
                TextWrapping = TextWrapping.Wrap
            });
        }

        foreach (var term in _state.CareerTerms.CompletedTerms)
        {
            TermsPanel.Children.Add(BuildTermRow(term));
        }

        if (_state.CareerTerms.ReadyToFinish)
        {
            ValidationText.Text = "Career Terms complete. Continue to Review when ready.";
        }
        else if (_state.CareerTerms.CompletedTerms.Count > 0)
        {
            ValidationText.Text = "Start another Term or finish Career creation.";
        }
        else
        {
            ValidationText.Text = "Complete at least one Career Term before continuing.";
        }

        ValidityChanged?.Invoke(this, EventArgs.Empty);
    }

    private UIElement BuildTermRow(CompletedCareerTerm term)
    {
        var border = new Border
        {
            Background = _slotBackgroundBrush,
            BorderBrush = _slotBorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18, 14, 18, 14),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var stack = new StackPanel();
        border.Child = stack;

        stack.Children.Add(new TextBlock
        {
            Text = $"Term {term.Sequence}: {term.Career} / {term.Assignment}",
            Style = (Style)FindResource("SectionHeaderTextStyle"),
            FontSize = 22,
            Foreground = _slotTextBrush,
            TextWrapping = TextWrapping.Wrap
        });

        stack.Children.Add(new TextBlock
        {
            Text =
                $"Career Term {term.CareerTermNumber} | Ending Rank {term.EndingRank} | " +
                (term.CareerEnded ? "Career ended" : "Career continues") + "\n" +
                $"{ValueOrNone(term.MishapSummary, term.EventSummary)}",
            Style = (Style)FindResource("DataFieldTextStyle"),
            FontSize = 20,
            Foreground = _slotTextBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        });

        return border;
    }

    private static string ValueOrNone(string first, string second)
    {
        if (!string.IsNullOrWhiteSpace(first))
        {
            return first;
        }

        return string.IsNullOrWhiteSpace(second) ? "No event recorded." : second;
    }
}

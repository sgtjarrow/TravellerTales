using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TravellerTales.Controls;

public partial class NumericStepper : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(int),
        typeof(NumericStepper),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(int),
        typeof(NumericStepper),
        new PropertyMetadata(1, OnRangeChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(int),
        typeof(NumericStepper),
        new PropertyMetadata(int.MaxValue, OnRangeChanged));

    private bool _isUpdatingText;

    public event EventHandler? ValueChanged;

    public NumericStepper()
    {
        InitializeComponent();
        UpdateText();
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    private static object CoerceValue(DependencyObject dependencyObject, object baseValue)
    {
        var control = (NumericStepper)dependencyObject;
        var value = (int)baseValue;
        return Math.Clamp(value, control.Minimum, Math.Max(control.Minimum, control.Maximum));
    }

    private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericStepper)dependencyObject;
        control.UpdateText();
        control.UpdateButtonStates();
        control.ValueChanged?.Invoke(control, EventArgs.Empty);
    }

    private static void OnRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericStepper)dependencyObject;
        control.CoerceValue(ValueProperty);
        control.UpdateText();
        control.UpdateButtonStates();
    }

    private void OnDecrease(object sender, RoutedEventArgs e)
    {
        Value = Math.Max(Minimum, Value - 1);
    }

    private void OnIncrease(object sender, RoutedEventArgs e)
    {
        Value = Math.Min(Math.Max(Minimum, Maximum), Value + 1);
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => !char.IsDigit(character));
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingText)
        {
            return;
        }

        if (int.TryParse(ValueTextBox.Text.Trim(), out var value))
        {
            Value = Math.Clamp(value, Minimum, Math.Max(Minimum, Maximum));
        }
    }

    private void UpdateText()
    {
        if (!IsInitialized)
        {
            return;
        }

        _isUpdatingText = true;
        ValueTextBox.Text = Value.ToString();
        _isUpdatingText = false;
    }

    private void UpdateButtonStates()
    {
        if (!IsInitialized)
        {
            return;
        }

        DecreaseButton.IsEnabled = Value > Minimum;
        IncreaseButton.IsEnabled = Value < Math.Max(Minimum, Maximum);
    }
}

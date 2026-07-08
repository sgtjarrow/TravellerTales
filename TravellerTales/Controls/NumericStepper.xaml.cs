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
        new PropertyMetadata(1));

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

    private static object CoerceValue(DependencyObject dependencyObject, object baseValue)
    {
        var control = (NumericStepper)dependencyObject;
        var value = (int)baseValue;
        return Math.Max(control.Minimum, value);
    }

    private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericStepper)dependencyObject;
        control.UpdateText();
        control.ValueChanged?.Invoke(control, EventArgs.Empty);
    }

    private void OnDecrease(object sender, RoutedEventArgs e)
    {
        Value = Math.Max(Minimum, Value - 1);
    }

    private void OnIncrease(object sender, RoutedEventArgs e)
    {
        Value++;
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
            Value = Math.Max(Minimum, value);
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
}

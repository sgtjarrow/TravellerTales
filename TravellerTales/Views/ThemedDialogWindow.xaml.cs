using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TravellerTales.Views;

public partial class ThemedDialogWindow : Window
{
    private readonly MessageBoxButton _buttons;
    private readonly MessageBoxImage _image;
    private MessageBoxResult _result = MessageBoxResult.None;

    public ThemedDialogWindow(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
    {
        _buttons = buttons;
        _image = image;

        InitializeComponent();

        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;

        ConfigureIcon();
        ConfigureButtons();
    }

    public MessageBoxResult Result => _result == MessageBoxResult.None ? GetDefaultResult() : _result;

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_result == MessageBoxResult.None)
        {
            _result = GetDefaultResult();
        }

        base.OnClosing(e);
    }

    private void ConfigureIcon()
    {
        var accent = _image == MessageBoxImage.Error ? "#FF6B6B" : "#FFB36A";
        var brush = (Brush)new BrushConverter().ConvertFromString(accent)!;

        IconFrame.BorderBrush = brush;
        IconText.Foreground = brush;
        IconText.Text = _image == MessageBoxImage.Error ? "X" : "!";
    }

    private void ConfigureButtons()
    {
        YesButton.Visibility = Visibility.Collapsed;
        NoButton.Visibility = Visibility.Collapsed;
        OkButton.Visibility = Visibility.Collapsed;

        if (_buttons == MessageBoxButton.YesNo)
        {
            YesButton.Visibility = Visibility.Visible;
            NoButton.Visibility = Visibility.Visible;
            NoButton.IsDefault = true;
            NoButton.IsCancel = true;
            Loaded += (_, _) => NoButton.Focus();
            return;
        }

        OkButton.Visibility = Visibility.Visible;
        OkButton.IsDefault = true;
        OkButton.IsCancel = true;
        Loaded += (_, _) => OkButton.Focus();
    }

    private MessageBoxResult GetDefaultResult()
    {
        return _buttons == MessageBoxButton.YesNo ? MessageBoxResult.No : MessageBoxResult.OK;
    }

    private void SetResultAndClose(MessageBoxResult result)
    {
        _result = result;
        Close();
    }

    private void OnYes(object sender, RoutedEventArgs e)
    {
        SetResultAndClose(MessageBoxResult.Yes);
    }

    private void OnNo(object sender, RoutedEventArgs e)
    {
        SetResultAndClose(MessageBoxResult.No);
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        SetResultAndClose(MessageBoxResult.OK);
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        SetResultAndClose(GetDefaultResult());
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        e.Handled = true;
        SetResultAndClose(GetDefaultResult());
    }
}

using System.Windows;

namespace TravellerTales.Views;

public static class ThemedDialog
{
    public static MessageBoxResult Show(
        Window owner,
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage image)
    {
        var dialog = new ThemedDialogWindow(message, title, buttons, image)
        {
            Owner = owner
        };

        dialog.ShowDialog();
        return dialog.Result;
    }
}

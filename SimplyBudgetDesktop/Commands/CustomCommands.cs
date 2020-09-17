using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SimplyBudget.Commands
{
    public static class CustomCommands
    {
        public static RoutedUICommand ClosePopup { get; } = new RoutedUICommand("Close", "ClosePopup", typeof(CustomCommands));

        public static ExecutedRoutedEventHandler OnClosePopup { get; } = (sender, e) =>
        {
            var popup = e.Source as Popup;
            if (popup != null)
                popup.IsOpen = false;
        };
    }
}
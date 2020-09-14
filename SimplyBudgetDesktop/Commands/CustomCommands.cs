using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SimplyBudget.Commands
{
    public static class CustomCommands
    {
        private static readonly RoutedUICommand _closePopup = new RoutedUICommand("Close", "ClosePopup", typeof(CustomCommands));

        private static readonly ExecutedRoutedEventHandler _onClosePopup =
            (sender, e) =>
                {
                    var popup = e.Source as Popup;
                    if (popup != null)
                        popup.IsOpen = false;
                };

        public static RoutedUICommand ClosePopup
        {
            get { return _closePopup; }
        }

        public static ExecutedRoutedEventHandler OnClosePopup
        {
            get { return _onClosePopup; }
        }
    }
}
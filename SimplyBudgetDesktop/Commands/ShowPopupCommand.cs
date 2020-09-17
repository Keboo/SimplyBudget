using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SimplyBudget.Commands
{
    public class ShowPopupCommand
    {
        private static readonly Lazy<ICommand> _instance = new Lazy<ICommand>(() => new RelayCommand<Popup>(OnShowPopup, CanShowPopup));

        public static ICommand Instance => _instance.Value;

        private ShowPopupCommand()
        { }

        private static bool CanShowPopup(Popup popup)
        {
            return popup != null && popup.IsOpen == false;
        }

        private static void OnShowPopup(Popup popup)
        {
            popup.IsOpen = true;
        }
    }
}
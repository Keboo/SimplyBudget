using System;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;

namespace SimplyBudget.Commands
{
    public class ShowPopupCommand : DelegateCommand<Popup>
    {
        private static readonly Lazy<ICommand> _instance = new Lazy<ICommand>(() => new ShowPopupCommand());

        public static ICommand Instance
        {
            get { return _instance.Value; }
        }

        private ShowPopupCommand()
            : base (OnShowPopup, CanShowPopup)
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
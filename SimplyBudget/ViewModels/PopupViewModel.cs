using System.Windows.Input;
using SimplyBudget.Utilities;
using SimplyBudgetShared.ViewModel;

namespace SimplyBudget.ViewModels
{
    public class PopupViewModel : ViewModelBase
    {
        private readonly DelegateCommand _showPopupCommand;

        public PopupViewModel()
        {
            _showPopupCommand = new DelegateCommand(OnShowPopup);
        }

        public virtual ICommand ShowPopupCommand
        {
            get { return _showPopupCommand; }
        }

        private bool _isOpen;
        public bool IsOpen
        {
            get { return _isOpen; }
            set { SetProperty(ref _isOpen, value); }
        }

        private void OnShowPopup()
        {
            IsOpen = true;
        }
    }
}
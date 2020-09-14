
using System.Windows.Input;
using SimplyBudget.Utilities;

namespace SimplyBudget.ViewModels
{
    public class InputPopupViewModel : PopupViewModel
    {
        private readonly DelegateCommand<int> _showPopup;

        public InputPopupViewModel()
        {
            _showPopup = new DelegateCommand<int>(OnShowPopupWithMode);
        }

        public override ICommand ShowPopupCommand
        {
            get { return _showPopup; }
        }

        public InputMode Mode { get; set; }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _input;
        public string Input
        {
            get { return _input; }
            set { SetProperty(ref _input, value); }
        }

        private void OnShowPopupWithMode(int mode)
        {
            Mode = (InputMode) mode;
            switch (Mode)
            {
                case InputMode.AddExpenseCategory:
                    Title = "Add Budget Item";
                    break;
                case InputMode.AddCategory:
                    Title = "Add Category";
                    break;
            }
            if (Mode != InputMode.None)
                IsOpen = true;
        }
    }
}
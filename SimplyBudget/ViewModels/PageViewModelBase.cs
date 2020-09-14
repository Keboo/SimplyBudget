using SimplyBudgetShared.ViewModel;

namespace SimplyBudget.ViewModels
{
    public abstract class PageViewModelBase : ViewModelBase
    {
        private bool _bottomAppBarIsOpen;
        public bool BottomAppBarIsOpen
        {
            get { return _bottomAppBarIsOpen; }
            set { SetProperty(ref _bottomAppBarIsOpen, value); }
        }
    }
}
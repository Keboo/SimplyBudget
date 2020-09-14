using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SimplyBudget.Utilities;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels
{
    public class MainPageViewModel : PageViewModelBase
    {
        private readonly ObservableCollection<string> _categories;
        private readonly InputPopupViewModel _inputPopup;
        private readonly DelegateCommand _addItemCommand;

        public MainPageViewModel()
        {
            _categories = new ObservableCollection<string>();

            _addItemCommand = new DelegateCommand(OnAddItem);
            _inputPopup = new InputPopupViewModel();

            LoadData();
        }

        public ObservableCollection<string> Categories
        {
            get { return _categories; }
        }

        public InputPopupViewModel InputPopup
        {
            get { return _inputPopup; }
        }

        public ICommand AddItemCommand
        {
            get { return _addItemCommand; }
        }

        private async void LoadData()
        {
            //TODO
        }

        private async void OnAddItem()
        {
            //Close the popup and extract its name
            InputPopup.IsOpen = false;
            var name = InputPopup.Input;
            var mode = InputPopup.Mode;
            InputPopup.Input = string.Empty;

            if (string.IsNullOrWhiteSpace(name)) return;
            
            switch (mode)
            {
                case InputMode.AddExpenseCategory:
                    //TODO
                    //var category = (await budget.GetCategories()).First();
                    //await category.CreateBudgetItem(name);
                    //LoadData();
                    break;
                case InputMode.AddCategory:
                    //TODO
                    //var newCategory = await budget.CreateCategory(name);
                    //_categories.Add(newCategory);
                    break;
            }


            //TODO: Is there no better way to do this???
            //LoadData();
        }
    }
}
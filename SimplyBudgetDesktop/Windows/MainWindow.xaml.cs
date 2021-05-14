using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using System.Linq;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow :
        IRecipient<OpenHistory>
    {
        public MainWindow()
        {
            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;
            InitializeComponent();
            viewModel.Messenger.Register(this);
        }

        public void Receive(OpenHistory message)
        {
            var history = ((MainWindowViewModel)DataContext).History;
            ExpenseCategoryViewModelEx category = message.ExpenseCategory;
            if (!history.FilterCategories.Any(x => x.ID == category.ExpenseCategoryID) &&
                history.ExpenseCategories.FirstOrDefault(x => x.ID == category.ExpenseCategoryID) is ExpenseCategory foundCategory)
            {
                history.FilterCategories.Clear();
                history.FilterCategories.Add(foundCategory);
            }
            TabControl.SelectedIndex = 1;
        }
    }
}

using SimplyBudget.ViewModels;

namespace SimplyBudget.Views
{
    /// <summary>
    /// Interaction logic for BudgetView.xaml
    /// </summary>
    public partial class BudgetView
    {
        public BudgetView()
        {
            InitializeComponent();
        }

        private void Open_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var viewModel = (BudgetViewModel)DataContext;
            var category = (ExpenseCategoryViewModelEx)e.Parameter;
            viewModel.OpenExpenseCategory(category);
        }
    }
}

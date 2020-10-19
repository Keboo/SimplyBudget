using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using System.Linq;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        public MainWindow()
        {
            DataContext = new MainWindowViewModel();
            InitializeComponent();
        }

        private void Open_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var history = ((MainWindowViewModel)DataContext).History;
            var category = (ExpenseCategoryViewModelEx)e.Parameter;
            if (!history.FilterCategories.Any(x => x.ID == category.ExpenseCategoryID) &&
                history.ExpenseCategories.FirstOrDefault(x => x.ID == category.ExpenseCategoryID) is ExpenseCategory foundCategory)
            {
                history.FilterCategories.Add(foundCategory);
            }
            TabControl.SelectedIndex = 1;
        }
    }
}

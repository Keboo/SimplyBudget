using SimplyBudget.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SimplyBudget.Views
{
    /// <summary>
    /// Interaction logic for BudgetView.xaml
    /// </summary>
    public partial class BudgetView
    {
        private BudgetViewModel ViewModel => (BudgetViewModel)DataContext;

        public BudgetView()
        {
            InitializeComponent();
        }

        private void Open_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var category = (ExpenseCategoryViewModelEx)e.Parameter;
            ViewModel.OpenExpenseCategory(category);
        }

        private void Properties_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ExpenseCategoryViewModelEx category)
            {
                category.IsEditing = true;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var category in e.RemovedItems.OfType<ExpenseCategoryViewModelEx>())
            {
                category.IsEditing = false;
            }
        }

        private async void Save_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            //TODO: debounce?
            if (e.Parameter is ExpenseCategoryViewModelEx category &&
                await ViewModel.SaveChanges(category))
            {
                category.IsEditing = false;
            }
        }

        private async void Delete_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ExpenseCategoryViewModelEx category)
            {
                await ViewModel.Delete(category);
            }
        }

        private void Delete_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter is ExpenseCategoryViewModelEx category)
            {
                e.CanExecute = category.IsHidden == false;
            }
        }

        private async void Restore_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ExpenseCategoryViewModelEx category)
            {
                await ViewModel.Undelete(category);
            }
        }

        private void Restore_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter is ExpenseCategoryViewModelEx category)
            {
                e.CanExecute = category.IsHidden;
            }
        }

        private void Copy_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            switch (e.Parameter)
            {
                case IClipboardData clipboardData:
                    clipboardData.OnCopy();
                    break;
                case object obj:
                    Clipboard.SetText(obj.ToString());
                    break;
            }
        }
    }
}

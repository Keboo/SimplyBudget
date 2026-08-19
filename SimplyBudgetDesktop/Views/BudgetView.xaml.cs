using SimplyBudget.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimplyBudget.Views;

/// <summary>
/// Interaction logic for BudgetView.xaml
/// </summary>
public partial class BudgetView
{
    /// <summary>
    /// Permanently removes an expense category (as opposed to <see cref="ApplicationCommands.Delete"/>,
    /// which just hides it). Only enabled when the category has no items.
    /// </summary>
    public static readonly RoutedUICommand DeletePermanentlyCommand =
        new("Delete Permanently", nameof(DeletePermanentlyCommand), typeof(BudgetView));

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

    private async void DeletePermanently_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not ExpenseCategoryViewModelEx category)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Permanently delete '{category.Name}'? This cannot be undone.",
            "Delete Category",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (!await ViewModel.DeletePermanently(category))
        {
            MessageBox.Show(
                "This category could not be deleted because it still has items (or is used by an import rule). Hide it instead.",
                "Delete Category",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void DeletePermanently_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
    {
        if (e.Parameter is ExpenseCategoryViewModelEx category)
        {
            e.CanExecute = !category.HasItems;
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

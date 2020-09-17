using SimplyBudget.ViewModels.Data;
using SimplyBudget.Windows;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Commands
{
    public class EditDatabaseItemCommand : MarkupCommandExtension<EditDatabaseItemCommand>
    {
        // ReSharper disable EmptyConstructor
        public EditDatabaseItemCommand() { }
        // ReSharper restore EmptyConstructor

        public override async void Execute(object parameter)
        {
            var databaseItem = parameter as BaseItem;
            if (databaseItem is null)
            {
                var dbItem = parameter as IDatabaseItem;
                if (dbItem != null)
                {
                    databaseItem = await dbItem.GetItem();
                }
            }

            if (databaseItem != null)
            {
                var transaction = databaseItem as Transaction;
                if (transaction != null)
                {
                    var window = new EditTransactionWindow();
                    await window.ViewModel.SetItemToEditAsync(transaction);
                    window.Show();
                    return;
                }
                var income = databaseItem as Income;
                if (income != null)
                {
                    var window = new EditIncomeItemWindow();
                    await window.ViewModel.SetItemToEditAsync(income);
                    window.Show();
                    return;
                }
                var transfer = databaseItem as Transfer;
                if (transfer != null)
                {
                    var window = new EditTransferWindow();
                    //TODO
                    return;
                }
                var expenseCategory = databaseItem as ExpenseCategory;
                if (expenseCategory != null)
                {
                    var window = new EditExpenseCategoryWindow();
                    await window.ViewModel.SetItemToEditAsync(expenseCategory);
                    window.Show();
                    return;
                }
                var account = databaseItem as Account;
                if (account != null)
                {
                    var window = new EditAccountWindow();
                    await window.ViewModel.SetItemToEditAsync(account);
                    window.Show();
                    return;
                }
            }
        }

        public override bool CanExecute(object parameter)
        {
            return parameter as BaseItem != null || parameter as IDatabaseItem != null;
        }
    }
}
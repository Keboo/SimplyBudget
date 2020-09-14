using SimplyBudget.ViewModels.Data;
using SimplyBudget.Windows;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Commands
{
    public class ShowExpenseCategoryHistory : MarkupCommandExtension<ShowExpenseCategoryHistory>
    {
        // ReSharper disable once EmptyConstructor
        public ShowExpenseCategoryHistory()
        { }

        public override async void Execute(object parameter)
        {
            var category = parameter as ExpenseCategory;
            if (category == null)
            {
                var databaseItem = parameter as IDatabaseItem;
                if (databaseItem != null)
                    category = await databaseItem.GetItem() as ExpenseCategory;
            }

            if (category == null) return;

            var window = new ExpenseCategoryHistory();
            window.Show();
            await window.ViewModel.LoadItemsAsync(category);
        }
    }
}
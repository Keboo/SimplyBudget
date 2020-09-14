
using JetBrains.Annotations;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.ObjectModel;
using SimplyBudgetShared.ViewModel;

namespace SimplyBudget.ViewModels
{
    public class ExpenseCategoryViewModel : ViewModelBase
    {
        private readonly ObservableCollection<TransactionItem> _transactions = new ObservableCollection<TransactionItem>();
        private readonly ExpenseCategory _expenseCategory;

        public ExpenseCategoryViewModel([NotNull] ExpenseCategory expenseCategory)
        {
            if (expenseCategory == null) throw new ArgumentNullException("expenseCategory");
            _expenseCategory = expenseCategory;
            
            LoadData();
        }

        public ObservableCollection<TransactionItem> Transactions
        {
            get { return _transactions; }
        }

        private async void LoadData()
        {
            _transactions.AddRange(await _expenseCategory.GetTransactionItems(DateTime.Now.StartOfMonth(), DateTime.Now.EndOfMonth()));
        }
    }
}
using SimplyBudgetShared.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.Data
{
    internal class TransactionViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject, ITransactionItem
    {
        public static async Task<TransactionViewModel> Create(BudgetContext context, Transaction transaction)
        {
            if (transaction is null) throw new ArgumentNullException(nameof(transaction));

            return default;
            //var items = await transaction.GetTransactionItems();
            //
            //string expenseCategoryName = "(none)";
            //if (items.Count == 1)
            //{
            //    var expenseCategory = await context.ExpenseCategories.FindAsync(items[0].ExpenseCategoryID);
            //    if (expenseCategory != null)
            //        expenseCategoryName = expenseCategory.CategoryName;
            //}
            //else if (items.Count > 1)
            //{
            //    expenseCategoryName = "(multiple)";
            //}
            //
            //return new TransactionViewModel(transaction.ID)
            //           {
            //               Date = transaction.Date,
            //               Description = transaction.Description,
            //               ExpenseCategoryName = expenseCategoryName,
            //               NumItems = items.Count,
            //               Amount = items.Sum(x => x.Amount)
            //           };
        }

        private BudgetContext Context { get; } = BudgetContext.Instance;

        private TransactionViewModel(int transactionID)
        {
            TransactionID = transactionID;
        }

        public int TransactionID { get; }

        private int _amount;
        public int Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _expenseCategoryName;
        public string ExpenseCategoryName
        {
            get => _expenseCategoryName;
            set => SetProperty(ref _expenseCategoryName, value);
        }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private int _numItems;
        public int NumItems
        {
            get => _numItems;
            set => SetProperty(ref _numItems, value);
        }

        public async Task<BaseItem> GetItem()
        {
            return await Context.Transactions.FindAsync(TransactionID);
        }
    }
}
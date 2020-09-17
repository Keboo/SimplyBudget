using SimplyBudgetShared.Data;
using System;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.Data
{
    internal class TransactionItemViewModel : ViewModelBase, IDatabaseItem
    {
        public static async Task<TransactionItemViewModel> Create(BudgetContext context, TransactionItem transactionItem)
        {
            if (transactionItem is null) throw new ArgumentNullException("transactionItem");

            var transaction = await context.Transactions.FindAsync(transactionItem.TransactionID);

            var expenseCategory = await context.ExpenseCategories.FindAsync(transactionItem.ExpenseCategoryID);

            if (transaction != null)
                return Create(transactionItem, transaction, expenseCategory);
            return null;
        }

        public static TransactionItemViewModel Create(TransactionItem transactionItem,
                                                      Transaction transaction,
                                                      ExpenseCategory expenseCategory)
        {
            if (transactionItem is null) throw new ArgumentNullException("transactionItem");
            if (transaction is null) throw new ArgumentNullException("transaction");
            return new TransactionItemViewModel(transactionItem.ID)
                       {
                           Amount = transactionItem.Amount,
                           Description = transactionItem.Description,
                           Date = transaction.Date,
                           ExpenseCategoryName = expenseCategory?.Name
                       };
        }

        private BudgetContext Context { get; } = BudgetContext.Instance;

        private TransactionItemViewModel(int transactionItemID)
        {
            TransactionItemID = transactionItemID;
        }

        public int TransactionItemID { get; }

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

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private string _expenseCategoryName;
        public string ExpenseCategoryName
        {
            get => _expenseCategoryName;
            set => SetProperty(ref _expenseCategoryName, value);
        }

        public async Task<BaseItem> GetItem()
        {
            return await Context.TransactionItems.FindAsync(TransactionItemID);
        }
    }
}
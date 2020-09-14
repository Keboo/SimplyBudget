using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SimplyBudgetShared.Data;

namespace SimplyBudget.ViewModels.Data
{
    internal class TransactionItemViewModel : ViewModelBase, IDatabaseItem
    {
        public static async Task<TransactionItemViewModel> Create([NotNull] TransactionItem transactionItem)
        {
            if (transactionItem == null) throw new ArgumentNullException("transactionItem");

            var transaction = await DatabaseManager.GetAsync<Transaction>(transactionItem.TransactionID);

            var expenseCategory = await DatabaseManager.GetAsync<ExpenseCategory>(transactionItem.ExpenseCategoryID);

            if (transaction != null)
                return Create(transactionItem, transaction, expenseCategory);
            return null;
        }

        public static TransactionItemViewModel Create([NotNull] TransactionItem transactionItem,
                                                      [NotNull] Transaction transaction,
                                                      [CanBeNull] ExpenseCategory expenseCategory)
        {
            if (transactionItem == null) throw new ArgumentNullException("transactionItem");
            if (transaction == null) throw new ArgumentNullException("transaction");
            return new TransactionItemViewModel(transactionItem.ID)
                       {
                           Amount = transactionItem.Amount,
                           Description = transactionItem.Description,
                           Date = transaction.Date,
                           ExpenseCategoryName = expenseCategory != null ? expenseCategory.Name : null
                       };
        }

        private readonly int _transactionItemID;
        private TransactionItemViewModel(int transactionItemID)
        {
            _transactionItemID = transactionItemID;
        }

        public int TransactionItemID
        {
            get { return _transactionItemID; }
        }

        private int _amount;
        public int Amount
        {
            get { return _amount; }
            set { SetProperty(ref _amount, value); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        private DateTime _date;
        public DateTime Date
        {
            get { return _date; }
            set { SetProperty(ref _date, value); }
        }

        private string _expenseCategoryName;
        public string ExpenseCategoryName
        {
            get { return _expenseCategoryName; }
            set { SetProperty(ref _expenseCategoryName, value); }
        }

        public async Task<BaseItem> GetItem()
        {
            return await DatabaseManager.GetAsync<TransactionItem>(TransactionItemID);
        }
    }
}
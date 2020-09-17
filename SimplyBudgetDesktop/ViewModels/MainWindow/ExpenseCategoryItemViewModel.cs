using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class ExpenseCategoryItemViewModel : ViewModelBase, IDatabaseItem
    {
        public static async Task<ExpenseCategoryItemViewModel> Create(BudgetContext context, TransactionItem transactionItem)
        {
            if (transactionItem is null) throw new ArgumentNullException(nameof(transactionItem));
            var transaction = await context.Transactions.FindAsync(transactionItem.TransactionID);
            if (transaction is null) return null;
            return new ExpenseCategoryItemViewModel(transactionItem)
                       {
                           Amount = transactionItem.Amount,
                           Date = transaction.Date,
                           Description = transactionItem.Description
                       };
        }

        public static async Task<ExpenseCategoryItemViewModel> Create(BudgetContext context, Transfer transfer, int expenseCategoryID)
        {
            if (transfer is null) throw new ArgumentNullException(nameof(transfer));

            var amount = transfer.Amount;

            var sb = new StringBuilder();
            sb.Append("Transfer");
            if (transfer.FromExpenseCategoryID == expenseCategoryID)
            {
                var toExpenseCategory = await context.ExpenseCategories.FindAsync(transfer.ToExpenseCategoryID);
                sb.AppendFormat(" to {0}", toExpenseCategory != null ? toExpenseCategory.Name : "(unknown)");
            }
            else if (transfer.ToExpenseCategoryID == expenseCategoryID)
            {
                var fromExpenseCategory = await context.ExpenseCategories.FindAsync(transfer.ToExpenseCategoryID);
                sb.AppendFormat(" from {0}", fromExpenseCategory != null ? fromExpenseCategory.Name : "(unknown)");
                amount *= -1;
            }
            sb.AppendFormat(": {0}", transfer.Description);

            return new ExpenseCategoryItemViewModel(transfer)
                       {
                           Amount = amount,
                           Date = transfer.Date,
                           Description = sb.ToString()
                       };
        }

        public static async Task<ExpenseCategoryItemViewModel> Create(BudgetContext context, IncomeItem incomeItem)
        {
            if (incomeItem is null) throw new ArgumentNullException("incomeItem");
            var income = await context.Incomes.FindAsync(incomeItem.IncomeID);

            return new ExpenseCategoryItemViewModel(incomeItem)
                       {
                           Amount = -1*incomeItem.Amount,
                           Date = income.Date,
                           Description = string.Format("Income: {0}", incomeItem.Description ?? "")
                       };
        }

        private BudgetContext Context { get; } = BudgetContext.Instance;

        private ExpenseCategoryItemViewModel(TransactionItem transactionItem)
        {
            TransactionItemID = transactionItem.ID;
        }

        private ExpenseCategoryItemViewModel(Transfer transfer)
        {
            TransferID = transfer.ID;
        }

        private ExpenseCategoryItemViewModel(IncomeItem incomeItem)
        {
            IncomeItemID = incomeItem.ID;
        }

        public int TransactionItemID { get; }

        public int TransferID { get; }

        public int IncomeItemID { get; }

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

        public async Task<BaseItem> GetItem()
        {
            if (TransferID > 0)
                return await Context.Transfers.FindAsync(TransferID);
            if (TransactionItemID > 0)
                return await Context.TransactionItems.FindAsync(TransactionItemID);
            if (IncomeItemID > 0)
                return await Context.IncomeItems.FindAsync(IncomeItemID);
            return null;
        }
    }
}
using System;
using System.Threading.Tasks;
using SimplyBudgetShared.Data;

namespace SimplyBudget.ViewModels.Data
{
    internal class TransferViewModel : ViewModelBase, ITransactionItem
    {
        private BudgetContext Context { get; } = BudgetContext.Instance;

        public static async Task<TransferViewModel> Create(Transfer transfer)
        {
            if (transfer is null) throw new ArgumentNullException(nameof(transfer));

            var fromExpenseCategory = await BudgetContext.Instance.ExpenseCategories.FindAsync(transfer.FromExpenseCategoryID);
            var toExpenseCategory = await BudgetContext.Instance.ExpenseCategories.FindAsync(transfer.ToExpenseCategoryID);

            string from = fromExpenseCategory != null ? fromExpenseCategory.Name : "(unknown)";
            string to = toExpenseCategory != null ? toExpenseCategory.Name : "(unknown)";

            return new TransferViewModel(transfer.ID)
            {
                Date = transfer.Date,
                Description = transfer.Description,
                ExpenseCategoryName = string.Format("Transfer from {0} to {1}", from, to),
                Amount = transfer.Amount
            };
        }

        private TransferViewModel(int transferID)
        {
            TransferID = transferID;
        }

        public int TransferID { get; }

        private int _amount;
        public int Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private string _expenseCategoryName;
        public string ExpenseCategoryName
        {
            get => _expenseCategoryName;
            set => SetProperty(ref _expenseCategoryName, value);
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
            return await Context.Transfers.FindAsync(TransferID);
        }
    }
}
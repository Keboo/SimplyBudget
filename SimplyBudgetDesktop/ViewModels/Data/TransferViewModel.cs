using System;
using System.Threading.Tasks;
using SimplyBudgetShared.Data;

namespace SimplyBudget.ViewModels.Data
{
    internal class TransferViewModel : ViewModelBase, ITransactionItem
    {
        public static async Task<TransferViewModel> Create(Transfer transfer)
        {
            if (transfer == null) throw new ArgumentNullException("transfer");

            var fromExpenseCategory = await DatabaseManager.Instance.Connection.GetAsync<ExpenseCategory>(transfer.FromExpenseCategoryID);
            var toExpenseCategory = await DatabaseManager.Instance.Connection.GetAsync<ExpenseCategory>(transfer.ToExpenseCategoryID);

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

        private readonly int _transferID;
        private TransferViewModel(int transferID)
        {
            _transferID = transferID;
        }

        public int TransferID
        {
            get { return _transferID; }
        }

        private int _amount;
        public int Amount
        {
            get { return _amount; }
            set { SetProperty(ref _amount, value); }
        }

        private string _expenseCategoryName;
        public string ExpenseCategoryName
        {
            get { return _expenseCategoryName; }
            set { SetProperty(ref _expenseCategoryName, value); }
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

        public async Task<BaseItem> GetItem()
        {
            return await DatabaseManager.GetAsync<Transfer>(TransferID);
        }
    }
}
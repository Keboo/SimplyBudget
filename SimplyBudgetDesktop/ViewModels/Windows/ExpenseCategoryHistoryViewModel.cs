using SimplyBudget.Controls;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.Windows
{
    public class ExpenseCategoryHistoryViewModel : ViewModelBase
    {
        private readonly BindingList<IBarGraphItem> _months = new BindingList<IBarGraphItem>();
        
        private string _ExpenseCategoryName;
        public string ExpenseCategoryName
        {
            get { return _ExpenseCategoryName; }
            set { SetProperty(ref _ExpenseCategoryName, value); }
        }

        private int _BudgettedAmount;
        public int BudgettedAmount
        {
            get { return _BudgettedAmount; }
            set { SetProperty(ref _BudgettedAmount, value); }
        }

        public async Task LoadItemsAsync(ExpenseCategory expenseCategory)
        {
            ExpenseCategoryName = expenseCategory.Name;
            BudgettedAmount = expenseCategory.BudgetedAmount;

            foreach (var month in Enumerable.Range(0, 12).Reverse().Select(x => DateTime.Today.AddMonths(-x)))
            {
                var transactions = await expenseCategory.GetTransactionItems(month.StartOfMonth(), month.EndOfMonth());
                _months.Add(new MonthHistoryViewModel
                {
                    BarTitle = month.ToString("MMM yyyy"),
                    MonthlyExpenses = transactions.Sum(x => x.Amount)
                });
            }

            //bar max height is 90%
            double hundredPercent = Math.Max(_months.Cast<MonthHistoryViewModel>().Select(x => x.MonthlyExpenses).Max(),
                    expenseCategory.BudgetedAmount)/0.9;
            if ((int) hundredPercent == 0) return;
            foreach (var month in _months.Cast<MonthHistoryViewModel>())
            {
                month.BarPercentHeight = Convert.ToInt32(month.MonthlyExpenses / hundredPercent * 100);
                month.LinePercentHeight = Convert.ToInt32(expenseCategory.BudgetedAmount / hundredPercent * 100 );
            }
        }

        public BindingList<IBarGraphItem> Months
        {
            get { return _months; }
        }
    }

    internal class MonthHistoryViewModel : ViewModelBase, IBarGraphItem
    {
        public int MonthlyExpenses { get; set; }

        private string _BarTitle;
        public string BarTitle
        {
            get { return _BarTitle; }
            set { SetProperty(ref _BarTitle, value); }
        }

        private int _BarPercentHeight;
        public int BarPercentHeight
        {
            get { return _BarPercentHeight; }
            set { SetProperty(ref _BarPercentHeight, value); }
        }

        private int? _LinePercentHeight;
        public int? LinePercentHeight
        {
            get { return _LinePercentHeight; }
            set { SetProperty(ref _LinePercentHeight, value); }
        }
    }
}
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
        private string _ExpenseCategoryName;
        public string ExpenseCategoryName
        {
            get => _ExpenseCategoryName;
            set => SetProperty(ref _ExpenseCategoryName, value);
        }

        private int _BudgettedAmount;
        public int BudgettedAmount
        {
            get => _BudgettedAmount;
            set => SetProperty(ref _BudgettedAmount, value);
        }

        private BudgetContext Context { get; } = BudgetContext.Instance;

        public async Task LoadItemsAsync(ExpenseCategory expenseCategory)
        {
            ExpenseCategoryName = expenseCategory.Name;
            BudgettedAmount = expenseCategory.BudgetedAmount;

            foreach (var month in Enumerable.Range(0, 12).Reverse().Select(x => DateTime.Today.AddMonths(-x)))
            {
                var transactions = await Context.GetTransactionItems(expenseCategory, month.StartOfMonth(), month.EndOfMonth());
                Months.Add(new MonthHistoryViewModel
                {
                    BarTitle = month.ToString("MMM yyyy"),
                    MonthlyExpenses = transactions.Sum(x => x.Amount)
                });
            }

            //bar max height is 90%
            double hundredPercent = Math.Max(Months.Cast<MonthHistoryViewModel>().Select(x => x.MonthlyExpenses).Max(),
                    expenseCategory.BudgetedAmount)/0.9;
            if ((int) hundredPercent == 0) return;
            foreach (var month in Months.Cast<MonthHistoryViewModel>())
            {
                month.BarPercentHeight = Convert.ToInt32(month.MonthlyExpenses / hundredPercent * 100);
                month.LinePercentHeight = Convert.ToInt32(expenseCategory.BudgetedAmount / hundredPercent * 100 );
            }
        }

        public BindingList<IBarGraphItem> Months { get; } = new BindingList<IBarGraphItem>();
    }

    internal class MonthHistoryViewModel : ViewModelBase, IBarGraphItem
    {
        public int MonthlyExpenses { get; set; }

        private string _BarTitle;
        public string BarTitle
        {
            get => _BarTitle;
            set => SetProperty(ref _BarTitle, value);
        }

        private int _BarPercentHeight;
        public int BarPercentHeight
        {
            get => _BarPercentHeight;
            set => SetProperty(ref _BarPercentHeight, value);
        }

        private int? _LinePercentHeight;
        public int? LinePercentHeight
        {
            get => _LinePercentHeight;
            set => SetProperty(ref _LinePercentHeight, value);
        }
    }
}
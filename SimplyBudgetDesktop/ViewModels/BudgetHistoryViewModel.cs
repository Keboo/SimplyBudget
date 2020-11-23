using Microsoft.Toolkit.Mvvm.ComponentModel;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels
{
    public class BudgetHistoryViewModel : ObservableObject
    {
        public BudgetHistoryViewModel(ExpenseCategoryItem item, int currentAmount)
        {
            Item = item;

            Date = item.Date;
            Description = item.Description ?? "";
            CurrentAmount = currentAmount;

            Details = item.Details?
                .Select(x => new BudgetHistoryDetailsViewModel(x))
                .OrderBy(x => x.ExpenseCategoryName)
                .ToList() ?? new List<BudgetHistoryDetailsViewModel>();

            int total = item.Details?.Sum(x => x.Amount) ?? 0;
            if (item.Details?.Count == 2 && total == 0)
            {
                DisplayAmount = $"<{(-Math.Abs(item.Details[0].Amount)).FormatCurrency()}>";
            }
            else
            {
                DisplayAmount = total.FormatCurrency();
            }
        }

        private ExpenseCategoryItem Item { get; }

        public DateTime Date { get; }

        public string Description { get; }

        public string DisplayAmount { get; }

        public int CurrentAmount { get; }

        public IReadOnlyList<BudgetHistoryDetailsViewModel> Details { get; }

        public async Task Delete(BudgetContext context)
        {
            context.Remove(Item);
            await context.SaveChangesAsync();
        }
    }
}
using Microsoft.Toolkit.Mvvm.ComponentModel;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels
{
    public enum BudgetHistoryViewModelType
    {
        Unknown,
        Transfer,
        Transaction,
        Income
    }

    public class BudgetHistoryViewModel : ObservableObject
    {
        public BudgetHistoryViewModel(ExpenseCategoryItem item, int currentAccountAmount)
        {
            Item = item;

            Date = item.Date;
            Description = item.Description ?? "";
            CurrentAmount = currentAccountAmount;

            Details = item.Details?
                .Select(x => new BudgetHistoryDetailsViewModel(x))
                .OrderBy(x => x.ExpenseCategoryName)
                .ToList() ?? new List<BudgetHistoryDetailsViewModel>();

            if (item.IsTransfer)
            {
                Type = BudgetHistoryViewModelType.Transfer;
            }
            else if (item.Details?.Sum(x => x.Amount) > 0)
            {
                Type = BudgetHistoryViewModelType.Income;
            }
            else
            {
                Type = BudgetHistoryViewModelType.Transaction;
            }

            DisplayAmount = item.GetDisplayAmount();
        }

        private ExpenseCategoryItem Item { get; }

        public DateTime Date { get; }

        public string Description { get; }

        public string DisplayAmount { get; }

        public int CurrentAmount { get; }

        public BudgetHistoryViewModelType Type { get; }

        public IReadOnlyList<BudgetHistoryDetailsViewModel> Details { get; }

        public async Task Delete(BudgetContext context)
        {
            context.Remove(Item);
            await context.SaveChangesAsync();
        }
    }
}
using Microsoft.Toolkit.Mvvm.ComponentModel;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels
{
    public abstract class BudgetHistoryViewModel : ObservableObject
    {
        internal static BudgetHistoryViewModel Create(ExpenseCategoryItem item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return new ExpenseCategoryItemHistoryViewModel(item);
        }

        internal static BudgetHistoryViewModel Create(Income income)
        {
            if (income is null)
            {
                throw new ArgumentNullException(nameof(income));
            }

            return new IncomeBudgetHistoryViewModel(income);
        }

        internal static BudgetHistoryViewModel Create(Transaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            return new TransactionBudgetHistoryViewModel(transaction);
        }

        internal static BudgetHistoryViewModel Create(Transfer transfer)
        {
            if (transfer is null)
            {
                throw new ArgumentNullException(nameof(transfer));
            }

            return new TransferBudgetHistoryViewModel(transfer);
        }

        private class ExpenseCategoryItemHistoryViewModel : BudgetHistoryViewModel
        {
            private ExpenseCategoryItem Item { get; }

            public ExpenseCategoryItemHistoryViewModel(ExpenseCategoryItem item)
            {
                Item = item;

                Date = item.Date;
                Description = item.Description;

                Details = item.Details
                    .Select(x => new BudgetHistoryDetailsViewModel(x))
                    .OrderBy(x => x.ExpenseCategoryName)
                    .ToList();

                int total = item.Details.Sum(x => x.Amount);
                if (item.Details.Count == 2 && total == 0)
                {
                    DisplayAmount = $"<{Math.Abs(item.Details[0].Amount).FormatCurrency()}>";
                }
                else if (total > 0)
                {
                    DisplayAmount = $"({total.FormatCurrency()})";
                }
                else
                {
                    DisplayAmount = $"{(-total).FormatCurrency()}";
                }
            }

            public override async Task Delete(BudgetContext context)
            {
                context.Remove(Item);
                await context.SaveChangesAsync();
            }
        }

        private class IncomeBudgetHistoryViewModel : BudgetHistoryViewModel
        {
            private Income Income { get; }

            public IncomeBudgetHistoryViewModel(Income income)
            {
                Income = income;

                Date = income.Date;
                Description = income.Description;
                DisplayAmount = $"({income.TotalAmount.FormatCurrency()})";

                Details = income.IncomeItems
                    .Select(x => new BudgetHistoryDetailsViewModel(x))
                    .OrderBy(x => x.ExpenseCategoryName)
                    .ToList();

            }

            public override async Task Delete(BudgetContext context)
            {
                context.Remove(Income);
                await context.SaveChangesAsync();
            }
        }

        private class TransactionBudgetHistoryViewModel : BudgetHistoryViewModel
        {
            private Transaction Transaction { get; }

            public TransactionBudgetHistoryViewModel(Transaction transaction)
            {
                Transaction = transaction;

                int total = transaction.TransactionItems.Sum(x => x.Amount);

                Date = transaction.Date;
                Description = transaction.Description;
                DisplayAmount = total.FormatCurrency();

                Details = transaction.TransactionItems
                    .Select(x => new BudgetHistoryDetailsViewModel(x))
                    .OrderBy(x => x.ExpenseCategoryName)
                    .ToList();
            }

            public override async Task Delete(BudgetContext context)
            {
                context.Remove(Transaction);
                await context.SaveChangesAsync();
            }
        }

        private class TransferBudgetHistoryViewModel : BudgetHistoryViewModel
        {
            public Transfer Transfer { get; }

            public TransferBudgetHistoryViewModel(Transfer transfer)
            {
                Transfer = transfer;

                Date = transfer.Date;
                Description = transfer.Description;
                DisplayAmount = $"<{transfer.Amount.FormatCurrency()}>";

                Details = new List<BudgetHistoryDetailsViewModel>
                {
                    new BudgetHistoryDetailsViewModel(transfer.Amount.FormatCurrency(), transfer.FromExpenseCategory.Name),
                    new BudgetHistoryDetailsViewModel($"({transfer.Amount.FormatCurrency()})", transfer.ToExpenseCategory.Name),
                };
            }

            public override async Task Delete(BudgetContext context)
            {
                context.Remove(Transfer);
                await context.SaveChangesAsync();
            }
        }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            private set => SetProperty(ref _date, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            private set => SetProperty(ref _description, value);
        }

        private string _displayAmount;
        public string DisplayAmount
        {
            get => _displayAmount;
            private set => SetProperty(ref _displayAmount, value);
        }

        public IReadOnlyList<BudgetHistoryDetailsViewModel> Details { get; private set;  }

        public abstract Task Delete(BudgetContext context);
    }
}
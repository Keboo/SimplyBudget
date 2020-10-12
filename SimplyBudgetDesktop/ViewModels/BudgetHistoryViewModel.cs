using Microsoft.Toolkit.Mvvm.ComponentModel;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Threading.Tasks;

namespace SimplyBudget.ViewModels.MainWindow
{
    public abstract class BudgetHistoryViewModel : ObservableObject
    {
        internal static BudgetHistoryViewModel Create(Income income)
        {
            if (income is null)
            {
                throw new ArgumentNullException(nameof(income));
            }

            return new IncomeBudgetHistoryViewModel(income);
        }

        internal static BudgetHistoryViewModel Create(Transaction transaction, int totalAmount)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            return new TransactionBudgetHistoryViewModel(transaction, totalAmount);
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

            public TransactionBudgetHistoryViewModel(Transaction transaction, int totalAmount)
            {
                Transaction = transaction;

                Date = transaction.Date;
                Description = transaction.Description;
                DisplayAmount = totalAmount.FormatCurrency();
            }

            public override async Task Delete(BudgetContext context)
            {
                context.Remove(Transaction);
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

        public abstract Task Delete(BudgetContext context);
    }
}
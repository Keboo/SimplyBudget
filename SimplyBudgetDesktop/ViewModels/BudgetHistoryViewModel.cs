using Microsoft.Toolkit.Mvvm.ComponentModel;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;

namespace SimplyBudget.ViewModels.MainWindow
{
    public class BudgetHistoryViewModel : ObservableObject
    {
        internal static BudgetHistoryViewModel Create(Income income)
        {
            return new BudgetHistoryViewModel
            {
                Date = income.Date,
                Description = income.Description,
                DisplayAmount = $"({income.TotalAmount.FormatCurrency()})"
            };
        }

        internal static BudgetHistoryViewModel Create(Transaction transaction, int totalAmount)
        {
            return new BudgetHistoryViewModel
            {
                Date = transaction.Date,
                Description = transaction.Description,
                DisplayAmount = totalAmount.FormatCurrency()
            };
        }

        private BudgetHistoryViewModel()
        { }

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

    }
}
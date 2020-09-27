using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SimplyBudgetShared.Data;

namespace SimplyBudget.ViewModels.Data
{
    internal class IncomeViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject, IDatabaseItem
    {
        public static IncomeViewModel Create(Income income)
        {
            if (income is null) throw new ArgumentNullException("income");

            return new IncomeViewModel(income.ID)
                       {
                           Date = income.Date,
                           Description = income.Description,
                           TotalAmount = income.TotalAmount
                       };
        }

        private BudgetContext Context { get; } = BudgetContext.Instance;

        private IncomeViewModel(int incomeID)
        {
            IncomeID = incomeID;
        }

        public int IncomeID { get; }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private int _totalAmount;
        public int TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public async Task<BaseItem> GetItem()
        {
            return await Context.Incomes.FindAsync(IncomeID);
        }
    }
}
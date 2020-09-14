using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SimplyBudgetShared.Data;

namespace SimplyBudget.ViewModels.Data
{
    internal class IncomeViewModel : ViewModelBase, IDatabaseItem
    {
        public static IncomeViewModel Create([NotNull] Income income)
        {
            if (income == null) throw new ArgumentNullException("income");

            return new IncomeViewModel(income.ID)
                       {
                           Date = income.Date,
                           Description = income.Description,
                           TotalAmount = income.TotalAmount
                       };
        }

        private readonly int _incomeID;

        private IncomeViewModel(int incomeID)
        {
            _incomeID = incomeID;
        }

        public int IncomeID
        {
            get { return _incomeID; }
        }

        private DateTime _date;
        public DateTime Date
        {
            get { return _date; }
            set { SetProperty(ref _date, value); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        private int _totalAmount;
        public int TotalAmount
        {
            get { return _totalAmount; }
            set { SetProperty(ref _totalAmount, value); }
        }

        public async Task<BaseItem> GetItem()
        {
            return await GetDatabaseConnection().GetAsync<Income>(IncomeID);
        }
    }
}
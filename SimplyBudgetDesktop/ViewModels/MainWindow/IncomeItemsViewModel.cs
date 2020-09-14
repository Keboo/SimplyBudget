using System.ComponentModel;
using System.Linq;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class IncomeItemsViewModel : CollectionViewModelBase<IncomeViewModel>, 
        IEventListener<IncomeEvent>, IEventListener<IncomeItemEvent>
    {
        public IncomeItemsViewModel()
        {
            _view.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));

            NotificationCenter.Register<IncomeEvent>(this);
            NotificationCenter.Register<IncomeItemEvent>(this);
        }

        public ICollectionView IncomeItemsView
        {
            get { return _view; }
        }

        public string Title
        {
            get { return "Income Items"; }
        }

        private DateTime _queryStart = DateTime.Now.StartOfMonth();
        public DateTime QueryStart
        {
            get { return _queryStart; }
            set
            {
                if (SetProperty(ref _queryStart, value))
                    LoadItemsAsync();
            }
        }

        private DateTime _queryEnd = DateTime.Now.EndOfMonth();
        public DateTime QueryEnd
        {
            get { return _queryEnd; }
            set
            {
                if (SetProperty(ref _queryEnd, value))
                    LoadItemsAsync();
            }
        }

        protected override async Task<IEnumerable<IncomeViewModel>> GetItems()
        {
            var incomeItems = await GetDatabaseConnection()
                                        .Table<Income>()
                                        .Where(x => x.Date >= QueryStart && x.Date <= QueryEnd)
                                        .ToListAsync();
            return incomeItems.Select(IncomeViewModel.Create);
        }

        public async void HandleEvent(IncomeEvent @event)
        {
            await ReloadItemsAsync();
        }

        public async void HandleEvent(IncomeItemEvent @event)
        {
            await ReloadItemsAsync();
        }
    }
}
using System.ComponentModel;
using System.Linq;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;
using Microsoft.EntityFrameworkCore;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class IncomeItemsViewModel : CollectionViewModelBaseOld<IncomeViewModel>, 
        IEventListener<IncomeEvent>, IEventListener<IncomeItemEvent>
    {
        private BudgetContext Context { get; } = BudgetContext.Instance;

        public IncomeItemsViewModel()
        {
            _view.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));

            NotificationCenter.Register<IncomeEvent>(this);
            NotificationCenter.Register<IncomeItemEvent>(this);
        }

        public ICollectionView IncomeItemsView => _view;

        public string Title => "Income Items";

        private DateTime _queryStart = DateTime.Now.StartOfMonth();
        public DateTime QueryStart
        {
            get => _queryStart;
            set
            {
                if (SetProperty(ref _queryStart, value))
                    LoadItemsAsync();
            }
        }

        private DateTime _queryEnd = DateTime.Now.EndOfMonth();
        public DateTime QueryEnd
        {
            get => _queryEnd;
            set
            {
                if (SetProperty(ref _queryEnd, value))
                    LoadItemsAsync();
            }
        }

        protected override async Task<IEnumerable<IncomeViewModel>> GetItems()
        {
            var incomeItems = await Context.Incomes
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
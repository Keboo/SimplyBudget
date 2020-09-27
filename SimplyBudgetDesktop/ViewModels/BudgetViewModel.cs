using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SimplyBudget.ViewModels.MainWindow
{
    public class BudgetViewModel : CollectionViewModelBase<ExpenseCategoryViewModelEx>
    {
        private BudgetContext Context { get; } = BudgetContext.Instance;

        public BudgetViewModel(IMessenger messenger)
        {
            if (messenger is null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            messenger.Register<ExpenseCategoryEvent>(this, HandleEvent);
            GroupItems = true;
        }

        public ICollectionView ExpenseCategoriesView
        {
            get
            {
                SetDescriptors();
                return _view;
            }
        }

        public string Title => "Budget for " + DateTime.Today.ToString("MMMM");

        private bool _groupItems;
        public bool GroupItems
        {
            get => _groupItems;
            set
            {
                if (SetProperty(ref _groupItems, value))
                {
                    SetDescriptors();
                }
            }
        }

        protected override async Task<IEnumerable<ExpenseCategoryViewModelEx>> GetItems()
        {
            var expenseCategories = await Context.ExpenseCategories.ToListAsync();

            var rv = new List<ExpenseCategoryViewModelEx>();
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var item in expenseCategories)
                rv.Add(await ExpenseCategoryViewModelEx.Create(Context, item));
            // ReSharper restore LoopCanBeConvertedToQuery
            return rv;
        }

        public async void HandleEvent(ExpenseCategoryEvent @event)
        {
            var expenseCategory = @event.ExpenseCategory;

            switch (@event.Type)
            {
                case EventType.Created:
                    _items.Add(await ExpenseCategoryViewModelEx.Create(@event.Context, expenseCategory));
                    break;
                case EventType.Updated:
                    _items.RemoveFirst(x => x.ExpenseCategoryID == expenseCategory.ID);
                    _items.Add(await ExpenseCategoryViewModelEx.Create(@event.Context, expenseCategory));
                    break;
                case EventType.Deleted:
                    _items.RemoveFirst(x => x.ExpenseCategoryID == expenseCategory.ID);
                    break;
            }
        }

        private void SetDescriptors()
        {
            _view.SortDescriptions.Clear();
            if (GroupItems)
                _view.SortDescriptions.Add(new SortDescription("CategoryName", ListSortDirection.Ascending));
            _view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            _view.GroupDescriptions.Clear();

            if (GroupItems)
                _view.GroupDescriptions.Add(new PropertyGroupDescription("CategoryName"));
        }
    }
}
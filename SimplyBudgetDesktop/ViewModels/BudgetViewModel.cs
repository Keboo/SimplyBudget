using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SimplyBudget.ViewModels
{
    public class BudgetViewModel : CollectionViewModelBase<ExpenseCategoryViewModelEx>,
        IRecipient<ExpenseCategoryEvent>,
        IRecipient<CurrentMonthChanged>
    {
        private BudgetContext Context { get; }

        public IMessenger Messenger { get; }
        public ICurrentMonth CurrentMonth { get; }

        public BudgetViewModel(BudgetContext context, IMessenger messenger, ICurrentMonth currentMonth)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));
            messenger.Register<ExpenseCategoryEvent>(this);
            messenger.Register<CurrentMonthChanged>(this);
            GroupItems = true;
        }

        private int _totalBudget;
        public int TotalBudget
        {
            get => _totalBudget;
            private set => SetProperty(ref _totalBudget, value);
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

        protected override async IAsyncEnumerable<ExpenseCategoryViewModelEx> GetItems()
        {
            await foreach (var category in Context.ExpenseCategories)
            {
                yield return await ExpenseCategoryViewModelEx.Create(Context, category, CurrentMonth.CurrenMonth);
            }
        }

        public async void Receive(ExpenseCategoryEvent @event)
        {
            var expenseCategory = @event.ExpenseCategory;

            switch (@event.Type)
            {
                case EventType.Created:
                    Items.Add(await ExpenseCategoryViewModelEx.Create(@event.Context, expenseCategory));
                    break;
                case EventType.Updated:
                    Items.RemoveFirst(x => x.ExpenseCategoryID == expenseCategory.ID);
                    Items.Add(await ExpenseCategoryViewModelEx.Create(@event.Context, expenseCategory));
                    break;
                case EventType.Deleted:
                    Items.RemoveFirst(x => x.ExpenseCategoryID == expenseCategory.ID);
                    break;
            }
        }

        public void OpenExpenseCategory(ExpenseCategoryViewModelEx category)
            => Messenger.Send(new OpenHistory(category));

        public void Receive(CurrentMonthChanged message)
            => LoadItemsAsync();

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

        protected override async Task ReloadItemsAsync()
        {
            TotalBudget = 0;
            await base.ReloadItemsAsync();
            int percentage = 0;
            int total = 0;
            foreach (var category in Items)
            {
                if (category.BudgetedPercentage > 0)
                {
                    percentage += category.BudgetedPercentage;
                }
                else
                {
                    total += category.BudgetedAmount;
                }
            }
            //TODO: percentage > 100
            //Let x be to total budget
            //Let t = total budgeted
            //Let p = total percentage
            //x - (x * p)/100 = t
            //100x - (x * p) = 100t
            //x(100 - p) = 100t
            //x = 100t/(100 - p)
            TotalBudget = total * 100 / (100 - percentage);
        }
    }
}
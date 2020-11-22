﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SimplyBudget.ViewModels.MainWindow
{

    public class HistoryViewModel : CollectionViewModelBase<BudgetHistoryViewModel>,
        IRecipient<TransactionEvent>,
        IRecipient<TransactionItemEvent>,
        IRecipient<IncomeEvent>,
        IRecipient<IncomeItemEvent>,
        IRecipient<TransferEvent>,
        IRecipient<CurrentMonthChanged>
    {
        public BudgetContext Context { get; }
        public ICurrentMonth CurrentMonth { get; }

        public IRelayCommand AddFilterCommand { get; }
        public ICommand RemoveFilterCommand { get; }

        public ICollectionView HistoryView => _view;

        public ObservableCollection<ExpenseCategory> ExpenseCategories { get; }
            = new ObservableCollection<ExpenseCategory>();

        public ObservableCollection<ExpenseCategory> FilterCategories { get; }
            = new ObservableCollection<ExpenseCategory>();

        private ExpenseCategory _selectedCategory;
        public ExpenseCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    AddFilterCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public HistoryViewModel(BudgetContext context, IMessenger messenger, ICurrentMonth currentMonth)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));
            Sort(nameof(BudgetHistoryViewModel.Date), ListSortDirection.Descending);

            ExpenseCategories.AddRange(context.ExpenseCategories);

            AddFilterCommand = new RelayCommand<ExpenseCategory>(OnAddFilter, x => x != null);
            RemoveFilterCommand = new RelayCommand<ExpenseCategory>(
                x => FilterCategories.Remove(x), x => x != null);

            FilterCategories.CollectionChanged += FilterCategories_CollectionChanged;

            messenger.Register<TransactionEvent>(this);
            messenger.Register<TransactionItemEvent>(this);
            messenger.Register<IncomeEvent>(this);
            messenger.Register<IncomeItemEvent>(this);
            messenger.Register<TransferEvent>(this);
            messenger.Register<CurrentMonthChanged>(this);
        }

        private void OnAddFilter(ExpenseCategory category)
        {
            if (!FilterCategories.Contains(category))
            {
                FilterCategories.Add(category);
            }
            SelectedCategory = null;
        }

        private void FilterCategories_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => LoadItemsAsync();

        protected override async IAsyncEnumerable<BudgetHistoryViewModel> GetItems()
        {
            var oldestTime = CurrentMonth.CurrenMonth.AddMonths(-2).StartOfMonth();

            var categoryList = new List<int>();
            if (FilterCategories.Any())
            {
                categoryList.AddRange(FilterCategories.Select(x => x.ID));
            }

            var query = Context.ExpenseCategoryItems
                .Include(x => x.Details)
                .Where(x => x.Date >= oldestTime);

            if (categoryList.Any())
            {
                query = query.Where(x => x.Details.Any(x => categoryList.Contains(x.ExpenseCategoryId)));
            }

            query = query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.ID);

            await foreach (var item in query.AsAsyncEnumerable())
            {
                yield return BudgetHistoryViewModel.Create(item);
            }
        }

        public async Task DeleteItems(IEnumerable<BudgetHistoryViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach(var item in items.ToList())
            {
                await item.Delete(Context);
            }
        }

        public void Receive(IncomeItemEvent message) => LoadItemsAsync();

        public void Receive(IncomeEvent message) => LoadItemsAsync();

        public void Receive(TransactionEvent message) => LoadItemsAsync();

        public void Receive(TransactionItemEvent message) => LoadItemsAsync();

        public void Receive(TransferEvent message) => LoadItemsAsync();

        public void Receive(CurrentMonthChanged message) => LoadItemsAsync();
    }
}
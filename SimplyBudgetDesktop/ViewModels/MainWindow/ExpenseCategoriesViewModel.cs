﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;

namespace SimplyBudget.ViewModels.MainWindow
{
    internal class ExpenseCategoriesViewModel : CollectionViewModelBase<ExpenseCategoryViewModelEx>,
        IEventListener<ExpenseCategoryEvent>
    {
        public ExpenseCategoriesViewModel()
        {
            NotificationCenter.Register(this);
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

        public string Title
        {
            get { return "Budget for " + DateTime.Today.ToString("MMMM"); }
        }

        private bool _groupItems;
        public bool GroupItems
        {
            get { return _groupItems; }
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
            var expenseCategories = await GetDatabaseConnection().Table<ExpenseCategory>().ToListAsync();

            var rv = new List<ExpenseCategoryViewModelEx>();
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach (var item in expenseCategories)
                rv.Add(await ExpenseCategoryViewModelEx.Create(item));
            // ReSharper restore LoopCanBeConvertedToQuery
            return rv;
        }

        public async void HandleEvent(ExpenseCategoryEvent @event)
        {
            var expenseCategory = @event.ExpenseCategory;

            switch (@event.Type)
            {
                case EventType.Created:
                    _items.Add(await ExpenseCategoryViewModelEx.Create(expenseCategory));
                    break;
                case EventType.Updated:
                    _items.RemoveFirst(x => x.ExpenseCategoryID == expenseCategory.ID);
                    _items.Add(await ExpenseCategoryViewModelEx.Create(expenseCategory));
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

    internal class ExpenseCategoryViewModelEx : ExpenseCategoryViewModel
    {
        public static new async Task<ExpenseCategoryViewModelEx> Create([NotNull] ExpenseCategory expenseCategory)
        {
            return await Create(expenseCategory, DateTime.Today);
        }

        public static async Task<ExpenseCategoryViewModelEx> Create([NotNull] ExpenseCategory expenseCategory, DateTime month )
        {
            if (expenseCategory == null) throw new ArgumentNullException("expenseCategory");
            var transactions = await expenseCategory.GetTransactionItems(month.StartOfMonth(), month.EndOfMonth());
            var incomeItems = await expenseCategory.GetIncomeItems(month.StartOfMonth(), month.EndOfMonth());

            var rv = new ExpenseCategoryViewModelEx(expenseCategory.ID);
            SetProperties(expenseCategory, rv);
            rv.MonthlyExpenses = transactions.Sum(x => x.Amount);
            rv.MonthlyAllocations = incomeItems.Sum(x => x.Amount);
            rv.BudgetedAmountDisplay = expenseCategory.GetBudgetedDisplayString();
            return rv;
        }

        private ExpenseCategoryViewModelEx(int expenseCategoryID)
            : base(expenseCategoryID)
        { }

        private int _monthlyExpenses;
        public int MonthlyExpenses
        {
            get { return _monthlyExpenses; }
            set { SetProperty(ref _monthlyExpenses, value); }
        }

        private int _monthlyAllocations;
        public int MonthlyAllocations
        {
            get { return _monthlyAllocations; }
            set { SetProperty(ref _monthlyAllocations, value); }
        }

        private string _budgetedAmountDisplay;
        public string BudgetedAmountDisplay
        {
            get { return _budgetedAmountDisplay; }
            set { SetProperty(ref _budgetedAmountDisplay, value); }
        }
    }
}
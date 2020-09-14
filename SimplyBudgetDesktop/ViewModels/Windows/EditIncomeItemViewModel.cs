using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Practices.Prism.Commands;
using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using SimplyBudgetShared.Utilities.Events;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace SimplyBudget.ViewModels.Windows
{
    internal class EditIncomeItemViewModel : ViewEditViewModel<Income>, IRequestClose,
        IEventListener<ExpenseCategoryEvent>, IEventListener<IncomeItemEvent>
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly ObservableCollection<IncomeItemDetailsViewModel> _incomeItems;
        private readonly ICollectionView _incomeItemsView;

        private readonly DelegateCommand _clearCommand;

        private Income _existingIncome;

        public EditIncomeItemViewModel()
        {
            _incomeItems = new ObservableCollection<IncomeItemDetailsViewModel>();
            _incomeItemsView = CollectionViewSource.GetDefaultView(_incomeItems);
            _incomeItemsView.Filter = x =>
                                          {
                                              if (ShowAll == false)
                                              {
                                                  var vm = (IncomeItemDetailsViewModel)x;
                                                  return vm.ExpenseCategory.BudgetedPercentage > 0 ||
                                                         vm.ExpenseCategory.RemainingAmount > 0;
                                              }
                                              return true;
                                          };
            _incomeItemsView.SortDescriptions.Add(new SortDescription("ExpenseCategory.Name", ListSortDirection.Ascending));
            _clearCommand = new DelegateCommand(OnClear);

            _date = DateTime.Today;
        }

        public ICollectionView IncomeItems
        {
            get { return _incomeItemsView; }
        }

        public ICommand ClearCommand
        {
            get { return _clearCommand; }
        }

        private string _incomeItemsError;
        public string IncomeItemsError
        {
            get { return _incomeItemsError; }
            set { SetProperty(ref _incomeItemsError, value); }
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
            set
            {
                if (SetProperty(ref _totalAmount, value))
                    UpdateAmountRemaining();
            }
        }

        private int _remainingAmount;
        public int RemainingAmount
        {
            get { return _remainingAmount; }
            set
            {
                if (SetProperty(ref _remainingAmount, value))
                    RemainingAmountError = "";
            }
        }

        private string _remainingAmountError;
        public string RemainingAmountError
        {
            get { return _remainingAmountError; }
            set { SetProperty(ref _remainingAmountError, value); }
        }

        private bool _showAll;
        public bool ShowAll
        {
            get { return _showAll; }
            set
            {
                if (SetProperty(ref _showAll, value))
                    _incomeItemsView.Refresh();
            }
        }

        private void OnClear()
        {
            foreach (var item in _incomeItems)
                item.SetAmountWithoutUpdate(0);
            UpdateAmountRemaining();
        }

        protected override async Task CreateAsync()
        {
            if (HasErrors()) return;

            var income = new Income { Date = Date, TotalAmount = TotalAmount, Description = Description };
            await income.Save();

            foreach (var transactionItem in _incomeItems.Where(x => x.Amount > 0))
            {
                var item = new IncomeItem
                {
                    Amount = transactionItem.Amount,
                    ExpenseCategoryID = transactionItem.ExpenseCategory.ExpenseCategoryID,
                    Description = Description,
                    IncomeID = income.ID
                };
                await item.Save();
            }
            RequestClose.Raise(this, EventArgs.Empty);
        }

        protected override async Task SaveAsync()
        {
            if (_existingIncome == null) return;

            if (HasErrors()) return;

            _existingIncome.Date = Date;
            _existingIncome.Description = Description;
            _existingIncome.TotalAmount = TotalAmount;
            await _existingIncome.Save();

            var existingIncomeItems = await _existingIncome.GetIncomeItems();
            var existingDic = existingIncomeItems.ToDictionary(x => x.ID);

            foreach (var incomeItemVM in _incomeItems.Where(x => x.Amount > 0))
            {
                IncomeItem existingIncomeItem;
                if (incomeItemVM.ExistingIncomeItemID > 0 &&
                    existingDic.TryGetValue(incomeItemVM.ExistingIncomeItemID,
                                                     out existingIncomeItem))
                {
                    existingDic.Remove(incomeItemVM.ExistingIncomeItemID);
                    //Update existing item
                    existingIncomeItem = existingIncomeItem ?? new IncomeItem();
                    existingIncomeItem.Amount = incomeItemVM.Amount;
                    existingIncomeItem.ExpenseCategoryID = incomeItemVM.ExpenseCategory.ExpenseCategoryID;
                    await existingIncomeItem.Save();
                }
                else
                {
                    //Add new item
                    await _existingIncome.AddIncomeItem(incomeItemVM.ExpenseCategory.ExpenseCategoryID, incomeItemVM.Amount);
                }
            }

            //Remove any remaining transactions
            if (existingDic.Count > 0)
            {
                foreach (var incomeItem in existingDic.Values)
                {
                    await incomeItem.Delete();
                }
            }

            RequestClose.Raise(this, EventArgs.Empty);
        }

        private bool HasErrors()
        {
            bool rv = false;
            if (RemainingAmount != 0)
            {
                RemainingAmountError = "There is still a balance remaining";
                rv = true;
            }

            var incomeItems = _incomeItems.Where(x => x.Amount > 0).ToArray();
            if (incomeItems.Length == 0)
            {
                IncomeItemsError = "At least one allocation is required";
                rv = true;
            }

            return rv;
        }

        protected override async Task SetPropertiesToEditAsync(Income incomeItem)
        {
            Date = incomeItem.Date;
            Description = incomeItem.Description;
            TotalAmount = incomeItem.TotalAmount;

            _incomeItems.Clear();
            
            var expenseCategoryIds = new List<int>();
            var incomeItems = await incomeItem.GetIncomeItems();
            if (incomeItems != null)
            {
                foreach (var item in incomeItems)
                {
                    expenseCategoryIds.Add(item.ExpenseCategoryID);
                    var incomeViewModel = await IncomeExpenseCategoryViewModel.Create(await DatabaseManager.GetAsync<ExpenseCategory>(item.ExpenseCategoryID));
                    _incomeItems.Add(new IncomeItemDetailsViewModel(item, incomeViewModel, this));
                }
            }
            //Add in remaining expense categories
            var expenseCategories = await DatabaseManager.Instance.Connection.Table<ExpenseCategory>().Where(x => expenseCategoryIds.Contains(x.ID) == false).ToListAsync();
            foreach (var expenseCategory in expenseCategories)
            {
                var vm = await IncomeExpenseCategoryViewModel.Create(expenseCategory);
                _incomeItems.Add(new IncomeItemDetailsViewModel(this, vm));
            }

            UpdateAmountRemaining();
            ShowAll = true;
            _existingIncome = incomeItem;
        }

        public void UpdateAmountRemaining()
        {
            RemainingAmount = TotalAmount - _incomeItems.Select(x => x.Amount).Sum();
        }

        public async void HandleEvent(ExpenseCategoryEvent @event)
        {
            //This is likley wrong.... consider the case where we were editing an existing income item :/
            await LoadDefaultView();
        }

        public async void HandleEvent(IncomeItemEvent @event)
        {
            //This is likley wrong.... consider the case where we were editing an existing income item :/
            await LoadDefaultView();
        }

        public override async Task LoadDefaultView()
        {
            _incomeItems.Clear();
            var expenseCategories = await DatabaseManager.Instance.Connection.Table<ExpenseCategory>().ToListAsync();
            foreach (var expenseCategory in expenseCategories)
            {
                var vm = await IncomeExpenseCategoryViewModel.Create(expenseCategory);
                _incomeItems.Add(new IncomeItemDetailsViewModel(this, vm));
            }
        }
    }

    internal class IncomeItemDetailsViewModel : ViewModelBase
    {
        private readonly int _existingIncomeItemID;
        private readonly IncomeExpenseCategoryViewModel _expenseCategory;
        private readonly EditIncomeItemViewModel _editIncomeItemViewModel;

        private readonly ICommand _setAmountCommand;

        public IncomeItemDetailsViewModel([NotNull] EditIncomeItemViewModel parent,
                                          [NotNull] IncomeExpenseCategoryViewModel expenseCategory)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            if (expenseCategory == null) throw new ArgumentNullException("expenseCategory");

            _editIncomeItemViewModel = parent;
            _expenseCategory = expenseCategory;

            _setAmountCommand = new DelegateCommand(OnSetAmount);
        }

        public IncomeItemDetailsViewModel([NotNull] IncomeItem incomeItem,
            [NotNull] IncomeExpenseCategoryViewModel expenseCategory,
            [NotNull] EditIncomeItemViewModel parent)
            : this(parent, expenseCategory)
        {
            if (incomeItem == null) throw new ArgumentNullException("incomeItem");
            _existingIncomeItemID = incomeItem.ID;
            Amount = incomeItem.Amount;
        }

        public ICommand SetAmountCommand
        {
            get { return _setAmountCommand; }
        }

        public int ExistingIncomeItemID
        {
            get { return _existingIncomeItemID; }
        }

        private int? _amount;
        public int Amount
        {
            get { return _amount ?? 0; }
            set
            {
                if (SetProperty(ref _amount, value))
                {
                    _editIncomeItemViewModel.UpdateAmountRemaining();
                }
            }
        }

        public IncomeExpenseCategoryViewModel ExpenseCategory
        {
            get { return _expenseCategory; }
        }

        public void SetAmountWithoutUpdate(int amount)
        {
            SetProperty(ref _amount, amount, "Amount");
        }

        public bool AmountSet()
        {
            return _amount != null;
        }

        private void OnSetAmount()
        {
            int remainingAmount = _editIncomeItemViewModel.RemainingAmount;

            int totalAmount = _editIncomeItemViewModel.TotalAmount;
            int? amount = null;
            if (ExpenseCategory.BudgetedPercentage > 0)
            {
                amount = (int)((ExpenseCategory.BudgetedPercentage / 100.0) * totalAmount);
            }
            else if (ExpenseCategory.BudgetedAmount > 0)
            {
                amount = ExpenseCategory.RemainingAmount;
            }

            if (amount != null && remainingAmount > 0 && amount != Amount)
            {
                Amount = Math.Min(remainingAmount, amount.Value);
            }
        }
    }

    internal class IncomeExpenseCategoryViewModel : ExpenseCategoryViewModel
    {
        public new static async Task<IncomeExpenseCategoryViewModel> Create(ExpenseCategory expenseCategory)
        {
            var rv = new IncomeExpenseCategoryViewModel(expenseCategory.ID);

            SetProperties(expenseCategory, rv);

            var incomeItems = await expenseCategory.GetIncomeItems(DateTime.Today.StartOfMonth(), DateTime.Today.EndOfMonth());

            if (expenseCategory.UsePercentage)
            {
                rv.RemainingAmount = 0;
            }
            else
            {
                var incomeSum = incomeItems.Sum(x => x.Amount);
                rv.RemainingAmount = (expenseCategory.BudgetedAmount - incomeSum);
            }

            return rv;
        }

        private IncomeExpenseCategoryViewModel(int expenseCategoryID)
            : base(expenseCategoryID)
        { }

        private int _remainingAmount;
        public int RemainingAmount
        {
            get { return _remainingAmount; }
            set { SetProperty(ref _remainingAmount, value); }
        }
    }
}
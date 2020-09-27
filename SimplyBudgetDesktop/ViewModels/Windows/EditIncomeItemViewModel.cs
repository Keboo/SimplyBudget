using System.Collections.Generic;
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
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace SimplyBudget.ViewModels.Windows
{
    internal class EditIncomeItemViewModel : ViewEditViewModel<Income>, IRequestClose,
        IEventListener<ExpenseCategoryEvent>, IEventListener<IncomeItemEvent>
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly ObservableCollection<IncomeItemDetailsViewModel> _incomeItems;
        private readonly RelayCommand _clearCommand;

        private Income _existingIncome;

        private BudgetContext Context { get; } = BudgetContext.Instance;

        public EditIncomeItemViewModel()
        {
            _incomeItems = new ObservableCollection<IncomeItemDetailsViewModel>();
            IncomeItems = CollectionViewSource.GetDefaultView(_incomeItems);
            IncomeItems.Filter = x =>
                                          {
                                              if (ShowAll == false)
                                              {
                                                  var vm = (IncomeItemDetailsViewModel)x;
                                                  return vm.ExpenseCategory.BudgetedPercentage > 0 ||
                                                         vm.ExpenseCategory.RemainingAmount > 0;
                                              }
                                              return true;
                                          };
            IncomeItems.SortDescriptions.Add(new SortDescription("ExpenseCategory.Name", ListSortDirection.Ascending));
            _clearCommand = new RelayCommand(OnClear);

            _date = DateTime.Today;
        }

        public ICollectionView IncomeItems { get; }

        public ICommand ClearCommand => _clearCommand;

        private string _incomeItemsError;
        public string IncomeItemsError
        {
            get => _incomeItemsError;
            set => SetProperty(ref _incomeItemsError, value);
        }

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
            set
            {
                if (SetProperty(ref _totalAmount, value))
                    UpdateAmountRemaining();
            }
        }

        private int _remainingAmount;
        public int RemainingAmount
        {
            get => _remainingAmount;
            set
            {
                if (SetProperty(ref _remainingAmount, value))
                    RemainingAmountError = "";
            }
        }

        private string _remainingAmountError;
        public string RemainingAmountError
        {
            get => _remainingAmountError;
            set => SetProperty(ref _remainingAmountError, value);
        }

        private bool _showAll;
        public bool ShowAll
        {
            get => _showAll;
            set
            {
                if (SetProperty(ref _showAll, value))
                    IncomeItems.Refresh();
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
            if (_existingIncome is null) return;

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
                    var incomeViewModel = await IncomeExpenseCategoryViewModel.Create(await Context.ExpenseCategories.FindAsync(item.ExpenseCategoryID), Context);
                    _incomeItems.Add(new IncomeItemDetailsViewModel(item, incomeViewModel, this));
                }
            }
            //Add in remaining expense categories
            var expenseCategories = await Context.ExpenseCategories.Where(x => expenseCategoryIds.Contains(x.ID) == false).ToListAsync();
            foreach (var expenseCategory in expenseCategories)
            {
                var vm = await IncomeExpenseCategoryViewModel.Create(expenseCategory, Context);
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
            var expenseCategories = await Context.ExpenseCategories.ToListAsync();
            foreach (var expenseCategory in expenseCategories)
            {
                var vm = await IncomeExpenseCategoryViewModel.Create(expenseCategory, Context);
                _incomeItems.Add(new IncomeItemDetailsViewModel(this, vm));
            }
        }
    }

    internal class IncomeItemDetailsViewModel : ViewModelBase
    {
        private readonly EditIncomeItemViewModel _editIncomeItemViewModel;

        public IncomeItemDetailsViewModel(EditIncomeItemViewModel parent,
                                          IncomeExpenseCategoryViewModel expenseCategory)
        {
            if (parent is null) throw new ArgumentNullException(nameof(parent));
            if (expenseCategory is null) throw new ArgumentNullException(nameof(expenseCategory));

            _editIncomeItemViewModel = parent;
            ExpenseCategory = expenseCategory;

            SetAmountCommand = new RelayCommand(OnSetAmount);
        }

        public IncomeItemDetailsViewModel(IncomeItem incomeItem,
            IncomeExpenseCategoryViewModel expenseCategory,
            EditIncomeItemViewModel parent)
            : this(parent, expenseCategory)
        {
            if (incomeItem is null) throw new ArgumentNullException(nameof(incomeItem));
            ExistingIncomeItemID = incomeItem.ID;
            Amount = incomeItem.Amount;
        }

        public ICommand SetAmountCommand { get; }

        public int ExistingIncomeItemID { get; }

        private int? _amount;
        public int Amount
        {
            get => _amount ?? 0;
            set
            {
                if (SetProperty(ref _amount, value))
                {
                    _editIncomeItemViewModel.UpdateAmountRemaining();
                }
            }
        }

        public IncomeExpenseCategoryViewModel ExpenseCategory { get; }

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
        public new static async Task<IncomeExpenseCategoryViewModel> Create(ExpenseCategory expenseCategory, BudgetContext context)
        {
            var rv = new IncomeExpenseCategoryViewModel(expenseCategory.ID);

            SetProperties(expenseCategory, rv);

            var incomeItems = await context.GetIncomeItems(expenseCategory, DateTime.Today.StartOfMonth(), DateTime.Today.EndOfMonth());
            
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
            get => _remainingAmount;
            set => SetProperty(ref _remainingAmount, value);
        }
    }
}